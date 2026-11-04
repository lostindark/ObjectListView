using System.Linq;
using System.Windows.Forms;
using BrightIdeasSoftware;

namespace ObjectListViewPackageSmoke
{
    public partial class DesignerSmokeForm : Form
    {
        public DesignerSmokeForm() {
            this.InitializeComponent();
        }

        internal void VerifyViewRoundTrip() {
            this.CreateControl();
            this.list.CreateControl();
            var columns = this.list.Columns.Cast<OLVColumn>().ToArray();
            var widths = columns.Select(column => column.Width).ToArray();
            foreach (View view in new[] { View.LargeIcon, View.Tile, View.Details }) {
                this.list.View = view;
                if (this.list.View != view)
                {
                    throw new System.InvalidOperationException("Packaged designer fixture View change failed.");
                }
            }
            if (!this.list.Columns.Cast<OLVColumn>().SequenceEqual(columns) ||
                !columns.Select(column => column.Width).SequenceEqual(widths))
            {
                throw new System.InvalidOperationException("Packaged designer fixture lost its column configuration.");
            }
        }
    }

    public sealed class DesignerRow
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public sealed class DesignerListView : TypedObjectListView<DesignerRow> { }
}
