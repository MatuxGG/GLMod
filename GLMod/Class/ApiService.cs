using System;
using System.Collections.Generic;
using System.Collections;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GLMod.Class
{
    public static class ApiService
    {
        public static IEnumerator PostFormAsync(string url, Dictionary<string, string> formValues, System.Action<string> onComplete, System.Action<string> onError = null)
        {
            return CoroutineHelpers.RunAsync(
                async () =>
                {
                    var response = await PostAndSyncClockAsync(url, formValues).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return Regex.Unescape(responseString);
                },
                onCompleted: onComplete,
                onError: ex => onError?.Invoke(ex.Message)
            );
        }

        public static IEnumerator PostFormWithErrorHandlingAsync(string url, Dictionary<string, string> formValues, System.Action<ApiResponse> onComplete)
        {
            return CoroutineHelpers.RunAsync<ApiResponse>(
                async () =>
                {
                    var response = await PostAndSyncClockAsync(url, formValues).ConfigureAwait(false);
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    return new ApiResponse
                    {
                        IsSuccess = response.IsSuccessStatusCode,
                        StatusCode = (int)response.StatusCode,
                        Content = Regex.Unescape(responseString)
                    };
                },
                onCompleted: onComplete,
                // Map network exceptions to a failure-shaped ApiResponse so callers
                // can keep a single uniform code path.
                onError: ex => onComplete?.Invoke(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    Content = ex.Message
                })
            );
        }

        /// <summary>
        /// Posts the form and refreshes ServerClock from the response Date header.
        /// </summary>
        private static async Task<HttpResponseMessage> PostAndSyncClockAsync(string url, Dictionary<string, string> formValues)
        {
            var content = new FormUrlEncodedContent(formValues);
            DateTimeOffset sentAt = DateTimeOffset.UtcNow;
            var response = await HttpHelper.Client.PostAsync(url, content).ConfigureAwait(false);
            ServerClock.Update(response.Headers.Date, sentAt, DateTimeOffset.UtcNow);
            return response;
        }
    }

    public class ApiResponse
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string Content { get; set; }
    }
}
