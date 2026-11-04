namespace ObjectListViewPackageSmoke
{
    partial class DesignerSmokeForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing) {
            if (disposing && this.components != null) {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.list = new ObjectListViewPackageSmoke.DesignerListView();
            this.first = new BrightIdeasSoftware.OLVColumn();
            this.second = new BrightIdeasSoftware.OLVColumn();
            ((System.ComponentModel.ISupportInitialize)this.list).BeginInit();
            this.SuspendLayout();
            //
            // first
            //
            this.first.AspectName = "Name";
            this.first.Text = "Name";
            this.first.Width = 160;
            //
            // second
            //
            this.second.AspectName = "Description";
            this.second.IsTileViewColumn = true;
            this.second.Text = "Description";
            this.second.Width = 180;
            //
            // list
            //
            this.list.AllColumns.Add(this.first);
            this.list.AllColumns.Add(this.second);
            this.list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { this.first, this.second });
            this.list.Location = new System.Drawing.Point(20, 20);
            this.list.Name = "list";
            this.list.ShowGroups = false;
            this.list.Size = new System.Drawing.Size(600, 300);
            this.list.View = System.Windows.Forms.View.Details;
            //
            // DesignerSmokeForm
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(640, 340);
            this.Controls.Add(this.list);
            this.Name = "DesignerSmokeForm";
            this.Text = "ObjectListView packaged designer acceptance";
            ((System.ComponentModel.ISupportInitialize)this.list).EndInit();
            this.ResumeLayout(false);
        }

        private ObjectListViewPackageSmoke.DesignerListView list;
        private BrightIdeasSoftware.OLVColumn first;
        private BrightIdeasSoftware.OLVColumn second;
    }
}
