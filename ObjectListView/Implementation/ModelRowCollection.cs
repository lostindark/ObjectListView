using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace BrightIdeasSoftware
{
    internal sealed class ModelRowCollection<T> : IReadOnlyList<T> where T : class {
        private readonly ObjectListViewBase listView;

        internal ModelRowCollection(ObjectListViewBase listView) {
            this.listView = listView;
        }

        public int Count { get { return this.listView.GetItemCount(); } }

        public T this[int index] {
            get {
                if (index < 0 || index >= this.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (this.listView.ShowGroups && (this.listView.Groups.Count > 0 ||
                    (this.listView.OLVGroups != null && this.listView.OLVGroups.Count > 0)))
                {
                    index = this.listView.GetNthItemInDisplayOrder(index).Index;
                }

                return (T)this.listView.GetModelAt(index);
            }
        }

        public IEnumerator<T> GetEnumerator() {
            return this.Enumerate(this.listView.ModelRowsVersion).GetEnumerator();
        }

        private IEnumerable<T> Enumerate(int version) {
            for (int i = 0; ; i++) {
                if (version != this.listView.ModelRowsVersion)
                {
                    throw new InvalidOperationException("The displayed rows changed during enumeration.");
                }

                if (i >= this.Count)
                {
                    yield break;
                }

                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }
}
