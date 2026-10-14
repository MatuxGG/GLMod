using System.Text.Json;

namespace GLMod.Class
{
    /// <summary>
    /// Pure parsing of the /player/* GET responses (JSON). No IO, no logging.
    /// </summary>
    public static class PlayerApiResponse
    {
        /// <summary>
        /// Deserializes a JSON body; false when it is empty or not the expected shape.
        /// </summary>
        public static bool TryParse<T>(string json, out T value) where T : class => TryParse(json, out value, out string _);

        /// <summary>
        /// Deserializes a JSON body; on failure, error tells why (e.g. the JSON path of a mismatching value)
        /// </summary>
        public static bool TryParse<T>(string json, out T value, out string error) where T : class
        {
            value = null;
            error = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "empty body";
                return false;
            }

            try
            {
                value = JsonSerializer.Deserialize<T>(json);
                if (value == null)
                {
                    error = "null body";
                }
                return value != null;
            }
            catch (JsonException e)
            {
                error = e.Message;
                return false;
            }
        }

        /// <summary>
        /// Start of a response body for the logs
        /// </summary>
        public static string Snippet(string body, int maxLength = 300)
        {
            if (body == null)
            {
                return "<null>";
            }
            return body.Length <= maxLength ? body : body.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// Message of an API error body ({"error":"Player not found"}), or null.
        /// </summary>
        public static string ReadError(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("error", out JsonElement error)
                    && error.ValueKind == JsonValueKind.String)
                {
                    return error.GetString();
                }
            }
            catch (JsonException)
            {
                // Not JSON (e.g. an HTML 404 page when the endpoint does not exist)
            }
            return null;
        }
    }
}
