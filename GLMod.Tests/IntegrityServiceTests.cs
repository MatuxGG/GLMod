using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GLMod.Services.Implementations;
using Xunit;

namespace GLMod.Tests
{
    public class IntegrityServiceTests
    {
        // k0020: VerifyDll hashed the dll outside any try/catch, so a missing
        // BepInEx/plugins/glmod.dll (GLMod bundled in another mod or in a subfolder)
        // threw from MoveNext and onComplete was never called. Hashing must not throw.
        [Fact]
        public void TryCalculateFileSHA512_MissingFile_ReturnsNullWithoutThrowing_k0020()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "glmod.dll");

            string error = "unset";
            string checksum = null;
            var ex = Record.Exception(() => checksum = IntegrityService.TryCalculateFileSHA512(path, out error));

            Assert.Null(ex);
            Assert.Null(checksum);
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public void TryCalculateFileSHA512_ExistingFile_ReturnsLowercaseHex()
        {
            string path = Path.GetTempFileName();
            try
            {
                byte[] content = Encoding.UTF8.GetBytes("glmod");
                File.WriteAllBytes(path, content);
                string expected;
                using (var sha512 = SHA512.Create())
                    expected = BitConverter.ToString(sha512.ComputeHash(content)).Replace("-", "").ToLowerInvariant();

                string checksum = IntegrityService.TryCalculateFileSHA512(path, out string error);

                Assert.Equal(expected, checksum);
                Assert.Null(error);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
