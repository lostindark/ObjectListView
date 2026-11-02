using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests {
    [STATestClass]
    public class TestTypedSpecializedListViews : WinFormsTestFixture {
        [TestMethod]
        public void Test_TypedFastList_SourceRowsSelectionAndChecks() {
            using (var list = new TypedFastObjectListView<Person>()) {
                this.Host(list);
                list.UseNotifyPropertyChanged = true;
                list.Objects = new SingleUseEnumerable<Person>(PersonDb.All.Take(3));
                list.GetTypedColumn(0).AspectGetter = person => person.Name;
                Assert.AreEqual(PersonDb.All[0].Name, list.GetColumn(0).GetValue(PersonDb.All[0]));
                Assert.AreSame(list, list.Typed.ListView);

                List<Person> snapshot = list.GetObjectsSnapshot();
                snapshot.Clear();
                list.UseFiltering = true;
                list.ModelFilter = new ModelFilter(model => model != PersonDb.All[1]);
                Assert.HasCount(3, list.Objects);
                Assert.AreEqual(2, list.Rows.Count);
                Person first = list.GetModelObject(0);
                Assert.AreSame(PersonDb.All[0], first);

                list.SelectedObject = first;
                IList<Person> selection = list.SelectedObjects;
                Assert.AreSame(first, selection.Single());
                Assert.AreSame(first, ((ObjectListViewBase)list).SelectedModelObject);
                list.SelectedObjects = null;
                Assert.IsEmpty(list.SelectedObjects);
                Assert.HasCount(1, selection);

                list.CheckedObjects = new[] { first };
                Assert.AreSame(first, list.CheckedObject);
                Assert.AreSame(first, ((ObjectListViewBase)list).CheckedModelObjects[0]);
                list.CheckedObjects = null;
                Assert.IsEmpty(list.CheckedObjects);

                using (IEnumerator<Person> rows = list.Rows.GetEnumerator()) {
                    Assert.IsTrue(rows.MoveNext());
                    list.AddObjects(new SingleUseEnumerable<Person>(PersonDb.All.Skip(3).Take(1)));
                    Assert.ThrowsExactly<InvalidOperationException>(() => rows.MoveNext());
                }
                list.InsertObjects(0, new SingleUseEnumerable<Person>(PersonDb.All.Skip(4).Take(1)));
                Assert.AreSame(PersonDb.All[4], list.GetModelObject(0));
                list.RemoveObjects(new SingleUseEnumerable<Person>(PersonDb.All.Skip(4).Take(1)));
                Assert.HasCount(4, list.Objects);
                Assert.ThrowsExactly<InvalidOperationException>(() =>
                    list.SetObjects(EnumerableTestData.FailAfter(PersonDb.All[4])));
                Assert.HasCount(4, list.Objects);
            }
        }

        [TestMethod]
        public void Test_TypedVirtualList_UsesCustomSourceAndTypedCallbacks() {
            using (var list = new TypedVirtualObjectListView<Person>()) {
                list.VirtualListDataSource = new EnumerableVirtualDataSource(list);
                this.Host(list);
                list.UseNotifyPropertyChanged = true;
                list.SetObjects(new SingleUseEnumerable<Person>(PersonDb.All.Take(2)));
                list.GenerateAspectGetters();
                Assert.AreEqual(PersonDb.All[0].Name, list.GetColumn(0).GetValue(PersonDb.All[0]));
                Person model = list.GetModelObject(1);
                Assert.AreSame(PersonDb.All[1], model);
                CollectionAssert.AreEqual(PersonDb.All.Take(2).ToArray(), list.GetObjectsSnapshot());

                list.Typed.CellToolTipGetter = (column, person) => person.Name;
                Assert.AreEqual(model.Name, list.CellToolTipGetter(list.GetColumn(0), model));
                list.SetSelectedObjects(new SingleUseEnumerable<Person>(new[] { model }));
                Assert.AreSame(model, list.SelectedObject);
                Assert.AreSame(model, list.SelectedObjects.Single());
                list.SetCheckedObjects(new[] { model });
                Assert.AreSame(model, list.CheckedObject);
                Assert.AreSame(model, list.CheckedObjects.Single());
                list.Objects = new SingleUseEnumerable<Person>(new[] { model });
                Assert.AreSame(model, list.Rows.Single());
                Assert.AreSame(model, list.Objects.Single());
            }
        }

        [TestMethod]
        public void Test_TypedTree_RootsChildrenExpansionAndHierarchicalChecks() {
            using (var list = new TypedTreeListView<Person>()) {
                this.Host(list);
                Person root = PersonDb.All[0];
                Person child = PersonDb.All[1];
                list.CanExpandGetter = person => person == root;
                list.ChildrenGetter = person => person == root ? new[] { child } : Array.Empty<Person>();
                list.ParentGetter = person => person == child ? root : null;
                list.Roots = new SingleUseEnumerable<Person>(new[] { root });
                Assert.AreSame(root, list.Objects.Single());
                ((IList<Person>)list.Roots).Clear();
                Assert.AreSame(root, list.Roots.Single());
                Assert.IsTrue(list.CanExpandGetter(root));
                Assert.AreSame(child, list.ChildrenGetter(root).Single());
                list.Expand(root);
                CollectionAssert.AreEqual(new[] { root, child }, list.Rows.ToArray());
                Assert.AreSame(child, list.GetModelObject(1));
                Assert.AreSame(root, list.GetParent(child));
                Assert.AreSame(child, list.GetChildren(root).Single());
                Assert.AreSame(root, list.ParentGetter(child));
                Assert.AreSame(root, list.ExpandedObjects.Single());

                list.SelectedObjects = new[] { child };
                Assert.AreSame(child, list.SelectedObject);
                list.HierarchicalCheckboxes = true;
                list.CheckedObjects = new[] { child };
                CollectionAssert.AreEquivalent(new[] { root, child }, list.CheckedObjects.ToArray());
                list.CheckedObjects = null;
                Assert.IsEmpty(list.CheckedObjects);
                list.Objects = new SingleUseEnumerable<Person>(new[] { root });
                Assert.AreSame(root, list.GetObjectsSnapshot().Single());
                Assert.IsTrue(list.IsExpanded(root));
                Assert.ThrowsExactly<NotImplementedException>(() => list.InsertObjects(0, new[] { child }));
            }
        }

        [TestMethod]
        public void Test_TypedDataList_RetainsLiveBindingAndMutationNoOps() {
            using (var list = new TypedDataListView<Person> { AutoGenerateColumns = false }) {
                this.Host(list);
                Assert.IsNull(list.GetObjectsSnapshot());
                var source = new BindingList<Person>(PersonDb.All.Take(2).ToList());
                list.DataSource = source;
                Person model = list.GetModelObject(0);
                Assert.AreSame(PersonDb.All[0], model);
                list.SelectedObjects = new[] { model };
                list.CheckedObjects = new[] { model };
                Assert.AreSame(model, list.SelectedObject);
                Assert.AreSame(model, list.CheckedObject);
                Assert.AreSame(model, list.SelectedObjects.Single());
                Assert.AreSame(model, list.CheckedObjects.Single());
                list.AddObjects(EnumerableTestData.FailAfter(PersonDb.All[2]));
                list.InsertObjects(0, EnumerableTestData.FailAfter(PersonDb.All[2]));
                list.RemoveObjects(EnumerableTestData.FailAfter(model));
                source.Add(PersonDb.All[2]);
                Assert.AreEqual(3, list.Rows.Count);
                Assert.HasCount(3, list.Objects);
                list.GetObjectsSnapshot().Clear();
                Assert.AreEqual(3, source.Count);
                list.GetTypedColumn(0).AspectGetter = person => person.Name;
                Assert.AreEqual(model.Name, list.GetColumn(0).GetValue(model));
            }
        }

        [TestMethod]
        public void Test_TypedFastDataList_RetainsLiveBindingAndDisplayOnlyMutations() {
            using (var list = new TypedFastDataListView<Person> { AutoGenerateColumns = false }) {
                this.Host(list);
                var source = new BindingList<Person>(PersonDb.All.Take(2).ToList());
                list.DataSource = source;
                Person model = list.GetModelObject(0);
                Assert.AreSame(PersonDb.All[0], model);
                list.SelectedObjects = new[] { model };
                list.CheckedObjects = new[] { model };
                Assert.AreSame(model, list.SelectedObject);
                Assert.AreSame(model, list.CheckedObject);
                Assert.AreSame(model, list.SelectedObjects.Single());
                Assert.AreSame(model, list.CheckedObjects.Single());
                list.AddObjects(new[] { PersonDb.All[2] });
                Assert.AreEqual(3, list.Rows.Count);
                list.InsertObjects(0, new[] { PersonDb.All[3] });
                Assert.AreSame(PersonDb.All[3], list.GetModelObject(0));
                list.RemoveObjects(new[] { PersonDb.All[3] });
                Assert.AreEqual(3, list.Rows.Count);
                Assert.AreEqual(2, source.Count);
                source.Add(PersonDb.All[2]);
                Assert.AreEqual(3, list.Rows.Count);
                Assert.HasCount(3, list.Objects);
                list.GetObjectsSnapshot().Clear();
                Assert.AreEqual(3, source.Count);
                list.GenerateAspectGetters();
                Assert.AreEqual(model.Name, list.GetColumn(0).GetValue(model));
            }
        }

        [TestMethod]
        public void Test_TypedDataTree_RetainsBoundHierarchyAndCallbacks() {
            using (var list = new TypedDataTreeListView<BoundNode> {
                AutoGenerateColumns = false, KeyAspectName = "Id", ParentKeyAspectName = "ParentId"
            }) {
                this.Host(list);
                var root = new BoundNode { Id = 1, Name = "Root" };
                var child = new BoundNode { Id = 2, ParentId = 1, Name = "Child" };
                var source = new BindingList<BoundNode> { root, child };
                list.DataSource = source;
                Assert.AreSame(root, list.Roots.Single());
                Assert.AreSame(root, list.Objects.Single());
                Assert.IsTrue(list.CanExpandGetter(root));
                Assert.AreSame(child, list.ChildrenGetter(root).Single());
                list.Expand(root);
                Assert.AreSame(child, list.GetModelObject(1));
                CollectionAssert.AreEqual(new[] { root, child }, list.Rows.ToArray());
                list.SelectedObjects = new[] { child };
                list.CheckedObjects = new[] { child };
                Assert.AreSame(child, list.SelectedObject);
                Assert.AreSame(child, list.CheckedObject);
                Assert.AreSame(child, list.SelectedObjects.Single());
                Assert.AreSame(child, list.CheckedObjects.Single());
                Assert.AreSame(root, list.ExpandedObjects.Single());
                list.GetObjectsSnapshot().Clear();
                Assert.AreEqual(2, source.Count);
                source.Add(new BoundNode { Id = 3, ParentId = 1, Name = "Another child" });
                Assert.AreEqual(3, list.Rows.Count);
                list.GenerateAspectGetters();
                Assert.AreEqual(root.Name, list.GetColumn(0).GetValue(root));
            }
        }

        [TestMethod]
        [DataRow("Virtual")]
        [DataRow("Fast")]
        [DataRow("Tree")]
        [DataRow("Data")]
        [DataRow("FastData")]
        [DataRow("DataTree")]
        public void Test_TypedSpecializations_RejectIncompatibleModelsBeforeChangingRows(string kind) {
            using (ObjectListViewBase list = CreateList(kind)) {
                this.Host(list);
                list.SetObjects(PersonDb.All.Take(1));
                Assert.ThrowsExactly<InvalidCastException>(() =>
                    list.SetObjects(new object[] { PersonDb.All[1], "wrong model type" }));
                Assert.ThrowsExactly<InvalidCastException>(() =>
                    list.RefreshObjects(new object[] { PersonDb.All[0], "wrong model type" }));
                Assert.AreSame(PersonDb.All[0], list.GetModelAt(0));
                list.ItemsChanging += delegate(object sender, ItemsChangingEventArgs args) {
                    args.NewObjects = new object[] { "wrong event model type" };
                };
                Assert.ThrowsExactly<InvalidCastException>(() => list.SetObjects(PersonDb.All.Take(2)));
                Assert.AreEqual(1, list.GetItemCount());
            }
        }

        [TestMethod]
        [DataRow("Virtual")]
        [DataRow("Fast")]
        public void Test_TypedVirtualMutations_RejectIncompatibleEventPayloads(string kind) {
            using (ObjectListViewBase list = CreateList(kind)) {
                this.Host(list);
                list.SetObjects(PersonDb.All.Take(1));
                list.ItemsAdding += delegate(object sender, ItemsAddingEventArgs args) {
                    args.ObjectsToAdd = new object[] { PersonDb.All[1], "wrong model type" };
                };
                Assert.ThrowsExactly<InvalidCastException>(() => list.AddObjects(PersonDb.All.Take(2)));
                Assert.ThrowsExactly<InvalidCastException>(() => list.InsertObjects(0, PersonDb.All.Take(2)));
                list.ItemsRemoving += delegate(object sender, ItemsRemovingEventArgs args) {
                    args.ObjectsToRemove = new object[] { PersonDb.All[0], "wrong model type" };
                };
                Assert.ThrowsExactly<InvalidCastException>(() => list.RemoveObjects(PersonDb.All.Take(1)));
                Assert.AreEqual(1, list.GetItemCount());
            }
        }

        private void Host(ObjectListViewBase list) {
            list.View = View.Details;
            list.ShowGroups = false;
            list.CheckBoxes = true;
            list.Columns.Add(new OLVColumn("Name", "Name"));
            this.mainForm.Controls.Add(list);
            list.CreateControl();
        }

        private static TypedObjectListView<Person> CreateList(string kind) {
            switch (kind) {
                case "Virtual":
                    var list = new TypedVirtualObjectListView<Person>();
                    list.VirtualListDataSource = new EnumerableVirtualDataSource(list);
                    return list;
                case "Fast": return new TypedFastObjectListView<Person>();
                case "Tree": return new TypedTreeListView<Person>();
                case "Data": return new TypedDataListView<Person>();
                case "FastData": return new TypedFastDataListView<Person>();
                case "DataTree": return new TypedDataTreeListView<Person>();
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        [TestMethod]
        [DataRow("Virtual")]
        [DataRow("Fast")]
        [DataRow("Tree")]
        [DataRow("Data")]
        [DataRow("FastData")]
        [DataRow("DataTree")]
        public void Test_GenericBaseAccess_UsesSpecializedSourcesAndState(string kind) {
            using (TypedObjectListView<Person> list = CreateList(kind)) {
                this.Host(list);
                list.Objects = new SingleUseEnumerable<Person>(PersonDb.All.Take(2));
                CollectionAssert.AreEqual(PersonDb.All.Take(2).ToArray(), list.GetObjectsSnapshot());
                CollectionAssert.AreEqual(PersonDb.All.Take(2).ToArray(), list.Rows.ToArray());
                list.SelectedObjects = new[] { PersonDb.All[1] };
                list.CheckedObjects = new[] { PersonDb.All[0] };
                Assert.AreSame(PersonDb.All[1], list.SelectedObject);
                Assert.AreSame(PersonDb.All[0], list.CheckedObject);
                Assert.AreSame(PersonDb.All[1], ((ObjectListViewBase)list).SelectedModelObject);
                Assert.AreSame(PersonDb.All[0], ((ObjectListViewBase)list).CheckedModelObject);
                list.Objects = null;
                Assert.AreEqual(0, list.Rows.Count);
            }
        }

        [TestMethod]
        public void Test_GenericTree_GeneratedChildrenAndRenderingUseTheGenericModel() {
            using (var list = new TypedTreeListView<ClassWithChildren>()) {
                this.Host(list);
                list.Size = new Size(300, 150);
                Generator.GenerateColumns(list, typeof(ClassWithChildren), true);
                var child = new ClassWithChildren { MyChildren = new List<ClassWithChildren>() };
                var root = new ClassWithChildren { MyChildren = new List<ClassWithChildren> { child } };
                list.Roots = new[] { root };
                Assert.IsTrue(list.CanExpandGetter(root));
                Assert.AreSame(child, list.ChildrenGetter(root).Single());
                list.Expand(root);
                CollectionAssert.AreEqual(new[] { root, child }, list.Rows.ToArray());
                using (var bitmap = new Bitmap(list.Width, list.Height)) {
                    list.DrawToBitmap(bitmap, list.ClientRectangle);
                }
            }
        }

        [TestMethod]
        public void Test_GenericFastSource_PreservesObjectEqualityForRemovalAndReplacement() {
            using (var list = new TypedFastObjectListView<EqualityOnlyModel>()) {
                var first = new EqualityOnlyModel();
                var second = new EqualityOnlyModel();
                var replacement = new EqualityOnlyModel();
                var source = (FastObjectListDataSource<EqualityOnlyModel>)list.VirtualListDataSource;
                list.SetObjects(new[] { first, second });
                list.RemoveObjects(new[] { second });
                Assert.AreSame(first, source.ObjectList.Single());
                list.SetObjects(new[] { first, second });
                source.UpdateObject(1, replacement);
                CollectionAssert.AreEqual(new[] { first, replacement }, source.ObjectList);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_NonGenericTreeChildrenGetter_PreservesDelegatesAcrossRebuilds(bool dataBound) {
            using (TypedTreeListView<object> list = dataBound ? new DataTreeListView() : new TreeListView()) {
                this.Host(list);
                var root = new Person("root");
                var child = new Person("child");
                int calls = 0;
                TreeListView.ChildrenGetterDelegate callback = model => {
                    calls++;
                    return new[] { child };
                };
                list.Roots = new[] { root };
                list.ChildrenGetter = callback;

                for (int i = 0; i < 100; i++)
                {
                    list.RebuildAll(true);
                }

                Assert.AreSame(callback, list.ChildrenGetter);
                Assert.AreSame(callback, list.TreeModel.ChildrenGetter);
                Assert.AreSame(child, list.TreeModel.ChildrenGetter(root).Single());
                Assert.AreEqual(1, calls);

                TreeListView.ChildrenGetterDelegate other = model => Array.Empty<object>();
                list.ChildrenGetter += other;
                list.ChildrenGetter -= other;
                Assert.AreSame(callback, list.ChildrenGetter);
                list.ChildrenGetter -= callback;
                Assert.IsEmpty(list.TreeModel.ChildrenGetter(root));
                Assert.AreEqual(1, calls);
            }
        }

        [TestMethod]
        public void Test_NonGenericTreeChildrenGetter_HonorsOverridesThroughGenericAccess() {
            using (var list = new ChildrenGetterOverrideTree()) {
                this.Host(list);
                var child = new Person("child");
                TypedTreeListView<object> typed = list;
                TypedTreeListView<object>.ChildrenGetterDelegate callback = model => new[] { child };
                typed.ChildrenGetter = callback;
                typed.RebuildAll(false);

                Assert.AreEqual(2, list.SetterCalls);
                Assert.AreSame(callback, typed.ChildrenGetter);
                Assert.AreSame(child, list.TreeModel.ChildrenGetter(new Person("root")).Single());
            }
        }

        private sealed class ChildrenGetterOverrideTree : TreeListView {
            public int SetterCalls { get; private set; }

            public override ChildrenGetterDelegate ChildrenGetter {
                get { return base.ChildrenGetter; }
                set {
                    this.SetterCalls++;
                    base.ChildrenGetter = value;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1067", Justification = "This test model verifies that IEquatable alone does not change object-based identity semantics.")]
        private sealed class EqualityOnlyModel : IEquatable<EqualityOnlyModel> {
            public bool Equals(EqualityOnlyModel other) { return other != null; }
        }

        private sealed class BoundNode {
            public int Id { get; set; }
            public int? ParentId { get; set; }
            public string Name { get; set; }
        }
    }
}
