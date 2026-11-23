using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace BrightIdeasSoftware
{
    internal sealed class ModelRowCollection<T> : IReadOnlyList<T> where T : class {
        private readonly ObjectListViewBase listView;
        private readonly bool customDisplayOrder;
        private IList<OLVGroup> indexedGroups;
        private int indexedVersion;
        private int[] groupEnds;

        internal ModelRowCollection(ObjectListViewBase listView) {
            this.listView = listView;
            Type implementation = listView.GetType().GetMethod(nameof(ObjectListViewBase.GetNthItemInDisplayOrder),
                new[] { typeof(int) }).DeclaringType;
            this.customDisplayOrder = implementation != typeof(ObjectListViewBase) &&
                (!implementation.IsGenericType || implementation.GetGenericTypeDefinition() != typeof(TypedVirtualObjectListView<>));
        }

        public int Count {
            get {
                if (this.listView.ModelRowGroupingStrategy == null)
                    return this.listView.GetItemCount();

                this.EnsureGroupEnds();
                return this.groupEnds[this.groupEnds.Length - 1];
            }
        }

        public T this[int index] {
            get {
                if (index < 0 || index >= this.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                IVirtualGroups strategy = this.listView.ModelRowGroupingStrategy;
                if (this.customDisplayOrder && this.listView.ShowGroups &&
                    (this.listView.Groups.Count > 0 || this.listView.OLVGroups?.Count > 0)) {
                    index = this.listView.GetNthItemInDisplayOrder(index).Index;
                } else if (strategy != null) {
                    this.EnsureGroupEnds();
                    int first = 0;
                    int last = this.groupEnds.Length;
                    while (first < last) {
                        int middle = first + (last - first) / 2;
                        if (this.groupEnds[middle] <= index)
                            first = middle + 1;
                        else
                            last = middle;
                    }
                    if (first == this.groupEnds.Length)
                        throw new InvalidOperationException("The accepted groups do not contain the requested row.");
                    int offset = index - (first == 0 ? 0 : this.groupEnds[first - 1]);
                    index = strategy.GetGroupMember(this.indexedGroups[first], offset);
                } else if (!this.listView.VirtualMode && this.listView.ShowGroups && this.listView.Groups.Count > 0) {
                    index = this.listView.GetNthItemInDisplayOrder(index).Index;
                }

                return (T)this.listView.GetModelAt(index);
            }
        }

        public IEnumerator<T> GetEnumerator() {
            int version = this.listView.ModelRowsVersion;
            IVirtualGroups strategy = this.listView.ModelRowGroupingStrategy;
            if (!this.customDisplayOrder && strategy != null)
                return this.EnumerateVirtualGroups(version, strategy).GetEnumerator();
            if (!this.customDisplayOrder && !this.listView.VirtualMode &&
                this.listView.ShowGroups && this.listView.Groups.Count > 0)
                return this.EnumerateNativeGroups(version).GetEnumerator();
            return this.EnumerateFlat(version).GetEnumerator();
        }

        private IEnumerable<T> EnumerateVirtualGroups(int version, IVirtualGroups strategy) {
            this.CheckVersion(version);
            foreach (OLVGroup group in this.listView.OLVGroups) {
                for (int i = 0; i < group.VirtualItemCount; i++) {
                    this.CheckVersion(version);
                    yield return (T)this.listView.GetModelAt(strategy.GetGroupMember(group, i));
                }
            }
            this.CheckVersion(version);
        }

        private IEnumerable<T> EnumerateNativeGroups(int version) {
            this.CheckVersion(version);
            if (this.listView.GetDefaultGroupItemCount() > 0) {
                foreach (OLVListItem item in this.listView.GetDefaultGroupItems()) {
                    this.CheckVersion(version);
                    yield return (T)this.listView.GetModelAt(item.Index);
                }
            }
            foreach (ListViewGroup group in this.listView.Groups) {
                for (int i = 0; i < group.Items.Count; i++) {
                    this.CheckVersion(version);
                    yield return (T)this.listView.GetModelAt(group.Items[i].Index);
                }
            }
            this.CheckVersion(version);
        }

        private IEnumerable<T> EnumerateFlat(int version) {
            for (int i = 0; ; i++) {
                this.CheckVersion(version);

                if (i >= this.Count)
                {
                    yield break;
                }

                yield return this.customDisplayOrder ? this[i] : (T)this.listView.GetModelAt(i);
            }
        }

        private void CheckVersion(int version) {
            if (version != this.listView.ModelRowsVersion)
                throw new InvalidOperationException("The displayed rows changed during enumeration.");
        }

        private void EnsureGroupEnds() {
            IList<OLVGroup> groups = this.listView.OLVGroups;
            if (ReferenceEquals(groups, this.indexedGroups) && this.indexedVersion == this.listView.ModelRowsVersion)
                return;
            int[] ends = new int[groups.Count];
            int count = 0;
            for (int i = 0; i < groups.Count; i++) {
                count += groups[i].VirtualItemCount;
                ends[i] = count;
            }
            this.groupEnds = ends;
            this.indexedGroups = groups;
            this.indexedVersion = this.listView.ModelRowsVersion;
        }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }
}
