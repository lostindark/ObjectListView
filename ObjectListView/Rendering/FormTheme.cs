using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using CheckBoxState = System.Windows.Forms.VisualStyles.CheckBoxState;
using RadioButtonState = System.Windows.Forms.VisualStyles.RadioButtonState;

namespace BrightIdeasSoftware
{
    /// <summary>
    /// Opt-in standard WinForms theming, with per-control customization after standard styling.
    /// Attach once after creating a form's controls. Disposal of the root detaches the service.
    /// </summary>
    public sealed class FormTheme : NativeWindow, IDisposable
    {
        private static readonly ConditionalWeakTable<Control, FormTheme> Forms = new ConditionalWeakTable<Control, FormTheme>();
        private static readonly ConditionalWeakTable<Control, Appearance> SharedAppearances = new ConditionalWeakTable<Control, Appearance>();
        private static readonly ConditionalWeakTable<ToolStripItem, ItemAppearance> SharedItemAppearances = new ConditionalWeakTable<ToolStripItem, ItemAppearance>();
        private readonly Control root;
        private readonly Form form;
        private Action<IThemeColors> themeChanged;
        private Action<Control, IThemeColors> customizeControl;
        private readonly ConditionalWeakTable<Control, Appearance> appearances = new ConditionalWeakTable<Control, Appearance>();
        private readonly ConditionalWeakTable<ToolStripItem, ItemAppearance> itemAppearances = new ConditionalWeakTable<ToolStripItem, ItemAppearance>();
        private readonly List<WeakReference<Control>> trackedControls = new List<WeakReference<Control>>();
        private IThemeColors palette;
        private FormFirstFrame firstFrame;
        private bool applying;
        private bool updatePending;
        private bool refreshNativeColorsPending;
        private bool disposed;

        private FormTheme(Control root, Action<Control, IThemeColors> customizeControl, Action<IThemeColors> themeChanged)
        {
            this.root = root;
            this.form = root as Form;
            this.customizeControl = customizeControl;
            this.themeChanged = themeChanged;
            root.Disposed += this.FormDisposed;
            if (this.form != null)
            {
                this.form.HandleCreated += this.FormHandleCreated;
                this.form.HandleDestroyed += this.FormHandleDestroyed;
                if (this.form.IsHandleCreated)
                {
                    this.AssignHandle(this.form.Handle);
                }
            }
        }

        /// <summary>Gets the most recently resolved application palette.</summary>
        public static ThemePalette Current { get; private set; } = new ThemePalette(false, SystemInformation.HighContrast);

        /// <summary>Resamples the configured palette before controls are created.</summary>
        public static void Initialize()
        {
            Current = ReadPalette();
        }

        /// <summary>
        /// Changes the shared mode and refreshes attached, open forms.
        /// Light/Dark also configure WinForms on .NET 10; System preserves the host's current mode.
        /// </summary>
        public static void SetColorMode(ObjectListViewColorMode mode)
        {
            ObjectListViewTheme.ColorMode = mode;
#if NET10_0_OR_GREATER
            if (mode != ObjectListViewColorMode.System)
            {
                Application.SetColorMode(mode == ObjectListViewColorMode.Dark ? SystemColorMode.Dark
                    : SystemColorMode.Classic);
            }
#endif
            RefreshAll();
        }

        /// <summary>
        /// Themes a form and tracks system changes, new controls, and handle recreation.
        /// IThemeAware runs after children and standard styling, followed by the customization callback.
        /// themeChanged runs last.
        /// Reattaching preserves existing callbacks unless non-null replacements are provided.
        /// </summary>
        public static FormTheme Attach(Form form, Action<Control, IThemeColors> customizeControl = null, Action<IThemeColors> themeChanged = null)
        {
            return Attach(form, false, customizeControl, themeChanged);
        }

        /// <summary>
        /// Attaches form theming with optional first-frame preparation for dark mode.
        /// Opt in before the form's first Load or Show to paint before restoring its configured opacity.
        /// Light mode and subsequent hide/show operations are unchanged.
        /// </summary>
        public static FormTheme Attach(Form form, bool prepareFirstFrame, Action<Control, IThemeColors> customizeControl = null, Action<IThemeColors> themeChanged = null)
        {
            FormTheme theme = Apply(form, customizeControl, themeChanged);
            if (prepareFirstFrame && theme.firstFrame == null && !form.Visible)
            {
                theme.firstFrame = FormFirstFrame.Attach(form, () => theme.palette.Dark);
            }
            return theme;
        }

        /// <summary>
        /// Themes a control subtree, preserving explicit colors. Standalone subtrees can be reapplied
        /// explicitly; attach their containing form for automatic Windows theme notifications.
        /// </summary>
        public static FormTheme Apply(Control root, Action<Control, IThemeColors> customizeControl = null, Action<IThemeColors> themeChanged = null)
        {
            ArgumentNullException.ThrowIfNull(root, nameof(root));

            var theme = Forms.GetValue(root, owner => new FormTheme(owner, customizeControl, themeChanged));
            if (customizeControl != null)
            {
                theme.customizeControl = customizeControl;
            }
            if (themeChanged != null)
            {
                theme.themeChanged = themeChanged;
            }
            Current = ReadPalette();
            theme.Apply(Current);
            return theme;
        }

        /// <summary>Resamples System mode and color overrides, then reapplies attached open forms.</summary>
        public static void RefreshAll()
        {
            Current = ReadPalette();
            foreach (Form openForm in Application.OpenForms.Cast<Form>().ToArray())
            {
                RefreshRoot(openForm);
            }
        }

