using System.Collections;
using System.Threading.Tasks;

namespace GLMod.Services.Interfaces
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
        /// <param name="onComplete">Callback with verification result (true if valid, false if invalid or error)</param>
        /// <returns>Coroutine</returns>
        IEnumerator VerifyDll(string checksumId, string dllPath, System.Action<bool> onComplete);

        /// <summary>
        /// Verifies GLMod integrity
        /// </summary>
        /// <param name="onComplete">Callback with verification result (true if valid, false if invalid or error)</param>
        /// <returns>Coroutine</returns>
        IEnumerator VerifyGLMod(System.Action<bool> onComplete);

        #region Alternative Methods Without Coroutines (Async/Await)

        /// <summary>
        /// Alternative version of GetApiData using async/await instead of coroutines
        /// </summary>
        /// <param name="id">Data ID</param>
        /// <returns>API data as string</returns>
        Task<string> GetApiDataAsync(string id);

        /// <summary>
        /// Alternative version of GetChecksum using async/await instead of coroutines
        /// </summary>
        /// <param name="checksumId">Checksum ID</param>
        /// <returns>Checksum as string</returns>
        Task<string> GetChecksumAsync(string checksumId);

        /// <summary>
        /// Alternative version of VerifyDll using async/await instead of coroutines
        /// </summary>
        /// <param name="checksumId">Checksum ID</param>
        /// <param name="dllPath">Path to DLL file</param>
        /// <returns>True if valid, false otherwise</returns>
        Task<bool> VerifyDllAsync(string checksumId, string dllPath);

        /// <summary>
        /// Alternative version of VerifyGLMod using async/await instead of coroutines
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        Task<bool> VerifyGLModAsync();

        #endregion
    }
}
