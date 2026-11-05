using System;
using System.Windows.Forms;

namespace BrightIdeasSoftware {
    internal sealed class ControlScrollBarTheme : NativeWindow, IDisposable {
        private readonly Control control;
        private GlassPanelForm glass;
        private bool updating;
        private bool disposed;

        internal ControlScrollBarTheme(Control control) {
            this.control = control;
            control.HandleCreated += this.HandleCreated;
            control.HandleDestroyed += this.HandleDestroyed;
            control.VisibleChanged += this.VisibleChanged;
            control.Disposed += this.ControlDisposed;
            if (control is ScrollBar scrollBar)
            {
                scrollBar.ValueChanged += this.VisibleChanged;
            }
            if (control.IsHandleCreated)
            {
                this.AssignHandle(control.Handle);
            }
        }

        internal void Apply() {
            if (this.disposed || this.updating)
            {
                return;
            }
            this.updating = true;
            try {
                Form owner = this.control.FindForm();
                if (!NativeListViewTheme.ShouldPaint || !this.control.IsHandleCreated || !this.control.Visible
                    || owner == null || !owner.Visible || owner.WindowState == FormWindowState.Minimized)
                {
                    this.glass?.HideGlass();
                    return;
                }
                if (this.glass == null)
                {
                    this.glass = new GlassPanelForm(true);
                    this.glass.Bind(this.control,
                        (graphics, bounds) => NativeListViewTheme.DrawControlChrome(this.control, graphics), 255);
                }
                this.glass.ShowGlass();
                this.glass.Invalidate();
                this.glass.Update();
            } finally {
                this.updating = false;
            }
        }

        private void HandleCreated(object sender, EventArgs e) {
            this.AssignHandle(this.control.Handle);
            this.Apply();
        }

        private void HandleDestroyed(object sender, EventArgs e) {
            this.glass?.HideGlass();
            this.ReleaseHandle();
        }

        private void VisibleChanged(object sender, EventArgs e) => this.Apply();
        private void ControlDisposed(object sender, EventArgs e) => this.Dispose();

        protected override void WndProc(ref Message m) {
            base.WndProc(ref m);
            bool scrollBarMessage = this.control is ScrollBar
                && (m.Msg == 0x200 || m.Msg == 0x201 || m.Msg == 0x2A3);
            if (!this.disposed && !this.updating && this.control.IsHandleCreated
                && (NativeListViewTheme.IsPaintMessage(m.Msg) || scrollBarMessage))
            {
                if (m.Msg == 0x317 && (m.LParam.ToInt64() & (this.control is ScrollBar ? 4 : 2)) != 0
                    && NativeListViewTheme.ShouldPaint)
                {
                    _ = NativeListViewTheme.Paint(this.control, m.WParam, true, false);
                }
                else
                {
                    this.Apply();
                }
            }
        }

        public void Dispose() {
            if (this.disposed)
            {
                return;
            }
            this.disposed = true;
            this.control.HandleCreated -= this.HandleCreated;
            this.control.HandleDestroyed -= this.HandleDestroyed;
            this.control.VisibleChanged -= this.VisibleChanged;
            this.control.Disposed -= this.ControlDisposed;
            if (this.control is ScrollBar scrollBar)
            {
                scrollBar.ValueChanged -= this.VisibleChanged;
            }
            this.glass?.Dispose();
            this.ReleaseHandle();
            GC.SuppressFinalize(this);
        }
    }
}