        private static void RefreshRoot(Control root)
        {
            if (Forms.TryGetValue(root, out FormTheme theme))
            {
                theme.Apply(Current);
                return;
            }
            foreach (Control child in root.Controls)
            {
                RefreshRoot(child);
            }
        }

        private static ThemePalette ReadPalette()
        {
            if (ObjectListViewTheme.ColorMode == ObjectListViewColorMode.System)
            {
#if NETFRAMEWORK
                ObjectListViewTheme.ColorMode = ObjectListViewColorMode.System;
#else
                if (Application.ColorMode == SystemColorMode.System
                    && Environment.OSVersion.Version.Build >= 22000
                    && Application.IsDarkModeEnabled != ObjectListViewTheme.SystemPrefersDark)
                {
                    Application.SetColorMode(SystemColorMode.System);
                }
#endif
            }
            return new ThemePalette(ObjectListViewTheme.IsDarkMode, SystemInformation.HighContrast);
        }

        /// <summary>Reapplies this root using a resolved palette and its customization callbacks.</summary>
        public void Apply(IThemeColors colors)
        {
            ArgumentNullException.ThrowIfNull(colors, nameof(colors));

            if (this.disposed || this.applying)
            {
                return;
            }

            this.palette = colors;
            if (ObjectListViewTheme.IsDarkMode != colors.Dark)
            {
                ObjectListViewTheme.ColorMode = colors.Dark ? ObjectListViewColorMode.Dark : ObjectListViewColorMode.Light;
            }
            this.applying = true;
            this.root.SuspendLayout();
            try
            {
                this.Capture(this.root);
                this.ApplyControl(this.root);
                this.ApplyTitleBar();
                this.themeChanged?.Invoke(colors);
                this.root.Invalidate(true);
            }
            finally
            {
                this.root.ResumeLayout();
                this.applying = false;
            }
        }

        private void Capture(Control control)
        {
            this.appearances.GetValue(control, item =>
            {
                var appearance = SharedAppearances.GetValue(item, owner => new Appearance(owner));
                appearance.Owners.Add(new WeakReference<FormTheme>(this));
                item.ControlAdded += this.ControlAdded;
                item.ControlRemoved += this.ControlRemoved;
                item.Disposed += this.ControlDisposed;
                item.BackColorChanged += this.BackColorChanged;
                item.ForeColorChanged += this.ForeColorChanged;
                this.trackedControls.Add(new WeakReference<Control>(item));
                return appearance;
            });
            foreach (Control child in control.Controls)
            {
                this.Capture(child);
            }
        }

        private void ApplyControl(Control control)
        {
            if (!this.appearances.TryGetValue(control, out Appearance original))
            {
                this.Capture(control);
                original = this.appearances.GetValue(control, item => new Appearance(item));
            }
            original.Applying = true;
            try
            {
                this.ApplyAppearance(control, original);
            }
            finally
            {
                original.AppliedBackColor = control.BackColor;
                original.AppliedForeColor = control.ForeColor;
                original.Applying = false;
            }

            foreach (Control child in control.Controls)
            {
                this.ApplyControl(child);
            }

            if (control.ContextMenuStrip != null)
            {
                this.ApplyControl(control.ContextMenuStrip);
            }

            if (control is ObjectListViewBase list)
            {
                list.RefreshTheme();
            }

            if (control is IThemeAware themeAware)
            {
                themeAware.ApplyTheme(this.palette);
            }

            this.customizeControl?.Invoke(control, this.palette);
        }

        private bool ManualLight => !this.palette.Dark && !this.palette.HighContrast && HostUsesNativeDarkMode;

        private bool CustomColors => this.palette.Dark || this.palette.HighContrast || this.ManualLight
            || ObjectListViewTheme.HasColorOverrides;

