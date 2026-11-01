using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    /// <summary>
    /// Runs the same MoveObjects scenarios against each kind of list.
    /// Rows are A B C D E (the first five people in PersonDb).
    /// </summary>
    public abstract class MoveObjectsFixture : WinFormsTestFixture
    {
        protected abstract ObjectListViewBase CreateList();

        private ObjectListViewBase list;
        private Person[] rows;

        [TestInitialize]
        public void InitMoveList() {
            this.list = this.CreateList();
            this.rows = PersonDb.All.Take(5).ToArray();
            this.list.SetObjects(this.rows);
        }

        private Person Row(char letter) {
            return this.rows[letter - 'A'];
        }

        private string Letter(object model) {
            int i = Array.IndexOf(this.rows, model);
            return i < 0 ? "?" : ((char)('A' + i)).ToString();
        }

        private string StoredOrder() {
            return string.Concat(this.list.ModelObjects.Cast<object>().Select(this.Letter));
        }

        private string DisplayedOrder() {
            var letters = new List<string>();
            for (int i = 0; i < this.list.GetItemCount(); i++)
            {
                letters.Add(this.Letter(this.list.GetItem(i).RowObject));
            }

            return string.Concat(letters);
        }

        private void MoveAndAssert(int index, string toMove, string expected) {
            this.list.MoveObjects(index, toMove.Select(this.Row).ToArray());
            Assert.AreEqual(expected, this.DisplayedOrder(), $"Displayed order after MoveObjects({index}, [{toMove}])");
            Assert.AreEqual(expected, this.StoredOrder(), $"Stored order after MoveObjects({index}, [{toMove}])");
        }

        [TestMethod]
        [DataRow(0, "A")]
        [DataRow(1, "B")]
        [DataRow(2, "C")]
        [DataRow(4, "E")]
        public void Test_MoveObjects_ToCurrentPosition(int index, string toMove) {
            this.MoveAndAssert(index, toMove, "ABCDE");
        }

        [TestMethod]
        public void Test_MoveObjects_FirstObjectsToStart() {
            this.MoveAndAssert(0, "AB", "ABCDE");
        }

        [TestMethod]
        public void Test_MoveObjects_IncludingObjectAtTargetIndex() {
            this.MoveAndAssert(2, "CA", "BCADE");
        }

        [TestMethod]
        public void Test_MoveObjects_Forward() {
            this.MoveAndAssert(3, "A", "BCADE");
        }

        [TestMethod]
        public void Test_MoveObjects_Backward() {
            this.MoveAndAssert(1, "D", "ADBCE");
        }
    }

    [STATestClass]
    public class TestOlvMoveObjects : MoveObjectsFixture
    {
        protected override ObjectListViewBase CreateList() {
            return mainForm.objectListView1;
        }
    }

    [STATestClass]
    public class TestFastOlvMoveObjects : MoveObjectsFixture
    {
        protected override ObjectListViewBase CreateList() {
            return mainForm.fastObjectListView1;
        }
    }

    [STATestClass]
    public class TestTypedOlvMoveObjects : MoveObjectsFixture
    {
        protected override ObjectListViewBase CreateList() {
            var typed = new TypedObjectListView<Person> { View = View.Details, ShowGroups = false };
            typed.Columns.Add(new OLVColumn("Name", "Name"));
            typed.Columns.Add(new OLVColumn("Occupation", "Occupation"));
            mainForm.Controls.Add(typed);
            typed.CreateControl();
            return typed;
        }
    }
}
