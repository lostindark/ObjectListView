using System.Drawing;
using System.Windows.Forms;
using BrightIdeasSoftware;

namespace ObjectListViewDemo {
    /// <summary>
    /// This simple class just shows how an overlay can be drawn when the hot item changes.
    /// </summary>
    internal class BusinessCardOverlay : AbstractOverlay
    {
        public BusinessCardOverlay()
        {
            this.businessCardRenderer.HeaderBackColor = DemoTheme.IsDarkMode ? ObjectListViewTheme.ControlLightColor : Color.DarkBlue;
            this.businessCardRenderer.BorderColor = DemoTheme.IsDarkMode ? ObjectListViewTheme.ControlDarkColor : Color.DarkBlue;
            this.businessCardRenderer.BorderWidth = 2;
            this.Transparency = 255;
        }
        #region IOverlay Members

        public override void Draw(ObjectListViewBase olv, Graphics g, Rectangle r)
        {
            if (olv.HotRowIndex < 0)
                return;

            if (olv.View == View.Tile)
                return;

            OLVListItem item = olv.GetItem(olv.HotRowIndex);
            if (item == null)
                return;

            Size cardSize = BusinessCardRenderer.MeasureCardSize(g, olv, 250);
            Size margin = BusinessCardRenderer.ScaleSize(g, new Size(8, 8));
            Rectangle cardBounds = new Rectangle(
                r.Right - cardSize.Width - margin.Width, r.Bottom - cardSize.Height - margin.Height,
                cardSize.Width, cardSize.Height);
            this.businessCardRenderer.DrawBusinessCard(g, cardBounds, item.RowObject, olv, item);
        }

        #endregion

        private readonly BusinessCardRenderer businessCardRenderer = new BusinessCardRenderer();
    }
}