using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests {
    [STATestClass]
    public class TestTypedListView : WinFormsTestFixture {
        [TestMethod]
        public void Test_Objects_All() {
            this.tolv.Objects = PersonDb.All;
            Assert.AreEqual(PersonDb.All.Count, this.tolv.Objects.Count);
        }

        [TestMethod]
        public void Test_GenerateAspectGetters_ExtractsData() {
            this.tolv.GenerateAspectGetters();
            this.tolv.Objects = PersonDb.All;
            Person p = this.tolv.ListView.GetItem(0).RowObject as Person;
            Assert.AreEqual(p.Name, this.tolv.ListView.Items[0].SubItems[0].Text);
        }

        [TestMethod]
        public void Test_GenerateAspectGetters_NullDataObject() {
            this.tolv.GenerateAspectGetters();
            List<Person> list = new List<Person>();
            list.Add(null);
            this.tolv.Objects = list;
            Assert.AreEqual(list.Count, this.tolv.Objects.Count);
        }

        [TestMethod]
        public void Test_GenerateAspectGetters_ClearObjects() {
            this.tolv.GenerateAspectGetters();
            this.tolv.Objects = PersonDb.All;
            this.tolv.ListView.ClearObjects();
            Assert.AreEqual(0, this.tolv.Objects.Count);
            this.tolv.Objects = PersonDb.All;
            Assert.AreEqual(PersonDb.All.Count, this.tolv.Objects.Count);
        }

        [TestCleanup]
        public void TestTearDown() {
            // Clear any aspect getters that were generated
            for (int i = 0; i < this.tolv.ListView.Columns.Count; i++)
                this.tolv.ListView.GetColumn(i).AspectGetter = null;
        }

        [TestInitialize]
        public void Init() {
            this.tolv = new TypedObjectListView<Person>(mainForm.objectListView1);
        }

        protected TypedObjectListView<Person> tolv;
    }
}