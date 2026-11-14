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
        #region Image list manipulation

        /// <summary>
        /// Update our externally visible image list so it holds the same images as our shadow list, but sized correctly
        /// </summary>
        private void SetupBaseImageList() {
            ImageList previous = this.resizedImageList;
            ImageList previousBase = base.SmallImageList;
            ImageList replacement = this.shadowedImageList;
            ImageList resized = null;
            bool attached = false;
            try {
                if (rowHeight != -1 &&
                    this.View == View.Details &&
                    (this.shadowedImageList == null || this.shadowedImageList.ImageSize.Height != rowHeight)) {
                    int width = (this.shadowedImageList == null ? 16 : this.shadowedImageList.ImageSize.Width);
                    resized = MakeResizedImageList(width, rowHeight, shadowedImageList);
                    replacement = resized;
                }
                this.BaseSmallImageList = replacement;
                this.resizedImageList = resized ?? (ReferenceEquals(previous, replacement) ? previous : null);
                attached = true;
                if (previous != null && !ReferenceEquals(previous, replacement))
                {
                    previous.Dispose();
                }
            } finally {
                if (!attached) {
                    try {
                        base.SmallImageList = previousBase;
                    } finally {
                        if (resized != null)
                        {
                            resized.Dispose();
                        }
                    }
                }
            }
        }
        private ImageList resizedImageList;

        /// <summary>
        /// Return a copy of the given source image list, where each image has been resized to be height x height in size.
        /// If source is null, an empty image list of the given size is returned
        /// </summary>
        /// <param name="width">Height and width of the new images</param>
        /// <param name="height">Height and width of the new images</param>
        /// <param name="source">Source of the images (can be null)</param>
        /// <returns>A new image list</returns>
        private static ImageList MakeResizedImageList(int width, int height, ImageList source) {
            ImageList il = new ImageList();
            bool complete = false;
            try {
                il.ImageSize = new Size(width, height);
                if (source != null) {
                    il.TransparentColor = source.TransparentColor;
                    il.ColorDepth = source.ColorDepth;
                    IntPtr handle = il.Handle;
                    var keys = source.Images.Keys;
                    for (int i = 0; i < source.Images.Count; i++) {
                        using (Image image = source.Images[i])
                        using (Bitmap bitmap = MakeResizedImage(width, height, image, source.TransparentColor))
                        {
                            il.Images.Add(i < keys.Count ? keys[i] : string.Empty, bitmap);
                        }
                    }
                }
                complete = true;
                return il;
            } finally {
                if (!complete)
                {
                    il.Dispose();
                }
            }
        }

        /// <summary>
        /// Return a bitmap of the given height x height, which shows the given image, centred.
        /// </summary>
        /// <param name="width">Height and width of new bitmap</param>
        /// <param name="height">Height and width of new bitmap</param>
        /// <param name="image">Image to be centred</param>
        /// <param name="transparent">The background color</param>
        /// <returns>A new bitmap</returns>
        private static Bitmap MakeResizedImage(int width, int height, Image image, Color transparent) {
            Bitmap bm = new Bitmap(width, height);
            bool complete = false;
            try {
                using (Graphics g = Graphics.FromImage(bm)) {
                    g.Clear(transparent);
                    int x = Math.Max(0, (bm.Size.Width - image.Size.Width) / 2);
                    int y = Math.Max(0, (bm.Size.Height - image.Size.Height) / 2);
                    g.DrawImage(image, x, y, image.Size.Width, image.Size.Height);
                }
                complete = true;
                return bm;
            } finally {
                if (!complete)
                {
                    bm.Dispose();
                }
            }
        }

        /// <summary>
        /// Initialize the state image list with the required checkbox images
        /// </summary>
        protected virtual void InitializeStateImageList() {
            if (this.DesignMode)
            {
                return;
            }

            if (!this.CheckBoxes)
            {
                return;
            }

            ImageList source = this.StateImageList;
            bool resize = source != null && this.RowHeight != -1 && this.View == View.Details
                && source.ImageSize.Height != this.RowHeight;
            bool removeMixed = source != null && !this.TriStateCheckBoxes && source.Images.ContainsKey(INDETERMINATE_KEY);
            if (source != null && !resize && !removeMixed && source.Images.Count >= (this.TriStateCheckBoxes ? 3 : 2))
            {
                return;
            }

            int width = source == null || resize ? 16 : source.ImageSize.Width;
            int height = source == null ? this.RowHeight == -1 ? 16 : this.RowHeight
                : resize ? this.RowHeight : source.ImageSize.Height;
            ImageList images = MakeResizedImageList(width, height, resize ? null : source);
            bool attached = false;
            try {
                if (source == null || resize)
                {
                    images.ColorDepth = ColorDepth.Depth32Bit;
                }
                // Populate before attaching: ImageList changes on a live checkbox control can recreate its HWND.
                if (images.Images.Count == 0)
                {
                    AddCheckStateBitmap(images, UNCHECKED_KEY, CheckBoxState.UncheckedNormal);
                }
                if (images.Images.Count <= 1)
                {
                    AddCheckStateBitmap(images, CHECKED_KEY, CheckBoxState.CheckedNormal);
                }
                if (this.TriStateCheckBoxes && images.Images.Count <= 2)
                {
                    AddCheckStateBitmap(images, INDETERMINATE_KEY, CheckBoxState.MixedNormal);
                }
                if (removeMixed)
                {
                    images.Images.RemoveByKey(INDETERMINATE_KEY);
                }

                this.StateImageList = images;
                ImageList previous = this.primaryCheckBoxImageList;
                this.primaryCheckBoxImageList = images;
                attached = true;
                previous?.Dispose();
            } finally {
                if (!attached)
                {
                    images.Dispose();
                }
            }
        }
        private ImageList primaryCheckBoxImageList;

        /// <summary>
        /// The name of the image used when a check box is checked
        /// </summary>
        public const string CHECKED_KEY = "checkbox-checked";

        /// <summary>
        /// The name of the image used when a check box is unchecked
        /// </summary>
        public const string UNCHECKED_KEY = "checkbox-unchecked";

        /// <summary>
        /// The name of the image used when a check box is Indeterminate
        /// </summary>
        public const string INDETERMINATE_KEY = "checkbox-indeterminate";

        /// <summary>
        /// Setup this control so it can display check boxes on subitems
        /// (or primary checkboxes in virtual mode)
        /// </summary>
        /// <remarks>This gives the ListView a small image list, if it doesn't already have one.</remarks>
        public virtual void SetupSubItemCheckBoxes() {
            if (this.initializingColumns)
            {
                return;
            }

            this.ShowImagesOnSubItems = true;
            if (this.SmallImageList == null || !this.SmallImageList.Images.ContainsKey(CHECKED_KEY))
            {
                this.InitializeSubItemCheckBoxImages();
            }
        }

        /// <summary>
        /// Make sure the small image list for this control has checkbox images 
        /// (used for sub-item checkboxes).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This gives the ListView a small image list, if it doesn't already have one.
        /// </para>
        /// <para>
        /// ObjectListView has to manage checkboxes on subitems separate from the checkboxes on each row.
        /// The underlying ListView knows about the per-row checkboxes, and to make them work, OLV has to 
        /// correctly configure the StateImageList. However, the ListView cannot do checkboxes in subitems,
        /// so ObjectListView has to handle them in a different fashion. So, per-row checkboxes are controlled
        /// by images in the StateImageList, but per-cell checkboxes are handled by images in the SmallImageList.
        /// </para>
        /// </remarks>
        protected virtual void InitializeSubItemCheckBoxImages() {
            // Don't mess with the image list in design mode
            if (this.DesignMode)
            {
                return;
            }

            // Legacy serialized image lists can have no spare bitmap capacity for appended states.
            ImageList source = this.SmallImageList;
            ImageList il = MakeResizedImageList(source == null ? 16 : source.ImageSize.Width,
                source == null ? 16 : source.ImageSize.Height, source);
            bool attached = false;
            try {
                if (source == null)
                {
                    il.ColorDepth = ColorDepth.Depth32Bit;
                }

                AddCheckStateBitmap(il, CHECKED_KEY, CheckBoxState.CheckedNormal);
                AddCheckStateBitmap(il, UNCHECKED_KEY, CheckBoxState.UncheckedNormal);
                AddCheckStateBitmap(il, INDETERMINATE_KEY, CheckBoxState.MixedNormal);

                this.SmallImageList = il;
                ImageList previous = this.subItemCheckBoxImageList;
                this.subItemCheckBoxImageList = il;
                attached = true;
                if (previous != null)
                {
                    previous.Dispose();
                }
            } finally {
                if (!attached) {
                    this.shadowedImageList = source;
                    il.Dispose();
                }
            }
        }
        private ImageList subItemCheckBoxImageList;

        private static void AddCheckStateBitmap(ImageList il, string key, CheckBoxState boxState) {
            using (Bitmap b = new Bitmap(il.ImageSize.Width, il.ImageSize.Height)) {
                using (Graphics g = Graphics.FromImage(b)) {
                    g.Clear(il.TransparentColor);
                    Point location = new Point(b.Width / 2 - 5, b.Height / 2 - 6);
                    ThemedRendering.DrawCheckBox(g, location, boxState);
                }
                IntPtr handle = il.Handle;
                il.Images.Add(key, b);
            }
        }

        #endregion
    }
}
