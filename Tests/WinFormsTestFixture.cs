using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    [TestClass]
    public abstract class WinFormsTestFixture
    {
        protected MainForm mainForm;

        [TestInitialize]
        public void CreateTestForm()
        {
            PersonDb.Reset();
            mainForm = new MainForm();
            mainForm.Size = Size.Empty;
            mainForm.Opacity = 0;
            mainForm.ShowInTaskbar = false;
            mainForm.Show();
        }

        [TestCleanup]
        public void DisposeTestForm()
        {
            if (mainForm != null)
                mainForm.Dispose();
        }
    }
}
