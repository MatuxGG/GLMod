namespace GLMod.GLEntities
{
    /// <summary>
    /// Answer of POST /user/login for a Steam session ticket.
    /// Real JSON types (not the string convention of the game upload); names match the JSON keys.
    /// </summary>
    public class GLLoginResponse
    {
        /// <summary>Secret game token ("glm_…"), sent back as "Authorization: Bearer".</summary>
        public string token { get; set; }

        /// <summary>Good Loss account name.</summary>
        public string username { get; set; }

        /// <summary>Expiry of the token, Unix seconds.</summary>
        public long? expiresAt { get; set; }
    }
}
