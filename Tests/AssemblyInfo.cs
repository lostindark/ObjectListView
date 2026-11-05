using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace BrightIdeasSoftware.Tests
{
    [TestClass]
    public sealed class TestAssembly
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            Application.EnableVisualStyles();
        }
    }
}
