using System;
using System.Text.Json;

namespace GLMod.GLEntities
{
    public static class GLJson
    {
        // Truncate payload snippets in logs so we never spam BepInEx with multi-MB strings.
        private const int LogSnippetMaxLength = 200;

        public static T Deserialize<T>(string jsonString)
        {
            try
            {
                T obj = JsonSerializer.Deserialize<T>(jsonString);
                return obj;
            }
            catch (Exception ex)
            {
                GLMod.log($"[GLJson.Deserialize<{typeof(T).Name}>] {ex.GetType().Name}: {ex.Message} | payload: {Snippet(jsonString)}");
                return default(T);
            }
        }

        public static string Serialize<T>(T obj)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(obj);
                return jsonString;
            }
            catch (Exception ex)
            {
                GLMod.log($"[GLJson.Serialize<{typeof(T).Name}>] {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private static string Snippet(string payload)
        {
            if (payload == null) return "<null>";
            if (payload.Length == 0) return "<empty>";
            return payload.Length <= LogSnippetMaxLength
                ? payload
                : payload.Substring(0, LogSnippetMaxLength) + "…(+" + (payload.Length - LogSnippetMaxLength) + " chars)";
        }
    }
}