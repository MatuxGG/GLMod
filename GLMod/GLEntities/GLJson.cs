using System;
using System.Text.Json;

namespace GLMod
{
    public static class GLJson
    {
        public static T Deserialize<T>(string jsonString)
        {
            try
            {
                T obj = JsonSerializer.Deserialize<T>(jsonString);
                return obj;
            }
            catch (Exception ex)
            {
                GLMod.log("Deserialize Error: " + ex.Message);
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
                GLMod.log("Serialize Error: " + ex.Message);
                return null;
            }
        }
    }
}