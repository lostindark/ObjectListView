/*
 * Copyright (C) 2006-2014 Phillip Piper
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * If you wish to use this code in a closed source application, please contact phillip.piper@gmail.com.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace BrightIdeasSoftware
{
    /// <summary>
    /// A type-safe wrapper around an OLVColumn
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TypedColumn<T> where T : class
    {
        private static readonly ConditionalWeakTable<OLVColumn, TypedColumn<T>> adapters =
            new ConditionalWeakTable<OLVColumn, TypedColumn<T>>();

        internal static TypedColumn<T> For(OLVColumn column) {
            return column == null ? null : adapters.GetValue(column, key => new TypedColumn<T>(key));
        }

        /// <summary>
        /// Creates a TypedColumn
        /// </summary>
        /// <param name="column"></param>
        public TypedColumn(OLVColumn column) {
            ArgumentNullException.ThrowIfNull(column, nameof(column));
            this.column = column;
        }
        private OLVColumn column;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rowObject"></param>
        /// <returns></returns>
        [SuppressMessage("Naming", "CA1711", Justification = "The Delegate suffix identifies this public typed callback.")]
        public delegate Object TypedAspectGetterDelegate(T rowObject);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rowObject"></param>
        /// <param name="newValue"></param>
        [SuppressMessage("Naming", "CA1711", Justification = "The Delegate suffix identifies this public typed callback.")]
        public delegate void TypedAspectPutterDelegate(T rowObject, Object newValue);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rowObject"></param>
        /// <returns></returns>
        [SuppressMessage("Naming", "CA1711", Justification = "The Delegate suffix identifies this public typed callback.")]
        public delegate Object TypedGroupKeyGetterDelegate(T rowObject);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rowObject"></param>
        /// <returns></returns>
        [SuppressMessage("Naming", "CA1711", Justification = "The Delegate suffix identifies this public typed callback.")]
        public delegate Object TypedImageGetterDelegate(T rowObject);

        /// <summary>
        /// 
        /// </summary>
        public TypedAspectGetterDelegate AspectGetter {
            get {
                AspectGetterDelegate getter = this.column.AspectGetter;
                return getter == null ? null : new TypedAspectGetterDelegate(model => getter(model));
            }
            set {
                if (value == null)
                {
                    this.column.AspectGetter = null;
                }
                else
                {
                    this.column.AspectGetter = delegate(object x) {
                        return x == null ? null : value((T)x);
                    };
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public TypedAspectPutterDelegate AspectPutter {
            get {
                AspectPutterDelegate putter = this.column.AspectPutter;
                return putter == null ? null : new TypedAspectPutterDelegate((model, value) => putter(model, value));
            }
            set {
                if (value == null)
                {
                    this.column.AspectPutter = null;
                }
                else
                {
                    this.column.AspectPutter = delegate(object x, object newValue) {
                        value((T)x, newValue);
                    };
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public TypedImageGetterDelegate ImageGetter {
            get {
                ImageGetterDelegate getter = this.column.ImageGetter;
                return getter == null ? null : new TypedImageGetterDelegate(model => getter(model));
            }
            set {
                if (value == null)
                {
                    this.column.ImageGetter = null;
                }
                else
                {
                    this.column.ImageGetter = delegate(object x) {
                        return value((T)x);
                    };
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public TypedGroupKeyGetterDelegate GroupKeyGetter {
            get {
                GroupKeyGetterDelegate getter = this.column.GroupKeyGetter;
                return getter == null ? null : new TypedGroupKeyGetterDelegate(model => getter(model));
            }
            set {
                if (value == null)
                {
                    this.column.GroupKeyGetter = null;
                }
                else
                {
                    this.column.GroupKeyGetter = delegate(object x) {
                        return value((T)x);
                    };
                }
            }
        }

        #region Dynamic methods

        /// <summary>
        /// Generate an aspect getter that does the same thing as the AspectName,
        /// except without using reflection.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If you have an AspectName of "Owner.Address.Postcode", this will generate
        /// the equivalent of: <code>this.AspectGetter = delegate (object x) {
        ///     return x.Owner.Address.Postcode;
        /// }
        /// </code>
        /// </para>
        /// <para>
        /// If AspectName is empty, this method will do nothing, otherwise 
        /// this will replace any existing AspectGetter.
        /// </para>
        /// </remarks>
        public void GenerateAspectGetter() {
            if (!String.IsNullOrEmpty(this.column.AspectName))
            {
                this.AspectGetter = GenerateAspectGetter(typeof(T), this.column.AspectName);
            }
        }

        /// <summary>
        /// Generates an aspect getter method dynamically. The method will execute
        /// the given dotted chain of selectors against a model object given at runtime.
        /// </summary>
        /// <param name="type">The type of model object to be passed to the generated method</param>
        /// <param name="path">A dotted chain of selectors. Each selector can be the name of a 
        /// field, property or parameter-less method.</param>
        /// <returns>A typed delegate</returns>
        private static TypedAspectGetterDelegate GenerateAspectGetter(Type type, string path) {
            DynamicMethod getter = new DynamicMethod(String.Empty,
                typeof(Object), new Type[] { type }, type, true);
            GenerateIL(type, path, getter.GetILGenerator());
            return (TypedAspectGetterDelegate)getter.CreateDelegate(typeof(TypedAspectGetterDelegate));
        }

        /// <summary>
        /// This method generates the actual IL for the method.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="path"></param>
        /// <param name="il"></param>
        private static void GenerateIL(Type type, string path, ILGenerator il) {
            // Push our model object onto the stack
            il.Emit(OpCodes.Ldarg_0);

            // Generate the IL to access each part of the dotted chain
            string[] parts = path.Split('.');
            for (int i = 0; i < parts.Length; i++) {
                type = GeneratePart(il, type, parts[i], (i == parts.Length - 1));
                if (type == null)
                {
                    break;
                }
            }

            // If the object to be returned is a value type (e.g. int, bool), it
            // must be boxed, since the delegate returns an Object
            if (type != null && type.IsValueType && !typeof(T).IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }

            il.Emit(OpCodes.Ret);
        }

        private static Type GeneratePart(ILGenerator il, Type type, string pathPart, bool isLastPart) {
            // TODO: Generate check for null

            // Find the first member with the given name that is a field, property, or parameter-less method
            List<MemberInfo> infos = new List<MemberInfo>(type.GetMember(pathPart));
            MemberInfo info = infos.Find(delegate(MemberInfo x) {
                if (x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property)
                {
                    return true;
                }

                if (x.MemberType == MemberTypes.Method)
                {
                    return ((MethodInfo)x).GetParameters().Length == 0;
                }
                else
                {
                    return false;
                }
            });

            // If we couldn't find anything with that name, pop the current result and return an error
            if (info == null) {
                il.Emit(OpCodes.Pop);
                if (Munger.IgnoreMissingAspects)
                {
                    il.Emit(OpCodes.Ldnull);
                }
                else
                {
                    il.Emit(OpCodes.Ldstr, String.Format(CultureInfo.CurrentCulture, "'{0}' is not a parameter-less method, property or field of type '{1}'", pathPart, type.FullName));
                }

                return null;
            }

            // Generate the correct IL to access the member. We remember the type of object that is going to be returned
            // so that we can do a method lookup on it at the next iteration
            Type resultType = null;
            switch (info.MemberType) {
                case MemberTypes.Method:
                    MethodInfo mi = (MethodInfo)info;
                    if (mi.IsVirtual)
                    {
                        il.Emit(OpCodes.Callvirt, mi);
                    }
                    else
                    {
                        il.Emit(OpCodes.Call, mi);
                    }

                    resultType = mi.ReturnType;
                    break;
                case MemberTypes.Property:
                    PropertyInfo pi = (PropertyInfo)info;
                    il.Emit(OpCodes.Call, pi.GetGetMethod());
                    resultType = pi.PropertyType;
                    break;
                case MemberTypes.Field:
                    FieldInfo fi = (FieldInfo)info;
                    il.Emit(OpCodes.Ldfld, fi);
                    resultType = fi.FieldType;
                    break;
            }

            // If the method returned a value type, and something is going to call a method on that value,
            // we need to load its address onto the stack, rather than the object itself.
            if (resultType.IsValueType && !isLastPart) {
                LocalBuilder lb = il.DeclareLocal(resultType);
                il.Emit(OpCodes.Stloc, lb);
                il.Emit(OpCodes.Ldloca, lb);
            }

            return resultType;
        }

        #endregion
    }
}
