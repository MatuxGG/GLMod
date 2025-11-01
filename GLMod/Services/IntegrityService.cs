using BepInEx;
using BepInEx.Logging;
using GLMod.Class;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace GLMod.Services
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
                Log("Erreur HTTP : " + error);
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
    }
}
