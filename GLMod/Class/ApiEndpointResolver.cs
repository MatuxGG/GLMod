using System;
using GLMod.Constants;

namespace GLMod.Class
{
    /// <summary>
    /// Resolves the Good Loss API base URL: production by default, or the value of the
    /// GLMOD_API_ENDPOINT environment variable (e.g. preprod for end-to-end tests).
    /// </summary>
    public static class ApiEndpointResolver
    {
        /// <summary>
        /// Returns the override when it is an absolute http(s) URL (without trailing slash),
        /// otherwise the production endpoint.
        /// </summary>
        public static string Resolve(string overrideValue)
        {
            if (string.IsNullOrWhiteSpace(overrideValue))
            {
                return GameConstants.API_ENDPOINT;
            }

            string trimmed = overrideValue.Trim().TrimEnd('/');
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                return GameConstants.API_ENDPOINT;
            }

            return trimmed;
        }

        /// <summary>
        /// Reads the override from the process environment.
        /// </summary>
        public static string ResolveFromEnvironment()
        {
            return Resolve(Environment.GetEnvironmentVariable(GameConstants.API_ENDPOINT_ENV_VAR));
        }
    }
}
