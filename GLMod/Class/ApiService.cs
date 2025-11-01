using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking.Match;

namespace GLMod.Class
{
    public static class ApiService
    {
        public static IEnumerator PostFormAsync(string url, Dictionary<string, string> formValues, System.Action<string> onComplete, System.Action<string> onError = null)
        {
            // Variables partagées entre threads
            bool done = false;
            string error = null;
            string result = null;

            // Stocker la référence à la tâche pour une meilleure gestion
            var task = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var content = new FormUrlEncodedContent(formValues);
                    var response = await HttpHelper.Client.PostAsync(url, content).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    result = System.Text.RegularExpressions.Regex.Unescape(responseString);
                }
                catch (System.Exception ex)
                {
                    error = ex.Message;
                }
                finally
                {
                    // S'assurer que done est défini en dernier pour garantir la visibilité des autres variables
                    System.Threading.Volatile.Write(ref done, true);
                }
            });

            // Attendre la fin de la tâche avec une lecture volatile
            while (!System.Threading.Volatile.Read(ref done))
                yield return null;

            // Gestion du résultat
            if (error != null)
            {
                onError?.Invoke(error);
            }
            else
            {
                onComplete?.Invoke(result);
            }
        }

        public static IEnumerator PostFormWithErrorHandlingAsync(string url, Dictionary<string, string> formValues, System.Action<ApiResponse> onComplete)
        {
            // Variables partagées entre threads
            ApiResponse apiResponse = null;
            bool done = false;

            // Stocker la référence à la tâche pour une meilleure gestion
            var task = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var content = new FormUrlEncodedContent(formValues);
                    var response = await HttpHelper.Client.PostAsync(url, content).ConfigureAwait(false);
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    apiResponse = new ApiResponse
                    {
                        IsSuccess = response.IsSuccessStatusCode,
                        StatusCode = (int)response.StatusCode,
                        Content = System.Text.RegularExpressions.Regex.Unescape(responseString)
                    };
                }
                catch (System.Exception ex)
                {
                    // En cas d'exception réseau, créer une réponse d'erreur
                    apiResponse = new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = 0,
                        Content = ex.Message
                    };
                }
                finally
                {
                    // S'assurer que done est défini en dernier pour garantir la visibilité des autres variables
                    System.Threading.Volatile.Write(ref done, true);
                }
            });

            // Attendre la fin de la tâche avec une lecture volatile
            while (!System.Threading.Volatile.Read(ref done))
                yield return null;

            // Retourner le résultat via le callback
            onComplete?.Invoke(apiResponse);
        }
    }

    public class ApiResponse
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string Content { get; set; }
    }
}