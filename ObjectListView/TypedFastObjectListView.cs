/*
 * FastObjectListView - A listview that behaves like an ObjectListView but has the speed of a virtual list
 *
 * Author: Phillip Piper
 * Date: 27/09/2008 9:15 AM
 *
 * Change log:
 * 2014-10-15   JPP  - Fire Filter event when applying filters
 * v2.8
 * 2012-06-11   JPP  - Added more efficient version of FilteredObjects
 * v2.5.1
 * 2011-04-25   JPP  - Fixed problem with removing objects from filtered or sorted list
 * v2.4
 * 2010-04-05   JPP  - Added filtering
 * v2.3
 * 2009-08-27   JPP  - Added GroupingStrategy
 *                   - Added optimized Objects property
 * v2.2.1
 * 2009-01-07   JPP  - Made all public and protected methods virtual
 * 2008-09-27   JPP  - Separated from ObjectListView.cs
 *
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
using System.Windows.Forms;

namespace BrightIdeasSoftware
{
    /// <summary>
    /// A FastObjectListView trades function for speed.
    /// </summary>
    /// <remarks>
    /// <para>On my mid-range laptop, this view builds a list of 10,000 objects in 0.1 seconds,
    /// as opposed to a normal ObjectListView which takes 10-15 seconds. Lists of up to 50,000 items should be
    /// able to be handled with sub-second response times even on low end machines.</para>
    /// <para>
    /// A FastObjectListView is implemented as a virtual list with many of the virtual modes limits (e.g. no sorting)
    /// fixed through coding. There are some functions that simply cannot be provided. Specifically, a FastObjectListView cannot:
    /// <list type="bullet">
    /// <item><description>use Tile view</description></item>
    /// <item><description>show groups on XP</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class TypedFastObjectListView<T> : TypedVirtualObjectListView<T> where T : class
    {
        /// <summary>
        /// Make a FastObjectListView
        /// </summary>
        public TypedFastObjectListView() {
            this.VirtualListDataSource = this.CreateDataSource();
            this.GroupingStrategy = new FastListGroupingStrategy();
        }

        /// <summary>Create the fast source owned by this control.</summary>
        protected virtual FastObjectListDataSource<T> CreateDataSource() {
            return new FastObjectListDataSource<T>(this);
        }

        /// <summary>
        /// Gets the collection of objects that survive any filtering that may be in place.
        /// </summary>
        [Browsable(false),
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override IEnumerable FilteredObjects {
            get {
                // This is much faster than the base method
                return ((FastObjectListDataSource<T>)this.VirtualListDataSource).FilteredObjectList;
            }
        }

        /// <inheritdoc/>
        protected override IEnumerable GetModelObjectsCore() {
            return ((FastObjectListDataSource<T>)this.VirtualListDataSource).ObjectList.AsReadOnly();
        }

        /// <summary>
        /// Move the given collection of objects to the given index.
        /// </summary>
        /// <remarks>This operation only makes sense on non-grouped ObjectListViews.</remarks>
        /// <param name="index"></param>
        /// <param name="modelObjects"></param>
        public override void MoveObjects(int index, ICollection modelObjects) {
            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate() { this.MoveObjects(index, modelObjects); });
                return;
            }

            // If any object that is going to be moved is before the point where the insertion 
            // will occur, then we have to reduce the location of our insertion point
            int displacedObjectCount = 0;
            foreach (object modelObject in modelObjects) {
                int i = this.IndexOf(modelObject);
                if (i >= 0 && i < index)
                {
                    displacedObjectCount++;
                }
            }
            index -= displacedObjectCount;

            this.BeginUpdate();
            try {
                this.RemoveObjects(modelObjects);
                this.InsertObjects(index, modelObjects);
            }
            finally {
                this.EndUpdate();
            }
        }

        /// <summary>
        /// Remove any sorting and revert to the given order of the model objects
        /// </summary>
        /// <remarks>To be really honest, Unsort() doesn't work on FastObjectListViews since
        /// the original ordering of model objects is lost when Sort() is called. So this method
        /// effectively just turns off sorting.</remarks>
        public override void Unsort() {
            base.Unsort();
            this.SetObjects(this.Objects);
        }
    }


}
