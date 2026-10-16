using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests {
    [STATestClass]
    public class TestTypedListView : WinFormsTestFixture {
        [TestMethod]
        public void Test_Objects_All() {
            this.tolv.Objects = PersonDb.All;
            Assert.AreEqual(PersonDb.All.Count, this.tolv.Objects.Count());
        }

        [TestMethod]
        public void Test_EnumerableCollections_QueriesAndSnapshots() {
            this.tolv.ListView.UseNotifyPropertyChanged = true;
            this.tolv.Objects = new SingleUseEnumerable<Person>(PersonDb.All.Where(p => p.CulinaryRating >= 100));
            this.tolv.SetSelectedObjects(new SingleUseEnumerable<Person>(this.tolv.Objects.Take(2)));
            IList<Person> selected = this.tolv.SelectedObjects;
            this.tolv.SelectedObjects = new List<Person>();
            CollectionAssert.AreEqual(PersonDb.All.Take(2).ToArray(), selected.ToArray());

            this.tolv.ListView.CheckBoxes = true;
            this.tolv.SetCheckedObjects(new SingleUseEnumerable<Person>(this.tolv.Objects.Take(2)));
            IList<Person> checkedObjects = this.tolv.CheckedObjects;
            this.tolv.CheckedObjects = null;
            Assert.AreEqual(2, checkedObjects.Count);
            Assert.IsEmpty(this.tolv.CheckedObjects);
        }

        [TestMethod]
        public void Test_EnumerableCommands_ForwardToControl() {
            this.tolv.Objects = Enumerable.Empty<Person>();
            this.tolv.AddObjects(new SingleUseEnumerable<Person>(PersonDb.All.Take(2)));
            this.tolv.InsertObjects(1, new SingleUseEnumerable<Person>(PersonDb.All.Skip(2).Take(1)));
            CollectionAssert.AreEqual(new[] { PersonDb.All[0], PersonDb.All[2], PersonDb.All[1] },
                this.tolv.Objects.ToArray());
            this.tolv.RefreshObjects(new SingleUseEnumerable<Person>(PersonDb.All.Take(1)));
            this.tolv.RemoveObjects(new SingleUseEnumerable<Person>(this.tolv.Objects.Where(p => p == PersonDb.All[2])));
            CollectionAssert.AreEqual(PersonDb.All.Take(2).ToArray(), this.tolv.Objects.ToArray());
            this.tolv.CopyObjectsToClipboard(Enumerable.Empty<Person>());
        }

        [TestMethod]
        public void Test_CollectionProperties_GenericListsAndSnapshots() {
            this.tolv.Objects = PersonDb.All;
            this.tolv.ListView.CheckBoxes = true;
            IList<Person> models = new ArraySegment<Person>(PersonDb.All.ToArray(), 0, 2);
            this.tolv.SelectedObjects = models;
            this.tolv.CheckedObjects = models;

            IList<Person> selected = this.tolv.SelectedObjects;
            IList<Person> checkedObjects = this.tolv.CheckedObjects;
            Assert.AreEqual(2, selected.Count);
            Assert.AreSame(PersonDb.All[0], selected[0]);
            Assert.AreEqual(2, checkedObjects.Count);
            Assert.AreSame(PersonDb.All[0], checkedObjects[0]);

            selected.Clear();
            checkedObjects.Clear();
            Assert.AreEqual(2, this.tolv.SelectedObjects.Count);
            Assert.AreEqual(2, this.tolv.CheckedObjects.Count);
        }

        [TestMethod]
        public void Test_CollectionProperties_HonorControlPropertyOverrides() {
            using (var list = new PropertyGuardListView { View = View.Details, ShowGroups = false }) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                mainForm.Controls.Add(list);
                list.CreateControl();
                list.SetObjects(PersonDb.All);
                list.CheckBoxes = true;
                list.SelectedObject = PersonDb.All[1];
                list.CheckedObject = PersonDb.All[1];
                var typed = new TypedObjectListView<Person>(list);
                IList<Person> models = new ArraySegment<Person>(PersonDb.All.ToArray(), 0, 1);

                list.RejectPropertyChanges = true;
                typed.SelectedObjects = models;
                typed.CheckedObjects = models;
                Assert.AreSame(PersonDb.All[1], list.SelectedObject);
                Assert.AreSame(PersonDb.All[1], list.CheckedObject);
                typed.SelectedObjects = null;
                typed.CheckedObjects = null;
                Assert.AreSame(PersonDb.All[1], list.SelectedObject);
                Assert.AreSame(PersonDb.All[1], list.CheckedObject);

                list.RejectPropertyChanges = false;
                typed.SelectedObjects = models;
                typed.CheckedObjects = models;
                Assert.AreSame(PersonDb.All[0], list.SelectedObject);
                Assert.AreSame(PersonDb.All[0], list.CheckedObject);
                typed.SelectedObjects = null;
                typed.CheckedObjects = null;
                Assert.IsEmpty(list.SelectedObjects);
                Assert.IsEmpty(list.CheckedObjects);
            }
        }

        [TestMethod]
        public void Test_CheckedObjectsEnumerable_HonorsControlPropertyOverride() {
            using (var list = new PropertyGuardListView { View = View.Details, ShowGroups = false }) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                mainForm.Controls.Add(list);
                list.CreateControl();
                list.SetObjects(PersonDb.All);
                list.CheckBoxes = true;
                list.CheckedObject = PersonDb.All[1];

                list.RejectPropertyChanges = true;
                list.CheckedObjectsEnumerable = new SingleUseEnumerable<Person>(PersonDb.All.Take(1));
                Assert.AreSame(PersonDb.All[1], list.CheckedObject);

                list.RejectPropertyChanges = false;
                list.CheckedObjectsEnumerable = new SingleUseEnumerable<Person>(PersonDb.All.Take(1));
                Assert.AreSame(PersonDb.All[0], list.CheckedObject);
                list.CheckedObjectsEnumerable = null;
                Assert.IsEmpty(list.CheckedObjects);
            }
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
            Assert.AreEqual(list.Count, this.tolv.Objects.Count());
        }

        [TestMethod]
        public void Test_GenerateAspectGetters_ClearObjects() {
            this.tolv.GenerateAspectGetters();
            this.tolv.Objects = PersonDb.All;
            this.tolv.ListView.ClearObjects();
            Assert.AreEqual(0, this.tolv.Objects.Count());
            this.tolv.Objects = PersonDb.All;
            Assert.AreEqual(PersonDb.All.Count, this.tolv.Objects.Count());
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

        private sealed class PropertyGuardListView : ObjectListView {
            public bool RejectPropertyChanges { get; set; }

            public override IList SelectedObjects {
                get { return base.SelectedObjects; }
                set {
                    if (!RejectPropertyChanges)
                        base.SelectedObjects = value;
                }
            }

            public override IList CheckedObjects {
                get { return base.CheckedObjects; }
                set {
                    if (!RejectPropertyChanges)
                        base.CheckedObjects = value;
                }
            }
        }
    }
}