        private void ApplyAppearance(Control control, Appearance original)
        {
            bool custom = this.CustomColors;
            bool manualDark = this.palette.Dark && !NativeDarkModeEnabled;
            bool manualColors = manualDark || this.ManualLight;
            bool input = control is TextBoxBase || control is ComboBox || control is UpDownBase;
            control.BackColor = custom && (this.palette.HighContrast || original.BackColor.IsEmpty)
                ? (control is ListView || control is ListBox ? this.palette.WindowColor
                    : input ? this.palette.InputColor : this.palette.ControlColor) : original.BackColor;
            control.ForeColor = custom && (this.palette.HighContrast || original.ForeColor.IsEmpty)
                ? this.palette.TextColor : original.ForeColor;
            if (control is ObjectListViewBase && !this.palette.HighContrast)
            {
                // Keep default colors theme-bound instead of turning resolved colors into overrides.
                control.BackColor = original.BackColor;
                control.ForeColor = original.ForeColor;
            }

            if (control is Button button)
            {
                bool customButton = manualColors || ObjectListViewTheme.HasButtonColorOverrides;
                button.FlatStyle = customButton ? FlatStyle.Flat : original.FlatStyle;
                button.UseVisualStyleBackColor = !customButton && original.UseVisualStyleBackColor;
                button.EnabledChanged -= this.ButtonEnabledChanged;
                if (customButton)
                {
                    button.FlatAppearance.BorderColor = this.palette.ControlDarkColor;
                    this.ApplyButtonBackground(button, original);
                    button.EnabledChanged += this.ButtonEnabledChanged;
                    button.FlatAppearance.MouseOverBackColor = this.palette.ButtonHoverColor;
                    button.FlatAppearance.MouseDownBackColor = this.palette.ButtonPressedColor;
                }
                else
                {
                    button.FlatAppearance.BorderColor = original.ButtonBorder;
                    button.FlatAppearance.MouseOverBackColor = original.ButtonHover;
                    button.FlatAppearance.MouseDownBackColor = original.ButtonPressed;
                }
                if (custom && (this.palette.HighContrast || original.ForeColor.IsEmpty))
                {
                    button.ForeColor = this.palette.ButtonTextColor;
                }
            }

            if (control is CheckBox || control is RadioButton)
            {
                var buttonBase = (ButtonBase)control;
                buttonBase.FlatStyle = manualColors ? FlatStyle.Flat : original.FlatStyle;
                buttonBase.UseVisualStyleBackColor = !manualColors && original.UseVisualStyleBackColor;
            }

            if (control is ButtonBase captionButton)
            {
                captionButton.Paint -= this.DisabledButtonPaint;
                bool customDisabledText = captionButton is Button
                    ? ObjectListViewTheme.HasDisabledButtonTextOverride
                    : ObjectListViewTheme.HasMutedTextOverride;
                if (manualColors || customDisabledText)
                {
                    captionButton.Paint += this.DisabledButtonPaint;
                }
            }

            if (control is GroupBox group)
            {
                group.Paint -= this.GroupPaint;
                if (manualColors)
                {
                    group.Paint += this.GroupPaint;
                }
            }

            if (control is LinkLabel link)
            {
                link.LinkColor = custom && (this.palette.HighContrast || !original.HasLinkColor) ? this.palette.LinkColor : original.LinkColor;
                link.VisitedLinkColor = custom && (this.palette.HighContrast || !original.HasVisitedLinkColor) ? this.palette.VisitedLinkColor : original.VisitedLinkColor;
                link.ActiveLinkColor = custom && (this.palette.HighContrast || !original.HasActiveLinkColor) ? this.palette.LinkColor : original.ActiveLinkColor;
            }

            if (control is ToolStrip strip)
            {
                if (custom && (strip is MenuStrip || strip is ToolStripDropDown)
                    && (this.palette.HighContrast || original.BackColor.IsEmpty))
                {
                    strip.BackColor = this.palette.MenuColor;
                }
                if (!original.HasCustomRenderer)
                {
                    if (custom)
                    {
                        strip.Renderer = new ThemeRenderer(this.palette);
                    }
                    else
                    {
                        strip.RenderMode = original.RenderMode;
                    }
                }
                this.ApplyMenuItems(strip.Items);
            }

            if (control is ComboBox combo)
            {
                bool drawItems = custom && !this.palette.HighContrast && !NativeDarkModeEnabled && original.DrawMode == DrawMode.Normal
                    && combo.DropDownStyle == ComboBoxStyle.DropDownList;
                // Standard DropDownList chrome can retain a white face even with native dark mode enabled.
                bool flat = custom && !this.palette.HighContrast && original.DrawMode == DrawMode.Normal
                    && combo.DropDownStyle == ComboBoxStyle.DropDownList;
                combo.FlatStyle = flat ? FlatStyle.Flat : original.FlatStyle;
                combo.DrawMode = drawItems ? DrawMode.OwnerDrawFixed : original.DrawMode;
                combo.DrawItem -= this.ComboDrawItem;
                if (drawItems)
                {
                    combo.DrawItem += this.ComboDrawItem;
                }
                if (custom && (this.palette.HighContrast || original.BackColor.IsEmpty))
                {
                    combo.BackColor = this.palette.InputColor;
                }
            }

            if (control is TabPage page)
            {
                page.UseVisualStyleBackColor = !custom && original.UseVisualStyleBackColor;
            }

            if (control is DataGridView grid)
            {
                this.ApplyGrid(grid, original, custom);
                grid.Paint -= this.GridPaint;
                if (NativeListViewTheme.ShouldPaint)
                {
                    grid.Paint += this.GridPaint;
                }
            }

            if ((control is TextBoxBase textBox && textBox.Multiline) || control is ScrollBar)
            {
                if (original.ScrollBarTheme == null && NativeListViewTheme.ShouldPaint)
                {
                    original.ScrollBarTheme = new ControlScrollBarTheme(control);
                }
                original.ScrollBarTheme?.Apply();
            }

        }

        private static bool NativeDarkModeEnabled => HostUsesNativeDarkMode && ObjectListViewTheme.IsDarkMode;

        private static bool HostUsesNativeDarkMode
        {
            get
            {
#if NET10_0_OR_GREATER
                return Application.IsDarkModeEnabled;
#else
                return false;
#endif
            }
        }

