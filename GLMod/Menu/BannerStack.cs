namespace GLMod.Menu
{
    /// <summary>
    /// Banners of the main menu: centered, sharing the width of the widest one, stacked up from the
    /// bottom edge of the screen. Pure.
    /// </summary>
    public static class BannerStack
    {
        /// <summary>
        /// Left edge of every banner of this width
        /// </summary>
        public static float Left(float screenWidth, float width)
        {
            return (screenWidth - width) / 2f;
        }

        /// <summary>
        /// Top of the banner at this index, 0 being the one flush with the bottom edge
        /// </summary>
        public static float Top(float screenHeight, float height, float gap, int index)
        {
            return screenHeight - (index + 1) * height - index * gap;
        }
    }
}
