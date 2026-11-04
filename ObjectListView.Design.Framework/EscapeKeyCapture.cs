using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace BrightIdeasSoftware.Design
{
    internal sealed class EscapeKeyCapture : NativeWindow, IDisposable
    {
        private const int CancelMessage = 0x8000;
        private static readonly int MessageOffset = Marshal.OffsetOf<NativeMessageHeader>(nameof(NativeMessageHeader.Message)).ToInt32();
        private readonly Action cancel;
        private readonly HookProcedure procedure;
        private IntPtr hook;

        public EscapeKeyCapture(Control control, Action cancel) {
            uint thread = GetWindowThreadProcessId(control.Handle, out _);
            if (thread == 0 || thread != GetCurrentThreadId())
            {
                throw new InvalidOperationException("Resize capture must be created on the control's UI thread.");
            }
            this.cancel = cancel;
            this.procedure = this.FilterMessage;
            this.CreateHandle(new CreateParams { Caption = "ObjectListView resize cancellation", Parent = (IntPtr)(-3) });
            this.hook = SetWindowsHookEx(3, this.procedure, IntPtr.Zero, thread);
            if (this.hook == IntPtr.Zero) {
                int error = Marshal.GetLastWin32Error();
                this.DestroyHandle();
                throw new Win32Exception(error);
            }
        }

        public bool CancellationRequested { get; private set; }

        private IntPtr FilterMessage(int code, IntPtr remove, IntPtr pointer) {
            if (code >= 0 && remove == (IntPtr)1 && this.hook != IntPtr.Zero) {
                NativeMessageHeader message = Marshal.PtrToStructure<NativeMessageHeader>(pointer);
                if (message.Message == 0x100 && message.WParam == (IntPtr)Keys.Escape) {
                    Marshal.WriteInt32(pointer, MessageOffset, 0);
                    if (!this.CancellationRequested) {
                        this.CancellationRequested = true;
                        if (!PostMessage(this.Handle, CancelMessage, IntPtr.Zero, IntPtr.Zero))
                        {
                            Trace.TraceError("Could not post ObjectListView resize cancellation: {0}", Marshal.GetLastWin32Error());
                        }
                    }
                }
            }
            return CallNextHookEx(IntPtr.Zero, code, remove, pointer);
        }

        protected override void WndProc(ref Message message) {
            if (message.Msg == CancelMessage && this.CancellationRequested && this.hook != IntPtr.Zero) {
                this.cancel();
                return;
            }
            base.WndProc(ref message);
        }

        public void Dispose() {
            if (this.hook != IntPtr.Zero) {
                if (!UnhookWindowsHookEx(this.hook))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                this.hook = IntPtr.Zero;
            }
            this.DestroyHandle();
            GC.KeepAlive(this.procedure);
            GC.SuppressFinalize(this);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMessageHeader
        {
            public IntPtr Window;
            public int Message;
            public IntPtr WParam;
        }

        private delegate IntPtr HookProcedure(int code, IntPtr remove, IntPtr message);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int type, HookProcedure procedure, IntPtr module, uint thread);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr remove, IntPtr message);
        [DllImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
        private static extern bool PostMessage(IntPtr window, int message, IntPtr wparam, IntPtr lparam);
    }
}