        private void ComboDrawItem(object sender, DrawItemEventArgs e)
        {
            var combo = (ComboBox)sender;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            Color background = selected ? this.palette.HighlightColor : combo.BackColor;
            Color foreground = !combo.Enabled ? this.palette.GrayTextColor : selected ? this.palette.HighlightTextColor : combo.ForeColor;
            using (var brush = new SolidBrush(background))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
            string text = e.Index >= 0 ? combo.GetItemText(combo.Items[e.Index]) : combo.Text;
            TextRenderer.DrawText(e.Graphics, text, e.Font, e.Bounds, foreground,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            e.DrawFocusRectangle();
        }

        private void ApplyGrid(DataGridView grid, Appearance original, bool custom)
        {
            grid.EnableHeadersVisualStyles = !custom && original.GridHeaderVisualStyles;
            grid.BackgroundColor = custom && (this.palette.HighContrast || !original.HasGridBackground)
                ? this.palette.WindowColor : original.GridBackground;
            grid.GridColor = custom && (this.palette.HighContrast || !original.HasGridColor)
                ? this.palette.ControlDarkColor : original.GridColor;
            grid.DefaultCellStyle = this.GridStyle(original.GridCells, this.palette.WindowColor, custom, original.HasGridCells);
            grid.ColumnHeadersDefaultCellStyle = this.GridStyle(original.GridColumns, this.palette.ControlColor, custom, original.HasGridColumns);
            grid.RowHeadersDefaultCellStyle = this.GridStyle(original.GridRows, this.palette.ControlColor, custom, original.HasGridRows);
        }

        private void GridPaint(object sender, PaintEventArgs e)
        {
            if (!NativeListViewTheme.ShouldPaint)
            {
                return;
            }
            var grid = (DataGridView)sender;
            HScrollBar horizontal = grid.Controls.OfType<HScrollBar>().FirstOrDefault(bar => bar.Visible);
            VScrollBar vertical = grid.Controls.OfType<VScrollBar>().FirstOrDefault(bar => bar.Visible);
            if (horizontal != null && vertical != null)
            {
                using (var brush = new SolidBrush(ObjectListViewTheme.Current.ScrollBarTrackColor))
                {
                    e.Graphics.FillRectangle(brush, vertical.Left, horizontal.Top, vertical.Width, horizontal.Height);
                }
            }
        }

        private DataGridViewCellStyle GridStyle(DataGridViewCellStyle original, Color background, bool custom, bool explicitStyle)
        {
            var style = original.Clone();
            if (custom)
            {
                if (this.palette.HighContrast || style.BackColor.IsEmpty || (!explicitStyle && style.BackColor.IsSystemColor))
                {
                    style.BackColor = background;
                }
                if (this.palette.HighContrast || style.ForeColor.IsEmpty || (!explicitStyle && style.ForeColor.IsSystemColor))
                {
                    style.ForeColor = this.palette.TextColor;
                }
                if (this.palette.HighContrast || style.SelectionBackColor.IsEmpty || (!explicitStyle && style.SelectionBackColor.IsSystemColor))
                {
                    style.SelectionBackColor = this.palette.HighlightColor;
                }
                if (this.palette.HighContrast || style.SelectionForeColor.IsEmpty || (!explicitStyle && style.SelectionForeColor.IsSystemColor))
                {
                    style.SelectionForeColor = this.palette.HighlightTextColor;
                }
            }
            return style;
        }

        private void ApplyMenuItems(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                var original = this.itemAppearances.GetValue(item,
                    entry => SharedItemAppearances.GetValue(entry, owner => new ItemAppearance(owner)));
                bool custom = this.CustomColors;
                item.ForeColor = custom && (this.palette.HighContrast || original.ForeColor.IsEmpty)
                    ? this.palette.TextColor : original.ForeColor;
                if (item is ToolStripDropDownItem dropDown)
                {
                    dropDown.DropDownOpening -= this.DropDownOpening;
                    dropDown.DropDownOpening += this.DropDownOpening;
                    if (dropDown.HasDropDownItems)
                    {
                        this.ApplyControl(dropDown.DropDown);
                    }
                }
            }
        }

        private void GroupPaint(object sender, PaintEventArgs e)
        {
            var group = (GroupBox)sender;
            Color background = group.BackColor.A == 0 ? group.Parent?.BackColor ?? this.palette.ControlColor : group.BackColor;
            using (var brush = new SolidBrush(background))
            using (var pen = new Pen(this.palette.ControlDarkColor))
            {
                e.Graphics.FillRectangle(brush, group.ClientRectangle);
                int top = string.IsNullOrEmpty(group.Text) ? 0 : group.Font.Height / 2;
                if (group.ClientSize.Width > 1 && group.ClientSize.Height > top + 1)
                {
                    e.Graphics.DrawRectangle(pen, 0, top, group.ClientSize.Width - 1, group.ClientSize.Height - top - 1);
                }
            }
            var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
            Size size = TextRenderer.MeasureText(e.Graphics, group.Text, group.Font, Size.Empty, flags);
            int inset = (int)Math.Ceiling(8 * e.Graphics.DpiX / 96f);
            int x = group.RightToLeft == RightToLeft.Yes ? group.ClientSize.Width - inset - size.Width : inset;
            TextRenderer.DrawText(e.Graphics, group.Text, group.Font, new Rectangle(x, 0, size.Width, size.Height),
                group.Enabled ? group.ForeColor : this.palette.GrayTextColor, background, flags);
        }

        private void ApplyButtonBackground(Button button, Appearance original)
        {
            if (this.palette.HighContrast || original.BackColor.IsEmpty)
            {
                bool wasApplying = original.Applying;
                original.Applying = true;
                try
                {
                    button.BackColor = button.Enabled ? this.palette.ButtonColor : this.palette.ButtonDisabledColor;
                    original.AppliedBackColor = button.BackColor;
                }
                finally
                {
                    original.Applying = wasApplying;
                }
            }
        }

        private void ButtonEnabledChanged(object sender, EventArgs e)
        {
            var button = (Button)sender;
            if (this.appearances.TryGetValue(button, out Appearance original))
            {
                this.ApplyButtonBackground(button, original);
            }
        }

