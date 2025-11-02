namespace GLMod.Services.Interfaces
{
    /// <summary>
    /// Interface for managing mod configuration
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets the mod name
        /// </summary>
        string ModName { get; }

        /// <summary>
        /// Gets the configuration path
        /// </summary>
        string ConfigPath { get; }

        /// <summary>
        /// Finds and sets the mod name from .glmod file
        /// </summary>
        void FindModName();

        /// <summary>
        /// Sets the mod name manually
        /// </summary>
        /// <param name="modName">New mod name</param>
        void SetModName(string modName);
    }
}
