using BepInEx;
using GLMod.Services.Interfaces;
using BepInEx.Logging;
using GLMod.Class;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Net.Http;

namespace GLMod.Services.Implementations
{
    /// <summary>
    /// Service responsible for file integrity verification
    /// </summary>
    public class IntegrityService : IIntegrityService
    {
        private readonly ManualLogSource _logger;
        private readonly string _apiEndpoint;

        public IntegrityService(ManualLogSource logger, string apiEndpoint)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiEndpoint = apiEndpoint ?? throw new ArgumentNullException(nameof(apiEndpoint));
        }

        private void Log(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                string playerName = PlayerControl.LocalPlayer?.Data?.PlayerName;
                string prefix = playerName != null ? "[GLMod] " + playerName + ": " : "[GLMod] ";
                _logger.LogInfo(prefix + message);
            }
        }

        public IEnumerator GetApiData(string id, System.Action<string> onComplete, System.Action<string> onError = null)
        {
            var form = new Dictionary<string, string>
            {
                { "id", id }
            };

            string responseString = null;
            string error = null;

            yield return ApiService.PostFormAsync(_apiEndpoint + "/data", form,
                result => {
                    responseString = result;
                },
                err => {
                    error = err;
                }
            );

            if (error != null)
            {
                onError?.Invoke(error);
                onComplete?.Invoke("");
                yield break;
            }

            onComplete?.Invoke(responseString);
        }

        public IEnumerator GetChecksum(string checksumId, System.Action<string> onComplete, System.Action<string> onError = null)
        {
            string result = null;
            string error = null;
            Log("getChecksum:" + checksumId);

            yield return GetApiData("checksum_" + checksumId,
                data => {
                    result = data;
                },
                err => {
                    error = err;
                }
            );

            if (error != null)
            {
                Log($"getChecksum failed for {checksumId}, error: {error}");
                onError?.Invoke(error);
            }
            else
            {
                onComplete?.Invoke(result);
            }
        }

        private string CalculateFileSHA512(string filePath)
        {
            using (var sha512 = SHA512.Create())
            using (var fileStream = File.OpenRead(filePath))
            {
                byte[] hashBytes = sha512.ComputeHash(fileStream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public IEnumerator VerifyDll(string checksumId, string dllPath, System.Action<bool> onComplete)
        {
            string localChecksum = CalculateFileSHA512(dllPath);
            Log("Local checksum: " + localChecksum);

            string remoteChecksum = null;
            bool hasError = false;

            yield return GetChecksum(checksumId,
                checksum => {
                    remoteChecksum = checksum;
                },
                error => {
                    hasError = true;
                }
            );

            if (hasError)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            Log("Remote checksum: " + remoteChecksum);

            bool result = localChecksum == remoteChecksum;
            Log(result ? "Valid checksum" : "Invalid checksum");

            onComplete?.Invoke(result);
        }

        public IEnumerator VerifyGLMod(System.Action<bool> onComplete)
        {
            var pluginAttribute = typeof(GLMod).GetCustomAttribute<BepInPlugin>();
            // Format version as Major.Minor.Build (3 components) to match server expectations
            string version = pluginAttribute?.Version != null
                ? $"{pluginAttribute.Version.Major}.{pluginAttribute.Version.Minor}.{pluginAttribute.Version.Build}"
                : null;
            Log(version);

            bool result = false;

            yield return VerifyDll("glmod" + version, "BepInEx/plugins/glmod.dll",
                isValid => {
                    result = isValid;
                }
            );

            onComplete?.Invoke(result);
        }

        #region Alternative Methods Without Coroutines (Async/Await)

        /// <summary>
        /// Alternative version of GetApiData using async/await instead of coroutines
        /// </summary>
        public async Task<string> GetApiDataAsync(string id)
        {
            var form = new Dictionary<string, string>
            {
                { "id", id }
            };

            try
            {
                var content = new FormUrlEncodedContent(form);
                var response = await HttpHelper.Client.PostAsync(_apiEndpoint + "/data", content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return System.Text.RegularExpressions.Regex.Unescape(responseString);
            }
            catch (Exception ex)
            {
                Log($"GetApiDataAsync failed for {id}, error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Alternative version of GetChecksum using async/await instead of coroutines
        /// </summary>
        public async Task<string> GetChecksumAsync(string checksumId)
        {
            Log("getChecksumAsync:" + checksumId);

            try
            {
                string result = await GetApiDataAsync("checksum_" + checksumId).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                Log($"getChecksumAsync failed for {checksumId}, error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Alternative version of VerifyDll using async/await instead of coroutines
        /// </summary>
        public async Task<bool> VerifyDllAsync(string checksumId, string dllPath)
        {
            try
            {
                string localChecksum = CalculateFileSHA512(dllPath);
                Log("Local checksum: " + localChecksum);

                string remoteChecksum = await GetChecksumAsync(checksumId).ConfigureAwait(false);
                Log("Remote checksum: " + remoteChecksum);

                bool result = localChecksum == remoteChecksum;
                Log(result ? "Valid checksum" : "Invalid checksum");

                return result;
            }
            catch (Exception ex)
            {
                Log($"VerifyDllAsync failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Alternative version of VerifyGLMod using async/await instead of coroutines
        /// </summary>
        public async Task<bool> VerifyGLModAsync()
        {
            try
            {
                var pluginAttribute = typeof(GLMod).GetCustomAttribute<BepInPlugin>();
                // Format version as Major.Minor.Build (3 components) to match server expectations
                string version = pluginAttribute?.Version != null
                    ? $"{pluginAttribute.Version.Major}.{pluginAttribute.Version.Minor}.{pluginAttribute.Version.Build}"
                    : null;
                Log(version);

                bool result = await VerifyDllAsync("glmod" + version, "BepInEx/plugins/glmod.dll").ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                Log($"VerifyGLModAsync failed: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