        private void DisabledButtonPaint(object sender, PaintEventArgs e)
        {
            var button = (ButtonBase)sender;
            if (button.Enabled || string.IsNullOrEmpty(button.Text) || button.Image != null || button.ImageList != null)
            {
                return;
            }

            Rectangle bounds = Rectangle.FromLTRB(button.Padding.Left + 2, button.Padding.Top + 2,
                button.ClientSize.Width - button.Padding.Right - 2, button.ClientSize.Height - button.Padding.Bottom - 2);
            ContentAlignment alignment = button.TextAlign;
            bool rtl = button.RightToLeft == RightToLeft.Yes;
            if (button is CheckBox || button is RadioButton)
            {
                using (var background = new SolidBrush(button.BackColor))
                {
                    e.Graphics.FillRectangle(background, button.ClientRectangle);
                }
                Size glyphSize = button is CheckBox
                    ? CheckBoxRenderer.GetGlyphSize(e.Graphics, CheckBoxState.UncheckedDisabled)
                    : RadioButtonRenderer.GetGlyphSize(e.Graphics, RadioButtonState.UncheckedDisabled);
                ContentAlignment checkAlignment = button is CheckBox check ? check.CheckAlign : ((RadioButton)button).CheckAlign;
                bool glyphRight = checkAlignment == ContentAlignment.TopRight
                    || checkAlignment == ContentAlignment.MiddleRight || checkAlignment == ContentAlignment.BottomRight;
                int glyphX = glyphRight != rtl ? button.ClientSize.Width - glyphSize.Width : 0;
                int glyphY = checkAlignment == ContentAlignment.TopLeft || checkAlignment == ContentAlignment.TopCenter
                    || checkAlignment == ContentAlignment.TopRight ? 0
                    : checkAlignment == ContentAlignment.BottomLeft || checkAlignment == ContentAlignment.BottomCenter
                        || checkAlignment == ContentAlignment.BottomRight ? button.ClientSize.Height - glyphSize.Height
                        : (button.ClientSize.Height - glyphSize.Height) / 2;
                var glyph = new Rectangle(glyphX, glyphY, glyphSize.Width - 1, glyphSize.Height - 1);
                using (var pen = new Pen(this.palette.GrayTextColor))
                using (var mark = new SolidBrush(this.palette.GrayTextColor))
                {
                    if (button is RadioButton radio)
                    {
                        e.Graphics.DrawEllipse(pen, glyph);
                        if (radio.Checked)
                        {
                            glyph.Inflate(-glyph.Width / 4, -glyph.Height / 4);
                            e.Graphics.FillEllipse(mark, glyph);
                        }
                    }
                    else
                    {
                        e.Graphics.DrawRectangle(pen, glyph);
                        var state = ((CheckBox)button).CheckState;
                        if (state == CheckState.Indeterminate)
                        {
                            glyph.Inflate(-glyph.Width / 4, -glyph.Height / 4);
                            e.Graphics.FillRectangle(mark, glyph);
                        }
                        else if (state == CheckState.Checked)
                        {
                            using (var tick = new Pen(this.palette.GrayTextColor, Math.Max(1.5f, glyph.Width / 7f)))
                            {
                                e.Graphics.DrawLines(tick, new[]
                                {
                                    new PointF(glyph.Left + glyph.Width * 0.2f, glyph.Top + glyph.Height * 0.5f),
                                    new PointF(glyph.Left + glyph.Width * 0.4f, glyph.Top + glyph.Height * 0.75f),
                                    new PointF(glyph.Left + glyph.Width * 0.8f, glyph.Top + glyph.Height * 0.25f),
                                });
                            }
                        }
                    }
                }

                int spacing = (int)Math.Ceiling(3 * e.Graphics.DpiX / 96f);
                if (glyphRight != rtl)
                {
                    bounds.Width -= glyphSize.Width + spacing;
                }
                else
                {
                    bounds.X += glyphSize.Width + spacing;
                    bounds.Width -= glyphSize.Width + spacing;
                }
            }

            var flags = TextFormatFlags.WordBreak | TextFormatFlags.PreserveGraphicsClipping;
            if (!button.UseMnemonic)
            {
                flags |= TextFormatFlags.NoPrefix;
            }
            else
            {
                flags |= TextFormatFlags.HidePrefix;
            }

            bool right = alignment == ContentAlignment.TopRight || alignment == ContentAlignment.MiddleRight
                || alignment == ContentAlignment.BottomRight;
            bool center = alignment == ContentAlignment.TopCenter || alignment == ContentAlignment.MiddleCenter
                || alignment == ContentAlignment.BottomCenter;
            flags |= center ? TextFormatFlags.HorizontalCenter : right != rtl ? TextFormatFlags.Right : TextFormatFlags.Left;
            flags |= alignment == ContentAlignment.TopLeft || alignment == ContentAlignment.TopCenter || alignment == ContentAlignment.TopRight
                ? TextFormatFlags.Top : alignment == ContentAlignment.BottomLeft || alignment == ContentAlignment.BottomCenter
                    || alignment == ContentAlignment.BottomRight ? TextFormatFlags.Bottom : TextFormatFlags.VerticalCenter;
            if (rtl)
            {
                flags |= TextFormatFlags.RightToLeft;
            }

            TextRenderer.DrawText(e.Graphics, button.Text, button.Font, bounds,
                button is Button ? this.palette.ButtonDisabledTextColor : this.palette.GrayTextColor, button.BackColor, flags);
        }

        /// <summary>Themes a dynamically generated menu using this root's current palette.</summary>
        public void ApplyMenu(ToolStripDropDown menu)
        {
            if (this.disposed)
            {
                return;
            }

            this.ApplyControl(menu);
        }

        /// <summary>Themes a generated menu if its owning form has opted into form theming.</summary>
        public static void ApplyMenu(Form form, ToolStripDropDown menu)
        {
            if (form != null && menu != null && Forms.TryGetValue(form, out FormTheme theme))
            {
                theme.ApplyMenu(menu);
            }
        }

        private void DropDownOpening(object sender, EventArgs e)
        {
            this.ApplyControl(((ToolStripDropDownItem)sender).DropDown);
        }

