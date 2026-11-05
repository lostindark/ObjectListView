using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    [STATestClass]
    public class TestPrinterDemoTheme : ThemeTestFixture
    {
#if NET10_0_OR_GREATER
        private SystemColorMode originalWinFormsMode;

        [TestInitialize]
        public void ConfigureNativeMode()
        {
            this.originalWinFormsMode = Application.ColorMode;
            Application.SetColorMode(SystemColorMode.Dark);
        }

        [TestCleanup]
        public void RestoreNativeMode()
        {
            Application.SetColorMode(this.originalWinFormsMode);
        }
#endif

        [TestMethod]
        public void Test_PrinterDemo_UsesSharedDarkColorsForControlsAndCustomFormatSettings()
        {
            using (var form = new ListViewPrinterDemo.Form1())
            {
                Control[] controls = Descendants(form).ToArray();
                IThemeColors colors = ObjectListViewTheme.Current;
                var list = controls.OfType<ObjectListViewBase>().Single();
                var grid = controls.OfType<PropertyGrid>().Single();
                var banner = controls.OfType<Label>().Single(label => label.Name == "label1");
                var input = controls.OfType<TextBox>().First();
                Assert.AreEqual(colors.ControlColor.ToArgb(), form.BackColor.ToArgb());
                Assert.AreEqual(colors.WindowColor.ToArgb(), list.BackColor.ToArgb());
                Assert.AreEqual(colors.TextColor.ToArgb(), list.ForeColor.ToArgb());
                Assert.AreEqual(colors.InputColor.ToArgb(), input.BackColor.ToArgb());
                Assert.AreEqual(colors.WindowColor.ToArgb(), grid.ViewBackColor.ToArgb());
                Assert.AreEqual(colors.TextColor.ToArgb(), grid.ViewForeColor.ToArgb());
                Assert.AreEqual(colors.ControlColor.ToArgb(), grid.HelpBackColor.ToArgb());
                Assert.AreEqual(colors.InformationBackColor.ToArgb(), banner.BackColor.ToArgb());
                Assert.AreEqual(colors.InformationTextColor.ToArgb(), banner.ForeColor.ToArgb());
#if NETFRAMEWORK
                Assert.IsInstanceOfType<ThemedTabControl>(controls.OfType<TabControl>().Single());
#else
                Assert.AreEqual(typeof(TabControl), controls.OfType<TabControl>().Single().GetType());
#endif
            }
        }

        [TestMethod]
        public void Test_PrinterDemo_ThemesDataWithoutChangingDocumentFormatting()
        {
            string directory = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(AppContext.BaseDirectory);
                Assert.IsTrue(File.Exists("Persons.xml"));
                using (var form = new ListViewPrinterDemo.Form1())
                {
                    typeof(ListViewPrinterDemo.Form1).GetMethod("LoadXmlDataIntoList",
                        BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, null);
                    var list = Descendants(form).OfType<ObjectListViewBase>().Single();
                    Assert.IsTrue(list.GetItemCount() > 0);
                    Assert.AreEqual(ObjectListViewTheme.Current.WindowColor.ToArgb(), list.GetItem(0).BackColor.ToArgb());
                    Assert.AreEqual(ObjectListViewTheme.Current.TextColor.ToArgb(), list.GetItem(0).ForeColor.ToArgb());

                    Descendants(form).OfType<RadioButton>().Single(button => button.Name == "rbStyleMinimal").Checked = true;
                    typeof(ListViewPrinterDemo.Form1).GetMethod("UpdatePrintPreview",
                        BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { null, null });
                    var printer = (ListViewPrinter)typeof(ListViewPrinterDemo.Form1).GetField("listViewPrinter1",
                        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                    BlockFormat header = printer.HeaderFormat;
                    Brush ink = header.TextBrush;
                    Brush paper = printer.CellFormat.BackgroundBrush;
                    Assert.AreEqual(Color.White.ToArgb(), ((SolidBrush)paper).Color.ToArgb());
                    FormTheme.Apply(form).Apply(new ThemePalette(true, true));
                    Assert.AreSame(header, printer.HeaderFormat);
                    Assert.AreSame(ink, printer.HeaderFormat.TextBrush);
                    Assert.AreSame(paper, printer.CellFormat.BackgroundBrush);
                    Assert.AreEqual(Color.Black.ToArgb(), ((SolidBrush)ink).Color.ToArgb());
                }
            }
            finally
            {
                Directory.SetCurrentDirectory(directory);
            }
        }

        private static IEnumerable<Control> Descendants(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (Control descendant in Descendants(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
