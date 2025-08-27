using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking.Match;

namespace GLMod
{
    public static class ApiService
    {
        public static async Task<string> PostFormAsync(string url, Dictionary<string, string> formValues)
        {
            var content = new FormUrlEncodedContent(formValues);

            var response = await HttpHelper.Client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            return System.Text.RegularExpressions.Regex.Unescape(responseString);
        }

        public static async Task<ApiResponse> PostFormWithErrorHandlingAsync(string url, Dictionary<string, string> formValues)
        {
            var content = new FormUrlEncodedContent(formValues);
            var response = await HttpHelper.Client.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            return new ApiResponse
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                Content = System.Text.RegularExpressions.Regex.Unescape(responseString)
            };
        }
    }

    public class ApiResponse
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string Content { get; set; }
    }
}