        private void ControlAdded(object sender, ControlEventArgs e)
        {
            if (!this.disposed && !this.applying)
            {
                this.ApplyControl(e.Control);
            }
        }

        private void ControlRemoved(object sender, ControlEventArgs e) => this.Detach(e.Control);

        private void ControlDisposed(object sender, EventArgs e) => this.Detach((Control)sender);

        private void BackColorChanged(object sender, EventArgs e)
        {
            var control = (Control)sender;
            if (this.appearances.TryGetValue(control, out Appearance appearance) && !appearance.Applying)
            {
                bool explicitColor = TypeDescriptor.GetProperties(control)[nameof(Control.BackColor)].ShouldSerializeValue(control);
                if (!explicitColor || control.BackColor != appearance.AppliedBackColor)
                {
                    appearance.BackColor = explicitColor ? control.BackColor : Color.Empty;
                    if (explicitColor && control is ButtonBase button)
                    {
                        appearance.UseVisualStyleBackColor = button.UseVisualStyleBackColor;
                    }
                }
            }
        }

        private void ForeColorChanged(object sender, EventArgs e)
        {
            var control = (Control)sender;
            if (this.appearances.TryGetValue(control, out Appearance appearance) && !appearance.Applying)
            {
                bool explicitColor = TypeDescriptor.GetProperties(control)[nameof(Control.ForeColor)].ShouldSerializeValue(control);
                if (!explicitColor || control.ForeColor != appearance.AppliedForeColor)
                {
                    appearance.ForeColor = explicitColor ? control.ForeColor : Color.Empty;
                }
            }
        }

        private void FormHandleCreated(object sender, EventArgs e)
        {
            this.AssignHandle(this.form.Handle);
            this.ApplyTitleBar();
        }

        private void FormHandleDestroyed(object sender, EventArgs e)
        {
            this.ReleaseHandle();
        }

        private void ApplyTitleBar()
        {
            // Probe the attribute: Framework hosts without a supportedOS manifest report an older Windows version.
            if (this.form != null && this.form.IsHandleCreated)
            {
                int enabled = this.palette?.Dark == true ? 1 : 0;
                int result = DwmSetWindowAttribute(this.form.Handle, 20, ref enabled, sizeof(int));
                if (result < 0)
                {
                    Trace.TraceWarning($"Could not set the window title bar theme: 0x{result:X8}");
                }
            }
        }

        /// <inheritdoc />
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if ((m.Msg == 0x001A || m.Msg == 0x031A || m.Msg == 0x0015)
                && !this.disposed && !this.applying && this.form.IsHandleCreated)
            {
                this.refreshNativeColorsPending |= m.Msg == 0x031A || m.Msg == 0x0015;
                if (this.updatePending)
                {
                    return;
                }
                this.updatePending = true;
                this.form.BeginInvoke(new Action(() =>
                {
                    this.updatePending = false;
                    bool refreshColors = this.refreshNativeColorsPending;
                    this.refreshNativeColorsPending = false;
                    if (!this.disposed)
                    {
                        var current = ReadPalette();
                        if (refreshColors || current.Dark != Current.Dark || current.HighContrast != Current.HighContrast)
                        {
                            RefreshAll();
                        }
                    }
                }));
            }
        }

        private void FormDisposed(object sender, EventArgs e) => this.Dispose();

