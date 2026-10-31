using System;
using Xunit;

namespace GLMod.Tests
{
    public class MainMenuManagerPatchTests
    {
        [Fact]
        public void FormatInitError_WithoutInnerException_KeepsMessage()
        {
            // Bughunt k0013: the catch called e.InnerException.ToString(), so an exception without
            // InnerException threw a NullReferenceException and "boom" was never logged.
            Exception thrown;
            try { throw new Exception("boom"); }
            catch (Exception e) { thrown = e; }

            string line = MainMenuManagerStartPatch.FormatInitError(thrown);

            Assert.Contains("boom", line);
            Assert.Contains(thrown.Source, line);
        }

        [Fact]
        public void FormatInitError_WithoutSource_KeepsMessage()
        {
            // An exception that was never thrown has a null Source.
            string line = MainMenuManagerStartPatch.FormatInitError(new Exception("boom"));

            Assert.Contains("boom", line);
        }

        [Fact]
        public void FormatInitError_WithInnerException_IncludesIt()
        {
            var e = new Exception("outer", new InvalidOperationException("inner"));

            string line = MainMenuManagerStartPatch.FormatInitError(e);

            Assert.Contains("outer", line);
            Assert.Contains("inner", line);
        }
    }
}
