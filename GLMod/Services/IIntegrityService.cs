using System.Collections;

namespace GLMod.Services
{
    /// <summary>
    /// Interface for managing file integrity verification
    /// </summary>
    public interface IIntegrityService
    {
        /// <summary>
        /// Gets API data by ID
        /// </summary>
        /// <param name="id">Data ID</param>
        /// <param name="onComplete">Callback with data result</param>
        /// <param name="onError">Callback on error</param>
        /// <returns>Coroutine</returns>
        IEnumerator GetApiData(string id, System.Action<string> onComplete, System.Action<string> onError = null);

        /// <summary>
        /// Gets checksum from API
        /// </summary>
        /// <param name="checksumId">Checksum ID</param>
        /// <param name="onComplete">Callback with checksum result</param>
        /// <param name="onError">Callback on error</param>
        /// <returns>Coroutine</returns>
        IEnumerator GetChecksum(string checksumId, System.Action<string> onComplete, System.Action<string> onError = null);

        /// <summary>
        /// Verifies a DLL file against remote checksum
        /// </summary>
        /// <param name="checksumId">Checksum ID</param>
        /// <param name="dllPath">Path to DLL file</param>
        /// <param name="onComplete">Callback with verification result</param>
        /// <param name="onError">Callback on error</param>
        /// <returns>Coroutine</returns>
        IEnumerator VerifyDll(string checksumId, string dllPath, System.Action<bool> onComplete, System.Action<string> onError = null);

        /// <summary>
        /// Verifies GLMod integrity
        /// </summary>
        /// <param name="onComplete">Callback with verification result</param>
        /// <param name="onError">Callback on error</param>
        /// <returns>Coroutine</returns>
        IEnumerator VerifyGLMod(System.Action<bool> onComplete, System.Action<string> onError = null);
    }
}