        /// <summary>Detaches handlers and restores captured styling without disposing controls.</summary>
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.firstFrame?.Dispose();
            if (this.form != null)
            {
                this.form.HandleCreated -= this.FormHandleCreated;
                this.form.HandleDestroyed -= this.FormHandleDestroyed;
            }
            this.root.Disposed -= this.FormDisposed;
            foreach (WeakReference<Control> reference in this.trackedControls.ToArray())
            {
                if (reference.TryGetTarget(out Control control))
                {
                    this.Detach(control);
                }
            }
            this.ReleaseHandle();
            Forms.Remove(this.root);
            GC.SuppressFinalize(this);
        }

        private void Detach(Control control)
        {
            if (!this.appearances.TryGetValue(control, out Appearance appearance))
            {
                return;
            }
            this.appearances.Remove(control);
            bool shared = appearance.Owners.Any(reference => reference.TryGetTarget(out FormTheme owner)
                && owner.appearances.TryGetValue(control, out Appearance captured) && ReferenceEquals(captured, appearance));
            appearance.Owners.RemoveAll(reference => !reference.TryGetTarget(out FormTheme owner) || owner == this);
            this.trackedControls.RemoveAll(reference => !reference.TryGetTarget(out Control target) || target == control);
            if (!shared)
            {
                SharedAppearances.Remove(control);
                appearance.ScrollBarTheme?.Dispose();
            }
            control.ControlAdded -= this.ControlAdded;
            control.ControlRemoved -= this.ControlRemoved;
            control.Disposed -= this.ControlDisposed;
            control.BackColorChanged -= this.BackColorChanged;
            control.ForeColorChanged -= this.ForeColorChanged;
            if (control is Button pushButton)
            {
                pushButton.EnabledChanged -= this.ButtonEnabledChanged;
            }
            if (control is ButtonBase button)
            {
                button.Paint -= this.DisabledButtonPaint;
            }
            if (control is GroupBox group)
            {
                group.Paint -= this.GroupPaint;
            }
            if (control is ComboBox combo)
            {
                combo.DrawItem -= this.ComboDrawItem;
            }
            if (control is DataGridView grid)
            {
                grid.Paint -= this.GridPaint;
            }
            if (control is ToolStrip strip)
            {
                foreach (ToolStripItem item in strip.Items)
                {
                    if (this.itemAppearances.TryGetValue(item, out ItemAppearance original))
                    {
                        this.itemAppearances.Remove(item);
                        if (!shared)
                        {
                            SharedItemAppearances.Remove(item);
                            if (!item.IsDisposed)
                            {
                                item.ForeColor = original.ForeColor;
                            }
                        }
                    }
                    if (item is ToolStripDropDownItem dropDown)
                    {
                        dropDown.DropDownOpening -= this.DropDownOpening;
                        if (dropDown.HasDropDownItems)
                        {
                            this.Detach(dropDown.DropDown);
                        }
                    }
                }
            }
            foreach (Control child in control.Controls)
            {
                this.Detach(child);
            }
            if (control.ContextMenuStrip != null && (this.disposed || !this.IsContextMenuReferenced(control.ContextMenuStrip)))
            {
                this.Detach(control.ContextMenuStrip);
            }
            if (!shared && !control.Disposing && !control.IsDisposed)
            {
                appearance.Restore(control);
            }
        }

        private bool IsContextMenuReferenced(ContextMenuStrip menu)
        {
            return this.trackedControls.Any(reference => reference.TryGetTarget(out Control owner)
                && !owner.IsDisposed && ReferenceEquals(owner.ContextMenuStrip, menu));
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        private sealed class Appearance
        {
            public List<WeakReference<FormTheme>> Owners { get; } = new List<WeakReference<FormTheme>>();

            public Appearance(Control control)
            {
                var properties = TypeDescriptor.GetProperties(control);
                this.BackColor = properties[nameof(Control.BackColor)].ShouldSerializeValue(control) ? control.BackColor : Color.Empty;
                this.ForeColor = properties[nameof(Control.ForeColor)].ShouldSerializeValue(control) ? control.ForeColor : Color.Empty;
                if (control is ButtonBase buttonBase)
                {
                    this.FlatStyle = buttonBase.FlatStyle;
                    this.UseVisualStyleBackColor = buttonBase.UseVisualStyleBackColor;
                }
                if (control is ComboBox combo)
                {
                    this.FlatStyle = combo.FlatStyle;
                    this.DrawMode = combo.DrawMode;
                }
                if (control is TabPage page)
                {
                    this.UseVisualStyleBackColor = page.UseVisualStyleBackColor;
                }
                if (control is DataGridView grid)
                {
                    this.GridHeaderVisualStyles = grid.EnableHeadersVisualStyles;
                    this.GridBackground = grid.BackgroundColor;
                    this.GridColor = grid.GridColor;
                    this.GridCells = grid.DefaultCellStyle.Clone();
                    this.GridColumns = grid.ColumnHeadersDefaultCellStyle.Clone();
                    this.GridRows = grid.RowHeadersDefaultCellStyle.Clone();
                    this.HasGridBackground = properties[nameof(DataGridView.BackgroundColor)].ShouldSerializeValue(grid);
                    this.HasGridColor = properties[nameof(DataGridView.GridColor)].ShouldSerializeValue(grid);
                    this.HasGridCells = properties[nameof(DataGridView.DefaultCellStyle)].ShouldSerializeValue(grid);
                    this.HasGridColumns = properties[nameof(DataGridView.ColumnHeadersDefaultCellStyle)].ShouldSerializeValue(grid);
                    this.HasGridRows = properties[nameof(DataGridView.RowHeadersDefaultCellStyle)].ShouldSerializeValue(grid);
                }
                if (control is Button button)
                {
                    this.ButtonBorder = button.FlatAppearance.BorderColor;
                    this.ButtonHover = button.FlatAppearance.MouseOverBackColor;
                    this.ButtonPressed = button.FlatAppearance.MouseDownBackColor;
                }

                if (control is LinkLabel link)
                {
                    this.LinkColor = link.LinkColor;
                    this.ActiveLinkColor = link.ActiveLinkColor;
                    this.VisitedLinkColor = link.VisitedLinkColor;
                    this.HasLinkColor = properties[nameof(LinkLabel.LinkColor)].ShouldSerializeValue(link);
                    this.HasActiveLinkColor = properties[nameof(LinkLabel.ActiveLinkColor)].ShouldSerializeValue(link);
                    this.HasVisitedLinkColor = properties[nameof(LinkLabel.VisitedLinkColor)].ShouldSerializeValue(link);
                }

                if (control is ToolStrip strip)
                {
                    this.Renderer = strip.Renderer;
                    this.RenderMode = strip.RenderMode;
                    this.HasCustomRenderer = strip.RenderMode == ToolStripRenderMode.Custom
                        || (strip.RenderMode == ToolStripRenderMode.ManagerRenderMode && ToolStripManager.RenderMode == ToolStripManagerRenderMode.Custom);
                }
            }

            public void Restore(Control control)
            {
                control.BackColor = this.BackColor;
                control.ForeColor = this.ForeColor;
                if (control is ButtonBase buttonBase)
                {
                    buttonBase.FlatStyle = this.FlatStyle;
                    buttonBase.UseVisualStyleBackColor = this.UseVisualStyleBackColor;
                }
                if (control is Button button)
                {
                    button.FlatAppearance.BorderColor = this.ButtonBorder;
                    button.FlatAppearance.MouseOverBackColor = this.ButtonHover;
                    button.FlatAppearance.MouseDownBackColor = this.ButtonPressed;
                }
                if (control is ComboBox combo)
                {
                    combo.FlatStyle = this.FlatStyle;
                    combo.DrawMode = this.DrawMode;
                }
                if (control is TabPage page)
                {
                    page.UseVisualStyleBackColor = this.UseVisualStyleBackColor;
                }
                if (control is LinkLabel link)
                {
                    link.LinkColor = this.LinkColor;
                    link.ActiveLinkColor = this.ActiveLinkColor;
                    link.VisitedLinkColor = this.VisitedLinkColor;
                }
                if (control is ToolStrip strip)
                {
                    if (this.RenderMode == ToolStripRenderMode.Custom)
                    {
                        strip.Renderer = this.Renderer;
                    }
                    else
                    {
                        strip.RenderMode = this.RenderMode;
                    }
                }
                if (control is DataGridView grid)
                {
                    grid.EnableHeadersVisualStyles = this.GridHeaderVisualStyles;
                    grid.BackgroundColor = this.GridBackground;
                    grid.GridColor = this.GridColor;
                    grid.DefaultCellStyle = this.GridCells.Clone();
                    grid.ColumnHeadersDefaultCellStyle = this.GridColumns.Clone();
                    grid.RowHeadersDefaultCellStyle = this.GridRows.Clone();
                }
            }

            public Color BackColor { get; set; }
            public Color ForeColor { get; set; }
            public Color AppliedBackColor { get; set; }
            public Color AppliedForeColor { get; set; }
            public bool Applying { get; set; }
            public FlatStyle FlatStyle { get; }
            public bool UseVisualStyleBackColor { get; set; }
            public Color ButtonBorder { get; }
            public Color ButtonHover { get; }
            public Color ButtonPressed { get; }
            public Color LinkColor { get; }
            public Color ActiveLinkColor { get; }
            public Color VisitedLinkColor { get; }
            public bool HasLinkColor { get; }
            public bool HasActiveLinkColor { get; }
            public bool HasVisitedLinkColor { get; }
            public ToolStripRenderer Renderer { get; }
            public ToolStripRenderMode RenderMode { get; }
            public bool HasCustomRenderer { get; }
            public DrawMode DrawMode { get; }
            public bool GridHeaderVisualStyles { get; }
            public Color GridBackground { get; }
            public Color GridColor { get; }
            public DataGridViewCellStyle GridCells { get; }
            public DataGridViewCellStyle GridColumns { get; }
            public DataGridViewCellStyle GridRows { get; }
            public bool HasGridBackground { get; }
            public bool HasGridColor { get; }
            public bool HasGridCells { get; }
            public bool HasGridColumns { get; }
            public bool HasGridRows { get; }
            public ControlScrollBarTheme ScrollBarTheme { get; set; }
        }

        private sealed class ItemAppearance
        {
            public ItemAppearance(ToolStripItem item)
            {
                this.ForeColor = TypeDescriptor.GetProperties(item)[nameof(item.ForeColor)].ShouldSerializeValue(item)
                    ? item.ForeColor : Color.Empty;
            }
            public Color ForeColor { get; }
        }

        private sealed class ThemeRenderer : ToolStripProfessionalRenderer
        {
            private readonly IThemeColors colors;

            public ThemeRenderer(IThemeColors colors) : base(new ThemeColorTable(colors))
            {
                this.colors = colors;
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                if (e.ToolStrip is StatusStrip)
                {
                    using (var pen = new Pen(this.colors.ControlDarkColor))
                    {
                        e.Graphics.DrawLine(pen, 0, 0, e.ToolStrip.Width - 1, 0);
                    }
                    return;
                }
                base.OnRenderToolStripBorder(e);
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = !e.Item.Enabled ? this.colors.GrayTextColor
                    : e.Item.Selected && this.colors.HighContrast ? this.colors.HighlightTextColor : e.Item.ForeColor;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                e.ArrowColor = !e.Item.Enabled ? this.colors.GrayTextColor
                    : e.Item.Selected && this.colors.HighContrast ? this.colors.HighlightTextColor : e.Item.ForeColor;
                base.OnRenderArrow(e);
            }
        }

        private sealed class ThemeColorTable : ProfessionalColorTable
        {
            private readonly IThemeColors colors;

            public ThemeColorTable(IThemeColors colors)
            {
                this.colors = colors;
                this.UseSystemColors = false;
            }

            public override Color ToolStripDropDownBackground => this.colors.MenuColor;
            public override Color ImageMarginGradientBegin => this.colors.InputColor;
            public override Color ImageMarginGradientMiddle => this.colors.InputColor;
            public override Color ImageMarginGradientEnd => this.colors.InputColor;
            public override Color MenuStripGradientBegin => this.colors.MenuColor;
            public override Color MenuStripGradientEnd => this.colors.MenuColor;
            public override Color StatusStripGradientBegin => this.colors.ControlColor;
            public override Color StatusStripGradientEnd => this.colors.ControlColor;
            public override Color MenuItemSelected => this.colors.MenuHighlightColor;
            public override Color MenuItemBorder => this.colors.ControlDarkColor;
            public override Color MenuBorder => this.colors.ControlDarkColor;
            public override Color MenuItemSelectedGradientBegin => this.colors.MenuHighlightColor;
            public override Color MenuItemSelectedGradientEnd => this.colors.MenuHighlightColor;
            public override Color MenuItemPressedGradientBegin => this.colors.MenuHighlightColor;
            public override Color MenuItemPressedGradientMiddle => this.colors.MenuHighlightColor;
            public override Color MenuItemPressedGradientEnd => this.colors.MenuHighlightColor;
            public override Color SeparatorDark => this.colors.ControlDarkColor;
            public override Color SeparatorLight => this.colors.ControlColor;
            public override Color CheckBackground => this.colors.HighlightColor;
            public override Color CheckSelectedBackground => this.colors.HighlightColor;
            public override Color CheckPressedBackground => this.colors.HighlightColor;
        }
    }
}
