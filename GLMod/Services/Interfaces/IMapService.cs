namespace GLMod.Services.Interfaces
{
    /// <summary>
    /// Interface for managing map-related operations
    /// </summary>
    public interface IMapService
    {
        /// <summary>
        /// Gets the current map name
        /// </summary>
        /// <returns>Map name or "Unknown" if unable to determine</returns>
        string GetMapName();
    }
}
