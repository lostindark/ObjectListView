using System.Windows.Forms;

using BrightIdeasSoftware;

namespace ObjectListViewDemo {
    internal static class DemoStyles {
        internal static void ApplyInformationBanner(Label label, IThemeColors colors = null) {
            colors ??= ObjectListViewTheme.Current;
            label.BackColor = colors.InformationBackColor;
            label.ForeColor = colors.InformationTextColor;
        }
    }
}
