namespace FastReport.Layout
{
    /// <summary>
    /// Determines how an image fits in a report object's content rectangle.
    /// Names and values preserve the FRX SizeMode representation.
    /// </summary>
    public enum ImageSizeMode
    {
        /// <summary>Draw at the image's original size from the content origin.</summary>
        Normal = 0,
        /// <summary>Stretch to fill the content rectangle.</summary>
        StretchImage = 1,
        /// <summary>Resize the report object to the image.</summary>
        AutoSize = 2,
        /// <summary>Center the image at its original size.</summary>
        CenterImage = 3,
        /// <summary>Fit the image while preserving its aspect ratio.</summary>
        Zoom = 4
    }
}
