using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BrightIdeasSoftware.Tests {
    internal static class DemoWindowDiagnostics {
        internal static string DescribeOwnedWindows() {
            int processId = Process.GetCurrentProcess().Id;
            var output = new StringBuilder("Owned test-host windows:");
            EnumWindows(delegate(IntPtr window, IntPtr data) {
                uint owner;
                GetWindowThreadProcessId(window, out owner);
                if (owner != processId)
                    return true;
                var text = new StringBuilder(1024);
                GetWindowText(window, text, text.Capacity);
                output.AppendLine().Append(text);
                var name = new StringBuilder(256);
                GetClassName(window, name, name.Capacity);
                output.Append(" class=").Append(name).Append(" visible=").Append(IsWindowVisible(window));
                if (name.ToString() == "#32770") {
                    EnumChildWindows(window, delegate(IntPtr child, IntPtr state) {
                        var childText = new StringBuilder(1024);
                        GetWindowText(child, childText, childText.Capacity);
                        output.AppendLine().Append(childText);
                        return true;
                    }, IntPtr.Zero);
                }
                return true;
            }, IntPtr.Zero);
            return output.ToString();
        }

        private delegate bool EnumWindowProc(IntPtr window, IntPtr data);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowProc callback, IntPtr data);
        [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumWindowProc callback, IntPtr data);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder text, int count);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    }
}
