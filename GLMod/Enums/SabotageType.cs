namespace GLMod.Enums
{
    /// <summary>
    /// Represents the different types of sabotages in Among Us
    /// </summary>
    public enum SabotageType
    {
        Reactor,
        Coms,
        Lights,
        O2
    }

    /// <summary>
    /// Extension methods for SabotageType enum
    /// </summary>
    public static class SabotageTypeExtensions
    {
        /// <summary>
        /// Converts SabotageType to string representation
        /// </summary>
        public static string ToActionString(this SabotageType sabotageType)
        {
            return sabotageType.ToString();
        }

        /// <summary>
        /// Tries to parse a sabotage type from a string
        /// </summary>
        public static bool TryParse(string value, out SabotageType result)
        {
            switch (value)
            {
                case "Reactor":
                    result = SabotageType.Reactor;
                    return true;
                case "Coms":
                    result = SabotageType.Coms;
                    return true;
                case "Lights":
                    result = SabotageType.Lights;
                    return true;
                case "O2":
                    result = SabotageType.O2;
                    return true;
                default:
                    result = default;
                    return false;
            }
        }
    }
}
