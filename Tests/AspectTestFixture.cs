using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    [TestClass]
    public abstract class AspectTestFixture
    {
        private bool ignoreMissingAspects;

        [TestInitialize]
        public void ConfigureAspectErrors()
        {
            ignoreMissingAspects = ObjectListView.IgnoreMissingAspects;
            // The library uses different defaults in Debug and Release.
            ObjectListView.IgnoreMissingAspects = false;
        }

        [TestCleanup]
        public void RestoreAspectErrors()
        {
            ObjectListView.IgnoreMissingAspects = ignoreMissingAspects;
        }
    }
}
