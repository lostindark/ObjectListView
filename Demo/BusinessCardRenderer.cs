using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using BrightIdeasSoftware;
using ObjectListViewDemo.Models;

namespace ObjectListViewDemo {
    /// <summary>
    /// Hackish renderer that draw a fancy version of a person for a Tile view.
    /// </summary>
    /// <remarks>This is not the way to write a professional level renderer.
    /// It is hideously inefficient (we should at least cache the images),
    /// but it is obvious</remarks>
    public class BusinessCardRenderer : AbstractRenderer
    {
        private const int Spacing = 8;
        private const int BorderInset = 2;
        private const float TitleFontSize = 11;
        private const float DetailFontSize = 8;

        internal static Size ScaleSize(Graphics graphics, Size size) {
            return Size.Ceiling(new SizeF(size.Width * graphics.DpiX / 96f, size.Height * graphics.DpiY / 96f));
        }

        internal static Size MeasureCardSize(Graphics graphics, ObjectListViewBase list, int width) {
            Size size = ScaleSize(graphics, new Size(width, 120));
            Size spacing = ScaleSize(graphics, new Size(Spacing, Spacing));
            Size inset = ScaleSize(graphics, new Size(BorderInset, BorderInset));
            int rows = 0;
            foreach (OLVColumn column in list.Columns) {
                if (column.IsTileViewColumn)
                    rows++;
            }
            using (var titleFont = new Font("Tahoma", TitleFontSize))
            using (var detailFont = new Font("Tahoma", DetailFontSize))
            using (var format = new StringFormat(StringFormatFlags.NoWrap)) {
                float titleHeight = graphics.MeasureString("Wj", titleFont, size.Width, format).Height;
                float rowHeight = graphics.MeasureString("Wj", detailFont, size.Width, format).Height;
                int contentHeight = (int)Math.Ceiling(inset.Height * 2 + spacing.Height * 3 +
                    titleHeight + rows * rowHeight);
                return new Size(size.Width, Math.Max(size.Height, contentHeight));
            }
        }

        public override bool RenderItem(DrawListViewItemEventArgs e, Graphics g, Rectangle itemBounds, object rowObject)
        {
            // If we're in any other view than Tile, return false to say that we haven't done
            // the rendering and the default process should do it's stuff
            ObjectListViewBase olv = e.Item.ListView as ObjectListViewBase;
            if (olv == null || olv.View != View.Tile)
                return false;

            // Use buffered graphics to kill flickers
            BufferedGraphics buffered = BufferedGraphicsManager.Current.Allocate(g, itemBounds);
            g = buffered.Graphics;
            g.Clear(olv.BackColor);
            g.SmoothingMode = ObjectListView.SmoothingMode;
            g.TextRenderingHint = ObjectListView.TextRenderingHint;

            if (e.Item.Selected)
            {
                this.BorderPen = Pens.Blue;
                this.HeaderBackBrush = new SolidBrush(olv.SelectedBackColorOrDefault);
            }
            else
            {
                this.BorderPen = new Pen(Color.FromArgb(0x33, 0x33, 0x33));
                this.HeaderBackBrush = new SolidBrush(Color.FromArgb(0x33, 0x33, 0x33));
            }
            DrawBusinessCard(g, itemBounds, rowObject, olv, (OLVListItem)e.Item);

            // Finally render the buffered graphics
            buffered.Render();
            buffered.Dispose();

            // Return true to say that we've handled the drawing
            return true;
        }

        internal Pen BorderPen = new Pen(Color.FromArgb(0x33, 0x33, 0x33));
        internal Brush TextBrush = new SolidBrush(Color.FromArgb(0x22, 0x22, 0x22));
        internal Brush HeaderTextBrush = Brushes.AliceBlue;
        internal Brush HeaderBackBrush = new SolidBrush(Color.FromArgb(0x33, 0x33, 0x33));
        internal Brush BackBrush = Brushes.LemonChiffon;

        public void DrawBusinessCard(Graphics g, Rectangle itemBounds, object rowObject, ObjectListViewBase olv, OLVListItem item)
        {
            Size spacing = ScaleSize(g, new Size(Spacing, Spacing));
            Size inset = ScaleSize(g, new Size(BorderInset, BorderInset));

            // Allow a border around the card
            itemBounds.Inflate(-inset.Width, -inset.Height);

            // Draw card background
            int rounding = ScaleSize(g, new Size(20, 20)).Width;
            GraphicsPath path = this.GetRoundedRect(itemBounds, rounding);
            g.FillPath(this.BackBrush, path);
            g.DrawPath(this.BorderPen, path);

            g.Clip = new Region(itemBounds);

            // Draw the photo
            Rectangle photoRect = itemBounds;
            photoRect.Inflate(-spacing.Width, -spacing.Height);
            Person person = rowObject as Person;
            if (person != null)
            {
                photoRect.Width = ScaleSize(g, new Size(80, 0)).Width;
                string photoFile = String.Format(@".\Photos\{0}.png", person.Photo);
                if (File.Exists(photoFile))
                {
                    using (Image photo = Image.FromFile(photoFile)) {
                        photoRect.Height = (int)Math.Round(photo.Height * ((float)photoRect.Width / photo.Width));
                        g.DrawImage(photo, photoRect);
                    }
                }
                else
                {
                    g.DrawRectangle(Pens.DarkGray, photoRect);
                }
            }

            // Now draw the text portion
            RectangleF textBoxRect = photoRect;
            textBoxRect.X += (photoRect.Width + spacing.Width);
            textBoxRect.Width = itemBounds.Right - textBoxRect.X - spacing.Width;

            StringFormat fmt = new StringFormat(StringFormatFlags.NoWrap);
            fmt.Trimming = StringTrimming.EllipsisCharacter;
            fmt.Alignment = StringAlignment.Center;
            fmt.LineAlignment = StringAlignment.Near;
            String txt = item.Text;

            using (Font font = new Font("Tahoma", TitleFontSize))
            {
                // Measure the height of the title
                SizeF size = g.MeasureString(txt, font, (int)textBoxRect.Width, fmt);
                // Draw the title
                RectangleF r3 = textBoxRect;
                r3.Height = size.Height;
                path = this.GetRoundedRect(r3, ScaleSize(g, new Size(15, 15)).Width);
                g.FillPath(this.HeaderBackBrush, path);
                g.DrawString(txt, font, this.HeaderTextBrush, textBoxRect, fmt);
                textBoxRect.Y += size.Height + spacing.Height;
            }

            // Draw the other bits of information
            using (Font font = new Font("Tahoma", DetailFontSize))
            {
                SizeF size = g.MeasureString("Wj", font, itemBounds.Width, fmt);
                textBoxRect.Height = size.Height;
                fmt.Alignment = StringAlignment.Near;
                for (int i = 0; i < olv.Columns.Count; i++)
                {
                    OLVColumn column = olv.GetColumn(i);
                    if (column.IsTileViewColumn)
                    {
                        txt = column.GetStringValue(rowObject);
                        g.DrawString(txt, font, this.TextBrush, textBoxRect, fmt);
                        textBoxRect.Y += size.Height;
                    }
                }
            }
        }

        private GraphicsPath GetRoundedRect(RectangleF rect, float diameter)
        {
            GraphicsPath path = new GraphicsPath();

            RectangleF arc = new RectangleF(rect.X, rect.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}