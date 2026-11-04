using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using BrightIdeasSoftware.Design;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#if NETFRAMEWORK
using System.Linq;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Windows.Forms.Design.Behavior;
#endif

namespace BrightIdeasSoftware.Tests
{
    [STATestClass]
    public class TestDesignerColumnResize
    {
#if NETFRAMEWORK
        [TestMethod]
        public void Test_FrameworkDesigner_OffersHeaderResizeOnlyWhenSelected() {
            using (var surface = new DesignSurface(typeof(Form))) {
                var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost));
                var list = (ObjectListView)host.CreateComponent(typeof(ObjectListView), "list");
                var designer = (ControlDesigner)host.GetDesigner(list);
                Assert.AreEqual("ObjectListView.Design.Framework", designer.GetType().Assembly.GetName().Name);
                PropertyDescriptor columns = TypeDescriptor.GetProperties(list)["Columns"];
                var editor = columns.GetEditor(typeof(System.Drawing.Design.UITypeEditor));
                Assert.IsNotNull(editor);
                Assert.AreSame(designer.GetType().Assembly, editor.GetType().Assembly, editor.GetType().AssemblyQualifiedName);
                Assert.IsTrue(designer.GetGlyphs(GlyphSelectionType.Selected).Cast<Glyph>()
                    .Any(glyph => glyph.GetType().Name == "HeaderResizeGlyph"));
                Assert.IsFalse(designer.GetGlyphs(GlyphSelectionType.NotSelected).Cast<Glyph>()
                    .Any(glyph => glyph.GetType().Name == "HeaderResizeGlyph"));
            }
        }

        [TestMethod]
        public void Test_FrameworkDesigner_EscapeCancelsPreview() {
            using (var surface = new DesignSurface(typeof(Form)))
            using (var window = new Form { ClientSize = new Size(480, 300), Opacity = 0, ShowInTaskbar = false }) {
                var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost));
                var list = (ObjectListView)host.CreateComponent(typeof(ObjectListView), "list");
                var column = (OLVColumn)host.CreateComponent(typeof(OLVColumn), "column");
                list.Size = new Size(360, 160);
                column.Width = 120;
                list.AllColumns.Add(column);
                list.Columns.Add(column);
                var view = (Control)surface.View;
                view.Dock = DockStyle.Fill;
                window.Controls.Add(view);
                ((Form)host.RootComponent).Controls.Add(list);
                window.Show();
                list.CreateControl();
                int originalWidth = column.Width;
                var designer = (ControlDesigner)host.GetDesigner(list);
                Glyph glyph = designer.GetGlyphs(GlyphSelectionType.Selected).Cast<Glyph>()
                    .Single(item => item.GetType().Name == "HeaderResizeGlyph");
                Rectangle bounds = glyph.Bounds;
                Assert.IsFalse(bounds.IsEmpty);
                var start = new Point(bounds.Left + column.Width, bounds.Top + bounds.Height / 2);
                Assert.AreSame(Cursors.VSplit, glyph.GetHitTest(start));
                Assert.IsTrue(glyph.Behavior.OnMouseDown(glyph, MouseButtons.Left, start));
                glyph.Behavior.OnMouseMove(glyph, MouseButtons.Left, new Point(start.X + 40, start.Y));
                Assert.AreEqual(originalWidth + 40, column.Width);
                Assert.IsTrue(glyph.Behavior.DisableAllCommands);
                Assert.IsTrue(PostMessage(list.Handle, 0x100, (IntPtr)Keys.Escape, IntPtr.Zero));
                Application.DoEvents();
                Assert.AreEqual(originalWidth, column.Width);
                Assert.IsFalse(glyph.Behavior.DisableAllCommands);
            }

        }

        [DllImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
        private static extern bool PostMessage(IntPtr window, int message, IntPtr wparam, IntPtr lparam);
#endif

        [TestMethod]
        public void Test_RuntimeAssembly_DoesNotReferenceDesignerHosts() {
            foreach (var reference in typeof(ObjectListView).Assembly.GetReferencedAssemblies()) {
                Assert.IsFalse(reference.Name == "System.Design" || reference.Name == "WindowsBase" ||
                    reference.Name == "ObjectListView.Design.Framework" ||
                    reference.Name.StartsWith("Microsoft.DotNet.DesignTools", StringComparison.Ordinal),
                    "Runtime assembly references design host " + reference.Name);
            }
        }

        [TestMethod]
        public void Test_OverlayConverter_RemainsAvailableWithoutDesignerAssembly() {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(TextOverlay));
            Assert.AreSame(typeof(ObjectListView).Assembly, converter.GetType().Assembly);
            Assert.AreEqual("(none)", converter.ConvertToInvariantString(new TextOverlay()));
        }

        [TestMethod]
        public void Test_ResizePreview_CommitsOneBoundedChange() {
            using (var column = new OLVColumn { Width = 160, MaximumWidth = 220 }) {
                var changes = new Changes();
                var transaction = new Transaction();
                using (var resize = ColumnResizeTransaction.Begin(column, changes, () => transaction)) {
                    resize.Preview(200);
                    resize.Preview(240);
                    Assert.AreEqual(0, changes.Changed);
                    resize.Complete(true);
                }
                Assert.AreEqual(220, column.Width);
                Assert.AreEqual(1, changes.Changing);
                Assert.AreEqual(1, changes.Changed);
                Assert.AreEqual(160, changes.OldValue);
                Assert.AreEqual(220, changes.NewValue);
                Assert.IsTrue(transaction.Committed);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_ResizeCancelOrCaptureLoss_RestoresOriginalWidth(bool explicitCancel) {
            using (var column = new OLVColumn { Width = 160 }) {
                var changes = new Changes();
                var transaction = new Transaction();
                using (var resize = ColumnResizeTransaction.Begin(column, changes, () => transaction)) {
                    resize.Preview(240);
                    if (explicitCancel)
                    {
                        resize.Complete(false);
                    }
                }
                Assert.AreEqual(160, column.Width);
                Assert.AreEqual(0, changes.Changed);
                Assert.IsTrue(transaction.Canceled);
            }
        }

        [TestMethod]
        public void Test_ResizeNotificationFailure_RollsBackAndPropagates() {
            using (var column = new OLVColumn { Width = 160 }) {
                var changes = new Changes { RejectChange = true };
                var transaction = new Transaction();
                using (var resize = ColumnResizeTransaction.Begin(column, changes, () => transaction)) {
                    resize.Preview(240);
                    Assert.ThrowsExactly<InvalidOperationException>(() => resize.Complete(true));
                }
                Assert.AreEqual(160, column.Width);
                Assert.IsTrue(transaction.Canceled);
                Assert.IsFalse(transaction.Committed);
            }
        }

        [TestMethod]
        public void Test_ResizeChangingCanceled_CancelsTransactionWithoutChangingWidth() {
            using (var column = new OLVColumn { Width = 160 }) {
                var changes = new Changes { ChangingError = CheckoutException.Canceled };
                var transaction = new Transaction();
                Assert.IsNull(ColumnResizeTransaction.Begin(column, changes, () => transaction));
                Assert.AreEqual(160, column.Width);
                Assert.AreEqual(0, changes.Changed);
                Assert.IsTrue(transaction.Canceled);
            }
        }

        [TestMethod]
        public void Test_ResizeChangingError_CancelsTransactionAndPropagates() {
            using (var column = new OLVColumn { Width = 160 }) {
                var error = new CheckoutException("Checkout failed.");
                var changes = new Changes { ChangingError = error };
                var transaction = new Transaction();
                Assert.AreSame(error, Assert.ThrowsExactly<CheckoutException>(() =>
                    ColumnResizeTransaction.Begin(column, changes, () => transaction)));
                Assert.AreEqual(160, column.Width);
                Assert.AreEqual(0, changes.Changed);
                Assert.IsTrue(transaction.Canceled);
                Assert.IsFalse(transaction.Committed);
            }
        }

        private sealed class Transaction : DesignerTransaction
        {
            protected override void OnCommit() { }
            protected override void OnCancel() { }
        }

        private sealed class Changes : IComponentChangeService
        {
            public int Changing;
            public int Changed;
            public object OldValue;
            public object NewValue;
            public bool RejectChange;
            public Exception ChangingError;
            public void OnComponentChanging(object component, MemberDescriptor member) {
                this.Changing++;
                if (this.ChangingError != null)
                {
                    throw this.ChangingError;
                }
            }
            public void OnComponentChanged(object component, MemberDescriptor member, object oldValue, object newValue) {
                if (this.RejectChange)
                {
                    throw new InvalidOperationException("Change rejected.");
                }

                this.Changed++;
                this.OldValue = oldValue;
                this.NewValue = newValue;
            }
            public event ComponentEventHandler ComponentAdded { add { } remove { } }
            public event ComponentEventHandler ComponentAdding { add { } remove { } }
            public event ComponentChangedEventHandler ComponentChanged { add { } remove { } }
            public event ComponentChangingEventHandler ComponentChanging { add { } remove { } }
            public event ComponentEventHandler ComponentRemoved { add { } remove { } }
            public event ComponentEventHandler ComponentRemoving { add { } remove { } }
            public event ComponentRenameEventHandler ComponentRename { add { } remove { } }
        }
    }
}
