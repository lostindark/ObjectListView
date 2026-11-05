using System;
using System.Windows.Forms;

namespace BrightIdeasSoftware
{
    /// <summary>Paints a form's first complete frame before revealing it, independently of any theme service.</summary>
    public sealed class FormFirstFrame : NativeWindow, IDisposable
    {
        private readonly Form form;
        private readonly Func<bool> shouldPrepare;
        private double? opacity;
        private bool presenting;
        private bool presented;
        private bool disposed;

        private FormFirstFrame(Form form, Func<bool> shouldPrepare)
        {
            this.form = form;
            this.shouldPrepare = shouldPrepare;
            form.HandleCreated += this.FormHandleCreated;
            form.HandleDestroyed += this.FormHandleDestroyed;
            form.Shown += this.FormShown;
            form.Disposed += this.FormDisposed;
            if (form.IsHandleCreated)
            {
                this.AssignHandle(form.Handle);
            }
        }

        /// <summary>
        /// Attaches before the form's first Show. The optional predicate is sampled before native presentation;
        /// omit it to prepare unconditionally. Detaches after first display or form disposal.
        /// Dispose the returned instance to cancel preparation without disposing the form.
        /// </summary>
        public static FormFirstFrame Attach(Form form, Func<bool> shouldPrepare = null)
        {
            ArgumentNullException.ThrowIfNull(form, nameof(form));
#if NETFRAMEWORK
            if (form.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(form));
            }
#else
            ObjectDisposedException.ThrowIf(form.IsDisposed, form);
#endif
            if (form.Visible)
            {
                throw new InvalidOperationException("First-frame preparation must be attached before showing the form.");
            }
            return new FormFirstFrame(form, shouldPrepare);
        }

        private void FormHandleCreated(object sender, EventArgs e) => this.AssignHandle(this.form.Handle);

        private void FormHandleDestroyed(object sender, EventArgs e) => this.ReleaseHandle();

        /// <inheritdoc />
        protected override void WndProc(ref Message m)
        {
            if (m.Msg != 0x0018 || m.WParam == IntPtr.Zero || this.disposed || this.presented || this.presenting)
            {
                base.WndProc(ref m);
                return;
            }
            this.presenting = true;
            try
            {
                if (this.form.Site?.DesignMode != true && this.form.Opacity > 0
                    && (this.shouldPrepare == null || this.shouldPrepare()))
                {
                    this.opacity = this.form.Opacity;
                    this.form.Opacity = 0;
                }
                base.WndProc(ref m);
                if (this.opacity.HasValue && !this.form.IsDisposed && !this.form.Disposing)
                {
                    this.PaintFrame();
                }
            }
            finally
            {
                this.RestoreOpacity();
                this.presenting = false;
                this.presented = true;
            }
        }

        private void FormShown(object sender, EventArgs e)
        {
            if (this.disposed)
            {
                return;
            }
            try
            {
                if (!this.form.IsDisposed && !this.form.Disposing)
                {
                    // Owned chrome is exposed only after its owner's opacity is restored.
                    this.PaintFrame();
                }
            }
            finally
            {
                this.Dispose();
            }
        }

        private void PaintFrame()
        {
            this.form.Invalidate(true);
            UpdateVisibleControls(this.form);
        }

        private static void UpdateVisibleControls(Control control)
        {
            if (!control.Visible || !control.IsHandleCreated)
            {
                return;
            }
            control.Update();
            foreach (Control child in control.Controls)
            {
                UpdateVisibleControls(child);
            }
        }

        private void FormDisposed(object sender, EventArgs e) => this.Dispose();

        /// <summary>Detaches event handlers and restores any temporarily suppressed opacity.</summary>
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }
            this.disposed = true;
            this.form.HandleCreated -= this.FormHandleCreated;
            this.form.HandleDestroyed -= this.FormHandleDestroyed;
            this.form.Shown -= this.FormShown;
            this.form.Disposed -= this.FormDisposed;
            this.RestoreOpacity();
            this.ReleaseHandle();
            GC.SuppressFinalize(this);
        }

        private void RestoreOpacity()
        {
            if (this.opacity.HasValue && !this.form.IsDisposed && !this.form.Disposing)
            {
                this.form.Opacity = this.opacity.Value;
            }
            this.opacity = null;
        }
    }
}
