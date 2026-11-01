using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    public class TieRow
    {
        public TieRow(string name, string group) {
            this.Name = name;
            this.Group = group;
        }

        public string Name { get; set; }
        public string Group { get; set; }
    }

    /// <summary>
    /// Pins the documented sorting rules for tied rows and null values.
    /// </summary>
    [STATestClass]
    public class TestSortTiesAndNulls : WinFormsTestFixture
    {
        // Column 0 is Name. All rows except "a" tie on Group "x".
        private ObjectListView CreateList(bool fast) {
            ObjectListView list = fast ? new FastObjectListView() : new ObjectListView();
            list.View = View.Details;
            list.ShowGroups = false;
            list.Columns.Add(new OLVColumn("Name", "Name"));
            list.Columns.Add(new OLVColumn("Group", "Group"));
            mainForm.Controls.Add(list);
            list.CreateControl();
            list.SetObjects(new[] {
                new TieRow("d", "x"),
                new TieRow("b", "x"),
                new TieRow("a", "y"),
                new TieRow("e", "x"),
                new TieRow("c", "x"),
            });
            return list;
        }

        private static List<string> Names(ObjectListView list) {
            var names = new List<string>();
            for (int i = 0; i < list.GetItemCount(); i++)
            {
                names.Add(((TieRow)list.GetItem(i).RowObject).Name ?? "<null>");
            }

            return names;
        }

        [TestMethod]
        [DataRow(false, SortOrder.Ascending)]
        [DataRow(false, SortOrder.Descending)]
        [DataRow(true, SortOrder.Ascending)]
        [DataRow(true, SortOrder.Descending)]
        public void Test_Sort_NoImplicitFirstColumnTieBreaker(bool fast, SortOrder order) {
            ObjectListView list = this.CreateList(fast);
            IList<OLVSortDescription> seen = null;
            list.BeforeSorting += delegate(object sender, BeforeSortingEventArgs args) {
                seen = args.SortDescriptions;
            };

            list.Sort(list.GetColumn(1), order);

            Assert.IsNotNull(seen);
            Assert.AreEqual(1, seen.Count, "Only the primary column takes part in the sort");
            Assert.AreSame(list.GetColumn(1), seen[0].Column);
            Assert.IsNull(list.SecondarySortColumn);
            List<string> names = Names(list);
            Assert.AreEqual(order == SortOrder.Ascending ? 4 : 0, names.IndexOf("a"), "The primary sort is still applied");
        }

        [TestMethod]
        [DataRow(false, SortOrder.Ascending, "b,c,d,e,a")]
        [DataRow(false, SortOrder.Descending, "a,e,d,c,b")]
        [DataRow(true, SortOrder.Ascending, "b,c,d,e,a")]
        [DataRow(true, SortOrder.Descending, "a,e,d,c,b")]
        public void Test_Sort_SecondarySortColumnBreaksTies(bool fast, SortOrder order, string expected) {
            ObjectListView list = this.CreateList(fast);
            list.SecondarySortColumn = list.GetColumn(0);
            list.SecondarySortOrder = order;

            list.Sort(list.GetColumn(1), order);

            Assert.AreEqual(expected, string.Join(",", Names(list)));
        }

        [TestMethod]
        public void Test_ModelObjectComparer_NullComesFirstAscending() {
            var column = new OLVColumn("Name", "Name");
            var ascending = new ModelObjectComparer(column, SortOrder.Ascending);
            var descending = new ModelObjectComparer(column, SortOrder.Descending);
            Assert.IsLessThan(0, ascending.Compare(new TieRow(null, "x"), new TieRow("a", "x")));
            Assert.IsGreaterThan(0, descending.Compare(new TieRow(null, "x"), new TieRow("a", "x")));
        }

        [TestMethod]
        [DataRow(false, SortOrder.Ascending, "<null>,a,b")]
        [DataRow(false, SortOrder.Descending, "b,a,<null>")]
        [DataRow(true, SortOrder.Ascending, "<null>,a,b")]
        [DataRow(true, SortOrder.Descending, "b,a,<null>")]
        public void Test_Sort_NullPlacement(bool fast, SortOrder order, string expected) {
            ObjectListView list = fast ? new FastObjectListView() : new ObjectListView();
            list.View = View.Details;
            list.ShowGroups = false;
            list.Columns.Add(new OLVColumn("Name", "Name"));
            mainForm.Controls.Add(list);
            list.CreateControl();
            list.SetObjects(new[] { new TieRow("b", "x"), new TieRow(null, "x"), new TieRow("a", "x") });

            list.Sort(list.GetColumn(0), order);

            Assert.AreEqual(expected, string.Join(",", Names(list)));
        }
    }

    /// <summary>
    /// Pins how check state behaves for models hidden by a filter.
    /// </summary>
    [STATestClass]
    public class TestCheckedObjectsWithFilter : WinFormsTestFixture
    {
        private ObjectListView olv;

        [TestInitialize]
        public void InitCheckList() {
            this.olv = mainForm.objectListView1;
            this.olv.CheckBoxes = true;
            this.olv.TriStateCheckBoxes = false;
            this.olv.SetObjects(PersonDb.All);
            this.olv.CheckedObjects = null;
        }

        [TestMethod]
        public void Test_CheckedObjects_FilteredModelKeepsItsCheck() {
            Assert.IsTrue(this.olv.PersistentCheckBoxes);
            Person hidden = PersonDb.All[2];
            this.olv.CheckedObjects = new[] { PersonDb.All[0], hidden, PersonDb.All[3] };
            Assert.HasCount(3, this.olv.CheckedObjects);

            this.olv.UseFiltering = true;
            this.olv.ModelFilter = new ModelFilter(x => x != hidden);
            Assert.AreEqual(PersonDb.All.Count - 1, this.olv.GetItemCount());
            Assert.HasCount(2, this.olv.CheckedObjects, "Only shown rows are reported");
            Assert.IsTrue(this.olv.IsChecked(hidden));

            this.olv.ModelFilter = null;
            this.olv.UseFiltering = false;
            Assert.HasCount(3, this.olv.CheckedObjects);
            CollectionAssert.Contains(this.olv.CheckedObjects, hidden);
        }

        [TestMethod]
        public void Test_CheckAllAndUncheckAll_IncludeFilteredModels() {
            Person hidden = PersonDb.All[2];
            this.olv.UseFiltering = true;
            this.olv.ModelFilter = new ModelFilter(x => x != hidden);

            this.olv.CheckAll();
            Assert.HasCount(PersonDb.All.Count - 1, this.olv.CheckedObjects);
            Assert.IsTrue(this.olv.IsChecked(hidden));
            this.olv.ModelFilter = null;
            Assert.HasCount(PersonDb.All.Count, this.olv.CheckedObjects);

            this.olv.ModelFilter = new ModelFilter(x => x != hidden);
            this.olv.UncheckAll();
            Assert.IsFalse(this.olv.IsChecked(hidden));
            this.olv.ModelFilter = null;
            this.olv.UseFiltering = false;
            Assert.IsEmpty(this.olv.CheckedObjects);
        }
    }
}
