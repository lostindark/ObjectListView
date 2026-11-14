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
using System.Windows.Forms;

namespace BrightIdeasSoftware
{
    /// <summary>
    /// Provide a data source for a FastObjectListView
    /// </summary>
    /// <remarks>
    /// This class isn't intended to be used directly, but it is left as a public
    /// class just in case someone wants to subclass it.
    /// </remarks>
    public class FastObjectListDataSource<T> : AbstractVirtualListDataSource where T : class
    {
        /// <summary>
        /// Create a FastObjectListDataSource
        /// </summary>
        /// <param name="listView"></param>
        public FastObjectListDataSource(ObjectListViewBase listView)
            : base(listView) {
        }

        #region IVirtualListDataSource Members

        /// <summary>
        /// Get n'th object
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public override object GetNthObject(int n) {
            if (n >= 0 && n < this.filteredObjectList.Count)
            {
                return this.filteredObjectList[n];
            }

            return null;
        }

        /// <summary>
        /// How many items are in the data source
        /// </summary>
        /// <returns></returns>
        public override int GetObjectCount() {
            return this.filteredObjectList.Count;
        }

        /// <summary>
        /// Get the index of the given model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public override int GetObjectIndex(object model) {
            int index;

            if (model != null && this.objectsToIndexMap.TryGetValue(model, out index))
            {
                return index;
            }

            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public override int SearchText(string value, int first, int last, OLVColumn column) {
            if (first <= last) {
                for (int i = first; i <= last; i++) {
                    string data = column.GetStringValue(this.listView.GetNthItemInDisplayOrder(i).RowObject);
                    if (data.StartsWith(value, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return i;
                    }
                }
            } else {
                for (int i = first; i >= last; i--) {
                    string data = column.GetStringValue(this.listView.GetNthItemInDisplayOrder(i).RowObject);
                    if (data.StartsWith(value, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="column"></param>
        /// <param name="order"></param>
        public override void Sort(OLVColumn column, SortOrder order) {
            if (order != SortOrder.None) {
                ModelObjectComparer comparer = this.listView.CreateModelObjectComparer(column, order);
                this.fullObjectList.Sort(comparer);
                this.filteredObjectList.Sort(comparer);
            }
            this.RebuildIndexMap();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelObjects"></param>
        public override void AddObjects(IEnumerable modelObjects) {
            List<T> snapshot = TypedObjectListView<T>.CreateModelList(modelObjects);
            foreach (T modelObject in snapshot) {
                if (modelObject != null)
                {
                    this.fullObjectList.Add(modelObject);
                }
            }
            this.FilterObjects();
            this.RebuildIndexMap();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="modelObjects"></param>
        public override void InsertObjects(int index, IEnumerable modelObjects) {
            List<T> snapshot = TypedObjectListView<T>.CreateModelList(modelObjects);
            this.fullObjectList.InsertRange(index, snapshot);
            this.FilterObjects();
            this.RebuildIndexMap();
        }

        /// <summary>
        /// Remove the given collection of models from this source.
        /// </summary>
        /// <param name="modelObjects"></param>
        public override void RemoveObjects(IEnumerable modelObjects) {
            List<T> snapshot = TypedObjectListView<T>.CreateModelList(modelObjects);

            // We have to unselect any object that is about to be deleted
            List<int> indicesToRemove = new List<int>();
            foreach (object modelObject in snapshot) {
                int i = this.GetObjectIndex(modelObject);
                if (i >= 0)
                {
                    indicesToRemove.Add(i);
                }
            }

            // Sort the indices from highest to lowest so that we
            // remove latter ones before earlier ones. In this way, the
            // indices of the rows doesn't change after the deletes.
            indicesToRemove.Sort();
            indicesToRemove.Reverse();

            foreach (int i in indicesToRemove)
            {
                this.listView.SelectedIndices.Remove(i);
            }

            // Remove the objects from the unfiltered list
            foreach (T modelObject in snapshot)
            {
                int index = this.fullObjectList.FindIndex(model => Object.Equals(model, modelObject));
                if (index >= 0)
                {
                    this.fullObjectList.RemoveAt(index);
                }
            }

            this.FilterObjects();
            this.RebuildIndexMap();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="collection"></param>
        public override void SetObjects(IEnumerable collection) {
            List<T> newObjects = TypedObjectListView<T>.CreateModelList(collection);

            this.fullObjectList = newObjects;
            this.FilterObjects();
            this.RebuildIndexMap();
        }

        /// <summary>
        /// Update/replace the nth object with the given object
        /// </summary>
        /// <param name="index"></param>
        /// <param name="modelObject"></param>
        public override void UpdateObject(int index, object modelObject) {
            T replacement = (T)modelObject;
            if (index < 0 || index >= this.filteredObjectList.Count)
            {
                return;
            }

            int i = this.fullObjectList.FindIndex(model => Object.Equals(model, this.filteredObjectList[index]));
            if (i < 0)
            {
                return;
            }

            if (ReferenceEquals(this.fullObjectList[i], modelObject))
            {
                return;
            }

            this.fullObjectList[i] = replacement;
            this.filteredObjectList[index] = replacement;
            this.objectsToIndexMap[modelObject] = index;
        }

        private List<T> fullObjectList = new List<T>();
        private List<T> filteredObjectList = new List<T>();
        private IModelFilter modelFilter;
        private IListFilter listFilter;

        #endregion

        #region IFilterableDataSource Members

        /// <summary>
        /// Apply the given filters to this data source. One or both may be null.
        /// </summary>
        /// <param name="modelFilter"></param>
        /// <param name="listFilter"></param>
        public override void ApplyFilters(IModelFilter modelFilter, IListFilter listFilter) {
            this.modelFilter = modelFilter;
            this.listFilter = listFilter;
            this.SetObjects(this.fullObjectList);
        }

        #endregion

        #region Implementation

        /// <summary>
        /// Gets the full list of objects being used for this fast list. 
        /// This list is unfiltered.
        /// </summary>
        public List<T> ObjectList {
            get { return fullObjectList; }
        }

        /// <summary>
        /// Gets the list of objects from ObjectList which survive any installed filters.
        /// </summary>
        public List<T> FilteredObjectList {
            get { return filteredObjectList; }
        }

        /// <summary>
        /// Rebuild the map that remembers which model object is displayed at which line
        /// </summary>
        protected void RebuildIndexMap() {
            this.objectsToIndexMap.Clear();
            for (int i = 0; i < this.filteredObjectList.Count; i++)
            {
                this.objectsToIndexMap[this.filteredObjectList[i]] = i;
            }
        }
        readonly Dictionary<Object, int> objectsToIndexMap = new Dictionary<Object, int>();

        /// <summary>
        /// Build our filtered list from our full list.
        /// </summary>
        protected void FilterObjects() {

            // If this list isn't filtered, we don't need to do anything else
            if (!this.listView.UseFiltering) {
                this.filteredObjectList = new List<T>(this.fullObjectList);
                return;
            }

            // Tell the world to filter the objects. If they do so, don't do anything else
            // ReSharper disable PossibleMultipleEnumeration
            FilterEventArgs args = new FilterEventArgs(this.fullObjectList);
            this.listView.OnFilter(args);
            if (args.FilteredObjects != null) {
                this.filteredObjectList = TypedObjectListView<T>.CreateModelList(args.FilteredObjects);
                return;
            }

            IEnumerable objects = (this.listFilter == null) ?
                this.fullObjectList : this.listFilter.Filter(this.fullObjectList);

            // Apply the object filter if there is one
            if (this.modelFilter == null) {
                this.filteredObjectList = TypedObjectListView<T>.CreateModelList(objects);
            } else {
                this.filteredObjectList = new List<T>();
                foreach (T model in TypedObjectListView<T>.CreateModelList(objects)) {
                    if (this.modelFilter.Filter(model))
                    {
                        this.filteredObjectList.Add(model);
                    }
                }
            }
        }

        #endregion
    }
}
