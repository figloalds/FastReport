using System;

namespace FastReport.Layout
{
    /// <summary>
    /// The edges of a report object's parent that remain at a fixed distance when the parent resizes.
    /// </summary>
    [Flags]
    public enum AnchorStyles
    {
        /// <summary>No edge is anchored.</summary>
        None = 0,
        /// <summary>Anchor to the top edge.</summary>
        Top = 1,
        /// <summary>Anchor to the bottom edge.</summary>
        Bottom = 2,
        /// <summary>Anchor to the left edge.</summary>
        Left = 4,
        /// <summary>Anchor to the right edge.</summary>
        Right = 8
    }
}
