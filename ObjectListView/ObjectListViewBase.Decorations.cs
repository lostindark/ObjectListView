/*
 * Copyright (C) 2006-2018 Phillip Piper
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * If you wish to use this code in a closed source application, please contact phillip.piper@gmail.com.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using MethodInvoker = System.Windows.Forms.MethodInvoker;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Threading;

namespace BrightIdeasSoftware
{
    public abstract partial class ObjectListViewBase {
        #region Decorations and Overlays

        /// <summary>
        /// Add the given decoration to those on this list and make it appear
        /// </summary>
        /// <param name="decoration">The decoration</param>
        /// <remarks>
        /// A decoration scrolls with the listview. An overlay stays fixed in place.
        /// </remarks>
        public virtual void AddDecoration(IDecoration decoration) {
            if (decoration == null)
            {
                return;
            }

            this.Decorations.Add(decoration);
            this.Invalidate();
        }

        /// <summary>
        /// Add the given overlay to those on this list and make it appear
        /// </summary>
        /// <param name="overlay">The overlay</param>
        public virtual void AddOverlay(IOverlay overlay) {
            if (overlay == null)
            {
                return;
            }

            this.Overlays.Add(overlay);
            this.Invalidate();
        }

        /// <summary>
        /// Draw all the decorations
        /// </summary>
        /// <param name="g">A Graphics</param>
        /// <param name="itemsThatWereRedrawn">The items that were redrawn and whose decorations should also be redrawn</param>
        protected virtual void DrawAllDecorations(Graphics g, List<OLVListItem> itemsThatWereRedrawn) {
            g.TextRenderingHint = ObjectListViewBase.TextRenderingHint;
            g.SmoothingMode = ObjectListViewBase.SmoothingMode;

            Rectangle contentRectangle = this.ContentRectangle;

            if (this.HasEmptyListMsg && this.GetItemCount() == 0) {
                this.EmptyListMsgOverlay.Draw(this, g, contentRectangle);
            }

            // Let the drop sink draw whatever feedback it likes
            if (this.DropSink != null) {
                this.DropSink.DrawFeedback(g, contentRectangle);
            }

            // Draw our item and subitem decorations
            foreach (OLVListItem olvi in itemsThatWereRedrawn) {
                if (olvi.HasDecoration) {
                    foreach (IDecoration d in olvi.Decorations) {
                        d.ListItem = olvi;
                        d.SubItem = null;
                        d.Draw(this, g, contentRectangle);
                    }
                }
                foreach (OLVListSubItem subItem in olvi.SubItems) {
                    if (subItem.HasDecoration) {
                        foreach (IDecoration d in subItem.Decorations) {
                            d.ListItem = olvi;
                            d.SubItem = subItem;
                            d.Draw(this, g, contentRectangle);
                        }
                    }
                }
                if (this.SelectedRowDecoration != null && olvi.Selected && olvi.Enabled) {
                    this.SelectedRowDecoration.ListItem = olvi;
                    this.SelectedRowDecoration.SubItem = null;
                    this.SelectedRowDecoration.Draw(this, g, contentRectangle);
                }
            }

            // Now draw the specifically registered decorations
            foreach (IDecoration decoration in this.Decorations) {
                decoration.ListItem = null;
                decoration.SubItem = null;
                decoration.Draw(this, g, contentRectangle);
            }

            // Finally, draw any hot item decoration
            if (this.UseHotItem) {
                IDecoration hotItemDecoration = this.HotItemStyleOrDefault.Decoration;
                if (hotItemDecoration != null) {
                    hotItemDecoration.ListItem = this.GetItem(this.HotRowIndex);
                    if (hotItemDecoration.ListItem == null || hotItemDecoration.ListItem.Enabled) {
                        hotItemDecoration.SubItem = hotItemDecoration.ListItem == null ? null : hotItemDecoration.ListItem.GetSubItem(this.HotColumnIndex);
                        hotItemDecoration.Draw(this, g, contentRectangle);
                    }
                }
            }

            // If we are in design mode, we don't want to use the glass panels,
            // so we draw the background overlays here
            if (this.DesignMode) {
                foreach (IOverlay overlay in this.Overlays) {
                    overlay.Draw(this, g, contentRectangle);
                }
            }
        }

        /// <summary>
        /// Is the given decoration shown on this list
        /// </summary>
        /// <param name="decoration">The overlay</param>
        public virtual bool HasDecoration(IDecoration decoration) {
            return this.Decorations.Contains(decoration);
        }

        /// <summary>
        /// Is the given overlay shown on this list?
        /// </summary>
        /// <param name="overlay">The overlay</param>
        public virtual bool HasOverlay(IOverlay overlay) {
            return this.Overlays.Contains(overlay);
        }

        /// <summary>
        /// Hide any overlays.
        /// </summary>
        /// <remarks>
        /// This is only a temporary hiding -- the overlays will be shown
        /// the next time the ObjectListView redraws.
        /// </remarks>
        public virtual void HideOverlays() {
            foreach (GlassPanelForm glassPanel in this.glassPanels) {
                glassPanel.HideGlass();
            }
        }

        /// <summary>
        /// Create and configure the empty list msg overlay
        /// </summary>
        protected virtual void InitializeEmptyListMsgOverlay() {
            TextOverlay overlay = new TextOverlay();
            this.standardEmptyListMsgOverlay?.Dispose();
            this.standardEmptyListMsgOverlay = overlay;
            overlay.Alignment = System.Drawing.ContentAlignment.MiddleCenter;
            IThemeColors colors = ObjectListViewTheme.Current;
            overlay.InitializeDefaultColors(colors.EmptyListTextColor, colors.EmptyListBackgroundColor, colors.ControlDarkColor);
            overlay.BorderWidth = 2.0f;
            this.EmptyListMsgOverlay = overlay;
        }

        /// <summary>
        /// Initialize the standard image and text overlays
        /// </summary>
        protected virtual void InitializeStandardOverlays() {
            this.OverlayImage = new ImageOverlay();
            this.AddOverlay(this.OverlayImage);
            this.standardTextOverlay?.Dispose();
            this.standardTextOverlay = new TextOverlay();
            this.OverlayText = this.standardTextOverlay;
            this.AddOverlay(this.OverlayText);
        }

        /// <summary>
        /// Make sure that any overlays are visible.
        /// </summary>
        public virtual void ShowOverlays() {
            // If we shouldn't show overlays, then don't create glass panels
            if (!this.ShouldShowOverlays())
            {
                return;
            }

            // Make sure that each overlay has its own glass panels
            if (this.Overlays.Count != this.glassPanels.Count) {
                foreach (IOverlay overlay in this.Overlays) {
                    GlassPanelForm glassPanel = this.FindGlassPanelForOverlay(overlay);
                    if (glassPanel == null) {
                        glassPanel = new GlassPanelForm();
                        glassPanel.Bind(this, overlay);
                        this.glassPanels.Add(glassPanel);
                    }
                }
            }
            foreach (GlassPanelForm glassPanel in this.glassPanels) {
                glassPanel.ShowGlass();
            }
        }

        private bool ShouldShowOverlays() {
            // If we are in design mode, we don�t show the overlays
            if (this.DesignMode)
            {
                return false;
            }

            // If we are explicitly not using overlays, also don't show them
            if (!this.UseOverlays)
            {
                return false;
            }

            // If there are no overlays, guess...
            if (!this.HasOverlays)
            {
                return false;
            }

            // If we don't have 32-bit display, alpha blending doesn't work, so again, no overlays
            // TODO: This should actually figure out which screen(s) the control is on, and make sure
            // that each one is 32-bit.
            if (Screen.PrimaryScreen.BitsPerPixel < 32)
            {
                return false;
            }

            // Finally, we can show the overlays
            return true;
        }

        private GlassPanelForm FindGlassPanelForOverlay(IOverlay overlay) {
            return this.glassPanels.Find(delegate(GlassPanelForm x) { return x.Overlay == overlay; });
        }

        /// <summary>
        /// Refresh the display of the overlays
        /// </summary>
        public virtual void RefreshOverlays() {
            foreach (GlassPanelForm glassPanel in this.glassPanels) {
                glassPanel.Invalidate();
            }
        }

        /// <summary>
        /// Refresh the display of just one overlays
        /// </summary>
        public virtual void RefreshOverlay(IOverlay overlay) {
            GlassPanelForm glassPanel = this.FindGlassPanelForOverlay(overlay);
            if (glassPanel != null)
            {
                glassPanel.Invalidate();
            }
        }

        /// <summary>
        /// Remove the given decoration from this list
        /// </summary>
        /// <param name="decoration">The decoration to remove</param>
        public virtual void RemoveDecoration(IDecoration decoration) {
            if (decoration == null)
            {
                return;
            }

            this.Decorations.Remove(decoration);
            this.Invalidate();
        }

        /// <summary>
        /// Remove the given overlay to those on this list
        /// </summary>
        /// <param name="overlay">The overlay</param>
        public virtual void RemoveOverlay(IOverlay overlay) {
            if (overlay == null)
            {
                return;
            }

            this.Overlays.Remove(overlay);
            GlassPanelForm glassPanel = this.FindGlassPanelForOverlay(overlay);
            if (glassPanel != null) {
                this.glassPanels.Remove(glassPanel);
                glassPanel.Unbind();
                glassPanel.Dispose();
            }
        }

        #endregion
    }
}
