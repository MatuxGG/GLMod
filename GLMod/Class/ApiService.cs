using System;
using System.Collections.Generic;
using System.Collections;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GLMod.Class
{
    public static class ApiService
    {
        /// <summary>
        /// POST form; bearerToken (when not empty) is sent as "Authorization: Bearer".
        /// </summary>
        public static IEnumerator PostFormAsync(string url, Dictionary<string, string> formValues, System.Action<string> onComplete, System.Action<string> onError = null, string bearerToken = null)
        {
            return CoroutineHelpers.RunAsync(
                async () =>
                {
                    var response = await PostAndSyncClockAsync(url, formValues, bearerToken).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    // Body returned as sent: Regex.Unescape would break JSON escapes (\" and \\)
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                },
                onCompleted: onComplete,
                onError: ex => onError?.Invoke(ex.Message)
            );
        }

        /// <summary>
        /// POST form; network errors and non-2xx statuses come back as a failed ApiResponse.
        /// bearerToken (when not empty) is sent as "Authorization: Bearer". The body is kept as
        /// sent; unescape = true applies Regex.Unescape (never on JSON bodies: it turns \" into a raw
        /// quote and throws on unknown escapes).
        /// </summary>
        public static IEnumerator PostFormWithErrorHandlingAsync(string url, Dictionary<string, string> formValues, System.Action<ApiResponse> onComplete, string bearerToken = null, bool unescape = false)
        {
            return CoroutineHelpers.RunAsync<ApiResponse>(
                async () =>
                {
                    var response = await PostAndSyncClockAsync(url, formValues, bearerToken).ConfigureAwait(false);
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    return new ApiResponse
                    {
                        IsSuccess = response.IsSuccessStatusCode,
                        StatusCode = (int)response.StatusCode,
                        Content = unescape ? Regex.Unescape(responseString) : responseString
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
        /// GET request; network errors and non-2xx statuses come back as a failed ApiResponse
        /// (Content holds the body, e.g. {"error":"..."}, or the exception message).
        /// </summary>
        public static IEnumerator GetWithErrorHandlingAsync(string url, System.Action<ApiResponse> onComplete)
        {
            return CoroutineHelpers.RunAsync<ApiResponse>(
                async () =>
                {
                    DateTimeOffset sentAt = DateTimeOffset.UtcNow;
                    var response = await HttpHelper.Client.GetAsync(url).ConfigureAwait(false);
                    ServerClock.Update(response.Headers.Date, sentAt, DateTimeOffset.UtcNow);
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    return new ApiResponse
                    {
                        IsSuccess = response.IsSuccessStatusCode,
                        StatusCode = (int)response.StatusCode,
                        Content = responseString
                    };
                },
                onCompleted: onComplete,
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
        private static async Task<HttpResponseMessage> PostAndSyncClockAsync(string url, Dictionary<string, string> formValues, string bearerToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(formValues)
            };
            if (!string.IsNullOrEmpty(bearerToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }
            DateTimeOffset sentAt = DateTimeOffset.UtcNow;
            var response = await HttpHelper.Client.SendAsync(request).ConfigureAwait(false);
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
