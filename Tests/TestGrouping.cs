using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests {
    [STATestClass]
    public class TestGrouping : WinFormsTestFixture {
        [TestMethod]
        public void FastGroupsPreserveIndicesForEqualAndRepeatedModels() {
            using (var list = new TypedFastObjectListView<EqualRow> { View = View.Details, ShowGroups = false }) {
                var column = new OLVColumn("Group", "") { AspectGetter = x => ((EqualRow)x).Group };
                list.AllColumns.Add(column);
                list.Columns.Add(column);
                mainForm.Controls.Add(list);
                list.CreateControl();
                var first = new EqualRow("B");
                var second = new EqualRow("A");
                list.Objects = new[] { first, second, first };
                list.ShowGroups = true;

                list.Grouping = GroupingConfiguration.Fixed(column, SortOrder.Ascending);
                list.BuildGroups();

                int[] groupA = { 1 };
                int[] groupB = { 0, 2 };
                int[] expectedGroupByIndex = { 1, 0, 1 };
                CollectionAssert.AreEqual(groupA, list.OLVGroups[0].Contents.Cast<int>().ToArray());
                CollectionAssert.AreEqual(groupB, list.OLVGroups[1].Contents.Cast<int>().ToArray());
                CollectionAssert.AreEqual(expectedGroupByIndex, Enumerable.Range(0, 3)
                    .Select(list.GroupingStrategy.GetGroup).ToArray());
                Assert.AreSame(second, list.Rows[0]);
                Assert.AreSame(first, list.Rows[1]);
                Assert.AreSame(first, list.Rows[2]);
            }
        }

        [TestMethod]
        public void RegularGroupedRowsIncludeRowsInTheDefaultGroup() {
            using (TypedObjectListView<Row> list = this.CreateList(false)) {
                list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(1), SortOrder.None);
                list.AboutToCreateGroups += (_, args) => args.Groups.RemoveAt(0);
                list.BuildGroups();

                Assert.AreEqual(1, list.OLVGroups.Count);
                Assert.AreEqual(4, list.Rows.Count);
                Row[] expected = Enumerable.Range(0, list.GetItemCount())
                    .OrderBy(index => list.GetItemRect(index).Top)
                    .Select(list.GetModelObject).ToArray();
                CollectionAssert.AreEqual(expected, list.Rows.ToArray());
                CollectionAssert.AreEqual(expected, Enumerable.Range(0, list.Rows.Count)
                    .Select(index => list.Rows[index]).ToArray());
                AssertNativeOrder(list);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FixedGroupingKeepsItemSortingIndependent(bool useVirtual) {
            TypedObjectListView<Row> list = this.CreateList(useVirtual);
            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(1), SortOrder.Ascending);
            list.Sort(list.GetColumn(2), SortOrder.Descending);

            Assert.AreEqual("alpha,charlie,delta,bravo", Names(list));
            Assert.IsNull(list.OLVGroups[0].Key);
            AssertNativeOrder(list);
            list.Sort(list.GetColumn(2), SortOrder.Ascending);
            Assert.AreEqual("charlie,alpha,bravo,delta", Names(list));
            Assert.IsNull(list.OLVGroups[0].Key);
            AssertNativeOrder(list);

            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(1));
            list.Sort(list.GetColumn(2), SortOrder.Descending);
            Assert.AreEqual("delta,bravo,alpha,charlie", Names(list));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FollowSortingSupportsAnIndependentGroupDirection(bool useVirtual) {
            TypedObjectListView<Row> list = this.CreateList(useVirtual);
            list.Sort(list.GetColumn(2), SortOrder.Descending);
            Assert.AreEqual(4, list.OLVGroups[0].Key);
            Assert.AreSame(list.GetColumn(2), list.OLVGroups[0].Column);

            list.Grouping = GroupingConfiguration.FollowSorting(SortOrder.Ascending);
            list.Sort(list.GetColumn(2), SortOrder.Descending);
            Assert.AreEqual(1, list.OLVGroups[0].Key);
            Assert.AreEqual(SortOrder.Descending, list.PrimarySortOrder);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void GroupsWithoutItemSortingPreserveDiscoveryOrderIncludingNull(bool useVirtual) {
            TypedObjectListView<Row> list = this.CreateList(useVirtual);
            list.GetColumn(1).Sortable = false;
            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(1), SortOrder.None);
            list.BuildGroups();

            Assert.AreEqual("B", list.OLVGroups[0].Key);
            Assert.IsNull(list.OLVGroups[1].Key);
            Assert.AreEqual("delta,bravo,charlie,alpha", Names(list));
            Assert.AreEqual(0, list.SortDescriptions.Count);

            list.Sort(list.GetColumn(2), SortOrder.Descending);
            list.Sort(Array.Empty<OLVSortDescription>());
            Assert.IsTrue(list.ShowGroups);
            Assert.AreEqual(0, list.SortDescriptions.Count);
            Assert.AreEqual("delta,bravo,charlie,alpha", Names(list));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void NonGroupableSortColumnsKeepGroupingOrUseAValidFallback(bool useVirtual) {
            TypedObjectListView<Row> list = this.CreateList(useVirtual);
            list.Sort(list.GetColumn(1), SortOrder.Ascending);
            list.GetColumn(2).Groupable = false;
            list.Sort(list.GetColumn(2), SortOrder.Descending);
            Assert.AreSame(list.GetColumn(1), list.OLVGroups[0].Column);
            Assert.IsNull(list.OLVGroups[0].Key);
            Assert.AreEqual("alpha,charlie,delta,bravo", Names(list));

            list.GetColumn(1).Groupable = false;
            list.Sort();
            Assert.AreSame(list.GetColumn(0), list.OLVGroups[0].Column);

            list.GetColumn(0).Groupable = false;
            list.Sort();
            Assert.AreEqual(0, list.OLVGroups.Count);
            Assert.AreEqual("alpha,charlie,delta,bravo", Names(list));
            Assert.AreSame(list.GetItem(0), list.GetNextItem(null));
            Assert.AreSame(list.GetItem(3), list.GetLastItemInDisplayOrder());
            Assert.AreEqual(0, list.GetDisplayOrderOfItemIndex(0));
        }

        [TestMethod]
        [DataRow(false, 0)]
        [DataRow(false, 1)]
        [DataRow(false, 2)]
        [DataRow(true, 0)]
        [DataRow(true, 1)]
        [DataRow(true, 2)]
        public void CancellationDoesNotPublishGroupsSortingOrVirtualMappings(bool useVirtual, int stage) {
            TypedObjectListView<Row> list = this.CreateList(useVirtual);
            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(1), SortOrder.Ascending);
            list.Sort(list.GetColumn(2), SortOrder.Descending);
            list.SelectedObject = list.Objects.First();
            object selected = list.SelectedObject;
            IList<OLVGroup> originalGroups = list.OLVGroups;
            string originalRows = Names(list);
            TypedVirtualObjectListView<Row> virtualList = list as TypedVirtualObjectListView<Row>;
            int[] originalMap = virtualList == null ? null : Enumerable.Range(0, list.GetItemCount())
                .Select(virtualList.GroupingStrategy.GetGroup).ToArray();
            int completed = 0;
            list.AfterSorting += delegate { completed++; };
            list.AfterCreatingGroups += delegate { completed++; };
            list.BeforeSorting += delegate(object sender, BeforeSortingEventArgs args) {
                args.Canceled = stage == 0;
            };
            list.BeforeCreatingGroups += delegate(object sender, CreateGroupsEventArgs args) {
                args.Canceled = stage == 1;
            };
            list.AboutToCreateGroups += delegate(object sender, CreateGroupsEventArgs args) {
                args.Canceled = stage == 2;
            };

            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(0), SortOrder.Descending);
            list.Sort(list.GetColumn(2), SortOrder.Ascending);

            Assert.AreSame(originalGroups, list.OLVGroups);
            Assert.AreEqual(originalRows, Names(list));
            Assert.AreSame(list.GetColumn(2), list.PrimarySortColumn);
            Assert.AreEqual(SortOrder.Descending, list.PrimarySortOrder);
            Assert.AreSame(selected, list.SelectedObject);
            Assert.AreEqual(0, completed);
            if (virtualList != null)
            {
                CollectionAssert.AreEqual(originalMap, Enumerable.Range(0, list.GetItemCount())
                    .Select(virtualList.GroupingStrategy.GetGroup).ToArray());
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void GroupingEventsCanOverrideBothComparersWithoutItemSortFields(bool useVirtual) {
            TypedObjectListView<Row> list = this.CreateList(useVirtual);
            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(1), SortOrder.None);
            list.BeforeCreatingGroups += delegate(object sender, CreateGroupsEventArgs args) {
                args.Parameters.GroupComparer = new OLVGroupComparer(SortOrder.Ascending);
                args.Parameters.ModelComparer = Comparer<object>.Create((x, y) => ((Row)y).Value.CompareTo(((Row)x).Value));
            };
            list.BuildGroups();

            Assert.IsNull(list.OLVGroups[0].Key);
            Assert.AreEqual("alpha,charlie,delta,bravo", Names(list));
            Assert.AreEqual(0, list.SortDescriptions.Count);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SortingEventChangesAreResolvedWithoutOverwritingExplicitGroupingOverrides(bool useVirtual) {
            TypedObjectListView<Row> list = this.CreateList(useVirtual);
            EventHandler<BeforeSortingEventArgs> changeSort = delegate(object sender, BeforeSortingEventArgs args) {
                args.ColumnToSort = list.GetColumn(2);
                args.SortOrder = SortOrder.Descending;
            };
            list.BeforeSorting += changeSort;
            list.Sort(list.GetColumn(0), SortOrder.Ascending);
            Assert.AreEqual(4, list.OLVGroups[0].Key);

            EventHandler<BeforeSortingEventArgs> changeGroup = delegate(object sender, BeforeSortingEventArgs args) {
                args.ColumnToGroupBy = list.GetColumn(1);
                args.GroupByOrder = SortOrder.Ascending;
            };
            list.BeforeSorting += changeGroup;
            list.Sort();
            Assert.AreSame(list.GetColumn(1), list.OLVGroups[0].Column);
            Assert.IsNull(list.OLVGroups[0].Key);
            list.BeforeSorting -= changeGroup;
            list.BeforeSorting -= changeSort;
            list.Sort();
            Assert.AreSame(list.GetColumn(2), list.OLVGroups[0].Column);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CollapsedStateSurvivesRebuildsButNotGroupingColumnChanges(bool useVirtual) {
            TypedObjectListView<Row> list = this.CreateList(useVirtual);
            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(1), SortOrder.Ascending);
            list.Sort(list.GetColumn(2), SortOrder.Descending);
            list.OLVGroups[0].Collapsed = true;
            list.BuildList();
            Assert.IsNull(list.OLVGroups[0].Key);
            Assert.IsTrue(list.OLVGroups[0].Collapsed);
            list.Sort(list.GetColumn(2), SortOrder.Ascending);
            Assert.IsTrue(list.OLVGroups[0].Collapsed);
            OLVGroup[] collapsed = list.CollapsedGroups.ToArray();
            list.CollapsedGroups = null;
            Assert.IsFalse(list.OLVGroups[0].Collapsed);
            list.CollapsedGroups = collapsed;
            Assert.IsTrue(list.OLVGroups[0].Collapsed);

            list.GetColumn(0).GroupKeyGetter = x => ((Row)x).Group;
            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(0), SortOrder.Ascending);
            list.BuildGroups();
            Assert.IsNull(list.OLVGroups[0].Key);
            Assert.IsFalse(list.OLVGroups[0].Collapsed);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DeprecatedSettingsAdaptToTheNewPolicyAndRetainExplicitColumnZeroOrdering(bool useVirtual) {
            TypedObjectListView<Row> list = this.CreateList(useVirtual);
#pragma warning disable CS0618
            list.AlwaysGroupBySortOrder = SortOrder.Descending;
            list.AlwaysGroupByColumn = list.GetColumn(1);
            list.Sort(list.GetColumn(2), SortOrder.Ascending);
            Assert.AreEqual("bravo,delta,charlie,alpha", Names(list));
            list.AlwaysGroupBySortOrder = SortOrder.None;
            list.Sort();
            Assert.IsNull(list.OLVGroups[0].Key);
            list.AlwaysGroupByColumn = null;
            list.SortGroupItemsByPrimaryColumn = true;
            list.Sort(list.GetColumn(1), SortOrder.Ascending);
            Assert.AreEqual("alpha,charlie,bravo,delta", Names(list));
            list.SortGroupItemsByPrimaryColumn = false;
            list.Sort();
            Assert.AreEqual("charlie,alpha,delta,bravo", Names(list));

            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(1), SortOrder.Descending);
            Assert.AreSame(list.GetColumn(1), list.AlwaysGroupByColumn);
            Assert.AreEqual(SortOrder.Descending, list.AlwaysGroupBySortOrder);
#pragma warning restore CS0618
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void GroupingMenuDoesNotChangeItemSorting(bool useVirtual) {
            TypedObjectListView<Row> list = this.CreateList(useVirtual);
            list.Sort(list.GetColumn(2), SortOrder.Descending);
            using (ContextMenuStrip menu = new ContextMenuStrip()) {
                list.MakeColumnCommandMenu(menu, 1);
                menu.Items.Cast<ToolStripItem>().Single(item => item.Text == "Group by 'Group'").PerformClick();
            }
            Assert.AreSame(list.GetColumn(1), list.Grouping.Column);
            Assert.AreSame(list.GetColumn(2), list.PrimarySortColumn);
            Assert.AreEqual(SortOrder.Descending, list.PrimarySortOrder);
            using (ContextMenuStrip menu = new ContextMenuStrip()) {
                list.MakeColumnCommandMenu(menu, 1);
                menu.Items.Cast<ToolStripItem>().Single(item => item.Text == "Unlock grouping from 'Group'").PerformClick();
            }
            Assert.IsNull(list.Grouping.Column);
            Assert.AreSame(list.GetColumn(2), list.OLVGroups[0].Column);
        }

        [TestMethod]
        public void FixedGroupingValidatesOwnershipAndAllowsHiddenColumns() {
            TypedObjectListView<Row> list = this.CreateList(false);
            Assert.ThrowsExactly<ArgumentException>(() => list.Grouping = GroupingConfiguration.Fixed(new OLVColumn()));
            list.GetColumn(2).Groupable = false;
            Assert.ThrowsExactly<ArgumentException>(() => list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(2)));
            OLVColumn hidden = list.GetColumn(1);
            list.Columns.Remove(hidden);
            list.Grouping = GroupingConfiguration.Fixed(hidden, SortOrder.Ascending);
            list.BuildGroups();
            Assert.AreEqual(2, list.OLVGroups.Count);
            list.AllColumns.Remove(hidden);
            Assert.ThrowsExactly<ArgumentException>(() => list.BuildGroups());
        }

        [TestMethod]
        public void CustomVirtualStrategyUsesAcceptedGroupsAfterCancellation() {
            TypedFastObjectListView<Row> list = (TypedFastObjectListView<Row>)this.CreateList(true);
            ReverseGroups strategy = new ReverseGroups();
            list.GroupingStrategy = strategy;
            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(1), SortOrder.Ascending);
            list.BuildGroups();
            IList<OLVGroup> accepted = list.OLVGroups;
            Assert.AreEqual("B", list.OLVGroups[0].Key);
            list.AboutToCreateGroups += delegate(object sender, CreateGroupsEventArgs args) { args.Canceled = true; };
            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(0), SortOrder.Descending);
            list.BuildGroups();
            Assert.AreEqual(0, strategy.GetGroup(0));
            Assert.AreEqual(1, strategy.GetGroup(1));
            Assert.AreSame(accepted, list.OLVGroups);
            Assert.AreEqual(2, list.OLVGroups.Count);
            Assert.AreEqual("B", list.OLVGroups[0].Key);
        }

        [TestMethod]
        public void BuiltInVirtualLookupMapsFollowAcceptedGroupsAcrossCancellationAndRebuild() {
            TypedFastObjectListView<Row> list = (TypedFastObjectListView<Row>)this.CreateList(true);
            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(1), SortOrder.Ascending);
            list.BuildGroups();
            AssertGroupLookupMaps(list);
            IList<OLVGroup> accepted = list.OLVGroups;
            EventHandler<CreateGroupsEventArgs> cancel = (sender, args) => args.Canceled = true;
            list.AboutToCreateGroups += cancel;
            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(0), SortOrder.Descending);
            list.BuildGroups();
            Assert.AreSame(accepted, list.OLVGroups);
            AssertGroupLookupMaps(list);
            list.AboutToCreateGroups -= cancel;
            list.BuildGroups();
            Assert.AreNotSame(accepted, list.OLVGroups);
            AssertGroupLookupMaps(list);
            OLVGroup detached = new OLVGroup("detached") { Contents = new List<int> { 1, 1, 0 } };
            FastListGroupingStrategy unbound = new FastListGroupingStrategy();
            Assert.AreEqual(0, unbound.GetIndexWithinGroup(detached, 1));
            Assert.AreEqual(0, list.GroupingStrategy.GetIndexWithinGroup(detached, 1));
            Assert.AreEqual(-1, list.GroupingStrategy.GetIndexWithinGroup(detached, -1));
        }

        private static void AssertGroupLookupMaps(TypedFastObjectListView<Row> list) {
            for (int i = 0; i < list.OLVGroups.Count; i++) {
                OLVGroup group = list.OLVGroups[i];
                int[] members = group.Contents.Cast<int>().ToArray();
                for (int j = 0; j < members.Length; j++) {
                    Assert.AreEqual(i, list.GroupingStrategy.GetGroup(members[j]));
                    Assert.AreEqual(Array.IndexOf(members, members[j]),
                        list.GroupingStrategy.GetIndexWithinGroup(group, members[j]));
                }
            }
        }

        [TestMethod]
        public void LegacyGroupingOverrideParticipatesInCancellationAndRetry() {
            HookedListView list = new HookedListView();
            this.CreateList(list);
            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(1), SortOrder.Ascending);
            EventHandler<CreateGroupsEventArgs> cancel = delegate(object sender, CreateGroupsEventArgs args) {
                args.Canceled = true;
            };
            list.BeforeCreatingGroups += cancel;
            list.Sort(list.GetColumn(2), SortOrder.Descending);
            Assert.AreEqual(1, list.BuildCalls);
            Assert.AreEqual(0, list.SortDescriptions.Count);
            Assert.AreEqual("delta,charlie,bravo,alpha", Names(list));

            list.BeforeCreatingGroups -= cancel;
            list.Sort(list.GetColumn(2), SortOrder.Descending);
            Assert.AreEqual(2, list.BuildCalls);
            Assert.AreEqual(SortOrder.Descending, list.PrimarySortOrder);
            Assert.AreEqual("alpha,charlie,delta,bravo", Names(list));
        }

        [TestMethod]
        public void CanceledNestedPreparationDoesNotCancelTheAcceptedOuterOperation() {
            TypedObjectListView<Row> list = this.CreateList(false);
            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(1), SortOrder.Ascending);
            bool nested = false;
            list.BeforeCreatingGroups += delegate(object sender, CreateGroupsEventArgs args) {
                if (nested) {
                    args.Canceled = true;
                    return;
                }
                nested = true;
                try {
                    list.BuildGroups(list.GetColumn(0), SortOrder.Ascending, Array.Empty<OLVSortDescription>());
                } finally {
                    nested = false;
                }
            };
            int completed = 0;
            list.AfterSorting += delegate { completed++; };
            list.Sort(list.GetColumn(2), SortOrder.Descending);

            Assert.AreEqual("alpha,charlie,delta,bravo", Names(list));
            Assert.AreEqual(SortOrder.Descending, list.PrimarySortOrder);
            Assert.AreEqual(1, completed);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void AcceptedNestedLegacyGroupingDoesNotCommitTheCanceledOuterSort(bool useVirtual, bool cancelAfterPreparation) {
            TypedObjectListView<Row> list = this.CreateList(useVirtual);
            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(1), SortOrder.Ascending);
            list.Sort(list.GetColumn(2), SortOrder.Ascending);
            bool nested = false;
            int nestedSortFields = 0;
            int nestedParameterFields = 0;
            list.BeforeCreatingGroups += delegate(object sender, CreateGroupsEventArgs args) {
                if (nested) {
                    nestedSortFields = list.SortDescriptions.Count;
                    nestedParameterFields = args.Parameters.SortDescriptions.Count;
                }
            };
            EventHandler<CreateGroupsEventArgs> regroupAndCancel = delegate(object sender, CreateGroupsEventArgs args) {
                if (nested)
                {
                    return;
                }
                nested = true;
                try {
                    list.BuildGroups(list.GetColumn(1), SortOrder.Ascending,
                        list.GetColumn(2), SortOrder.Ascending, null, SortOrder.None);
                } finally {
                    nested = false;
                }
                args.Canceled = true;
            };
            if (cancelAfterPreparation)
            {
                list.AboutToCreateGroups += regroupAndCancel;
            }
            else
            {
                list.BeforeCreatingGroups += regroupAndCancel;
            }
            int groupingCompleted = 0;
            int sortingCompleted = 0;
            list.AfterCreatingGroups += delegate { groupingCompleted++; };
            list.AfterSorting += delegate { sortingCompleted++; };

            list.Sort(new[] {
                new OLVSortDescription(list.GetColumn(2), SortOrder.Descending),
                new OLVSortDescription(list.GetColumn(0), SortOrder.Ascending),
                new OLVSortDescription(list.GetColumn(1), SortOrder.Descending)
            });

            Assert.AreEqual("charlie,alpha,bravo,delta", Names(list));
            Assert.AreEqual(SortOrder.Ascending, list.PrimarySortOrder);
            Assert.AreEqual(1, list.SortDescriptions.Count);
            Assert.AreEqual(1, groupingCompleted);
            Assert.AreEqual(0, sortingCompleted);
            Assert.AreEqual(1, nestedSortFields);
            Assert.AreEqual(1, nestedParameterFields);
            list.Refresh();
            AssertNativeOrder(list);
        }

        [TestMethod]
        [DataRow(false, false, false)]
        [DataRow(false, true, false)]
        [DataRow(true, false, false)]
        [DataRow(true, true, false)]
        [DataRow(false, false, true)]
        [DataRow(false, true, true)]
        [DataRow(true, false, true)]
        [DataRow(true, true, true)]
        public void CanceledNestedGroupingAfterCommitDoesNotCancelTheOuterSort(bool useVirtual, bool cancelAfterPreparation, bool useLegacyOverload) {
            TypedObjectListView<Row> list = this.CreateList(useVirtual);
            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(1), SortOrder.Ascending);
            list.Sort(list.GetColumn(2), SortOrder.Ascending);
            bool nested = false;
            list.BeforeCreatingGroups += delegate(object sender, CreateGroupsEventArgs args) {
                args.Canceled = nested && !cancelAfterPreparation;
            };
            list.AboutToCreateGroups += delegate(object sender, CreateGroupsEventArgs args) {
                args.Canceled = nested && cancelAfterPreparation;
            };
            int groupingCompleted = 0;
            list.AfterCreatingGroups += delegate(object sender, CreateGroupsEventArgs args) {
                groupingCompleted++;
                nested = true;
                try {
                    if (useLegacyOverload)
                    {
                        list.BuildGroups(list.GetColumn(0), SortOrder.Ascending, null, SortOrder.None, null, SortOrder.None);
                    }
                    else
                    {
                        list.BuildGroups(list.GetColumn(0), SortOrder.Ascending, Array.Empty<OLVSortDescription>());
                    }
                } finally {
                    nested = false;
                }
                Assert.AreSame(args.Groups, list.OLVGroups);
            };
            int sortingCompleted = 0;
            SortOrder reportedOrder = SortOrder.None;
            list.AfterSorting += delegate(object sender, AfterSortingEventArgs args) {
                sortingCompleted++;
                reportedOrder = args.SortOrder;
            };

            list.Sort(list.GetColumn(2), SortOrder.Descending);

            Assert.AreEqual("alpha,charlie,delta,bravo", Names(list));
            Assert.AreEqual(SortOrder.Descending, list.PrimarySortOrder);
            Assert.AreEqual(SortOrder.Descending, reportedOrder);
            Assert.AreEqual(1, groupingCompleted);
            Assert.AreEqual(1, sortingCompleted);
            AssertNativeOrder(list);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void LegacyGroupingPreservesDeclaredInactiveSortFields(bool useVirtual) {
            TypedObjectListView<Row> list = this.CreateList(useVirtual);
            GroupingParameters parameters = null;
            list.BeforeCreatingGroups += delegate(object sender, CreateGroupsEventArgs args) {
                parameters = args.Parameters;
                Assert.AreEqual(0, list.SortDescriptions.Count);
            };

            list.BuildGroups(list.GetColumn(1), SortOrder.Ascending,
                list.GetColumn(2), SortOrder.None, list.GetColumn(0), SortOrder.Descending);

            Assert.AreSame(list.GetColumn(2), parameters.PrimarySort);
            Assert.AreEqual(SortOrder.None, parameters.PrimarySortOrder);
            Assert.AreSame(list.GetColumn(0), parameters.SecondarySort);
            Assert.AreEqual(SortOrder.Descending, parameters.SecondarySortOrder);
            Assert.AreEqual("charlie,alpha,delta,bravo", Names(list));
        }

        [TestMethod]
        public void CancelingFirstVirtualGroupingKeepsFlatRowsAccessible() {
            TypedObjectListView<Row> list = this.CreateList(true);
            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(1), SortOrder.Ascending);
            list.BeforeCreatingGroups += delegate(object sender, CreateGroupsEventArgs args) { args.Canceled = true; };
            list.BuildGroups();

            Assert.AreEqual("delta,charlie,bravo,alpha", Names(list));
            Assert.AreEqual(0, list.GetDisplayOrderOfItemIndex(0));
            Assert.AreSame(list.GetItem(0), list.GetNextItem(null));
            Assert.AreSame(list.GetItem(3), list.GetPreviousItem(null));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CanceledGroupingAfterSourceChangesLeavesNewRowsAccessible(bool useVirtual) {
            TypedObjectListView<Row> list = this.CreateList(useVirtual);
            list.Grouping = GroupingConfiguration.Fixed(list.GetColumn(1), SortOrder.Ascending);
            list.Sort(list.GetColumn(2), SortOrder.Descending);
            list.OLVGroups[0].Collapsed = true;
            EventHandler<CreateGroupsEventArgs> cancel = delegate(object sender, CreateGroupsEventArgs args) {
                args.Canceled = true;
            };
            list.BeforeCreatingGroups += cancel;
            list.SetObjects(new[] { new Row("new", null, 5), new Row("other", "C", 6) });

            Assert.AreEqual(0, list.OLVGroups.Count);
            Assert.AreEqual("new,other", Names(list));
            Assert.AreEqual(0, list.GetDisplayOrderOfItemIndex(0));
            Assert.AreSame(list.GetItem(0), list.GetNextItem(null));

            list.BeforeCreatingGroups -= cancel;
            list.BuildGroups();
            Assert.AreEqual(2, list.OLVGroups.Count);
            Assert.IsNull(list.OLVGroups[0].Key);
            Assert.IsTrue(list.OLVGroups[0].Collapsed);
        }

        private TypedObjectListView<Row> CreateList(bool useVirtual) {
            return this.CreateList(useVirtual ? new TypedFastObjectListView<Row>() : new TypedObjectListView<Row>());
        }

        private TypedObjectListView<Row> CreateList(TypedObjectListView<Row> list) {
            list.ShowGroups = false;
            list.View = View.Details;
            list.Size = new Size(450, 180);
            list.AllColumns.AddRange(new[] {
                new OLVColumn("Name", "") { AspectGetter = x => ((Row)x).Name, Width = 130 },
                new OLVColumn("Group", "") { AspectGetter = x => ((Row)x).Group, Width = 130 },
                new OLVColumn("Value", "") { AspectGetter = x => ((Row)x).Value, Width = 130 }
            });
            list.Columns.AddRange(list.AllColumns.ToArray());
            mainForm.Controls.Add(list);
            list.CreateControl();
            list.SetObjects(new[] {
                new Row("delta", "B", 2), new Row("charlie", null, 3),
                new Row("bravo", "B", 1), new Row("alpha", null, 4)
            });
            list.ShowGroups = true;
            return list;
        }

        private static string Names(TypedObjectListView<Row> list) {
            return String.Join(",", list.Rows.Select(row => row.Name));
        }

        private static void AssertNativeOrder(TypedObjectListView<Row> list) {
            int[] tops = list.Rows.Select(row => list.GetItemRect(list.IndexOf(row)).Top).ToArray();
            Assert.IsTrue(tops.Zip(tops.Skip(1), (first, second) => first < second).All(ordered => ordered),
                "Native row positions must match the resolved group/item order: " + String.Join(",", tops));
            CollectionAssert.AreEqual(Enumerable.Range(0, list.GetItemCount()).ToArray(),
                list.Rows.Select(row => list.GetDisplayOrderOfItemIndex(list.IndexOf(row))).ToArray());
        }

        private sealed class EqualRow {
            internal EqualRow(string group) { this.Group = group; }

            internal string Group { get; }

            public override bool Equals(object obj) => obj is EqualRow;
            public override int GetHashCode() => 0;
        }

        private sealed class Row {
            internal Row(string name, string group, int value) {
                this.Name = name;
                this.Group = group;
                this.Value = value;
            }

            internal readonly string Name;
            internal readonly string Group;
            internal readonly int Value;
        }

        private sealed class ReverseGroups : FastListGroupingStrategy {
            private readonly List<OLVGroup> buffer = new List<OLVGroup>();

            public override IList<OLVGroup> GetGroups(GroupingParameters parameters) {
                IList<OLVGroup> groups = base.GetGroups(parameters);
                this.buffer.Clear();
                this.buffer.AddRange(groups.Reverse());
                return this.buffer;
            }
        }

        private sealed class HookedListView : TypedObjectListView<Row> {
            internal int BuildCalls;

            public override void BuildGroups(OLVColumn groupByColumn, SortOrder groupByOrder,
                OLVColumn column, SortOrder order, OLVColumn secondaryColumn, SortOrder secondaryOrder) {
                this.BuildCalls++;
                base.BuildGroups(groupByColumn, groupByOrder, column, order, secondaryColumn, secondaryOrder);
            }
        }
    }
}
