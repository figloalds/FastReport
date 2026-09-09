namespace FastReport.Layout
{
    /// <summary>
    /// Positions a report object against an edge of its parent's available layout area.
    /// </summary>
    public enum DockStyle
    {
        /// <summary>Use the object's explicit bounds.</summary>
        None = 0,
        /// <summary>Occupy the top of the available area.</summary>
        Top = 1,
        /// <summary>Occupy the bottom of the available area.</summary>
        Bottom = 2,
        /// <summary>Occupy the left of the available area.</summary>
        Left = 3,
        /// <summary>Occupy the right of the available area.</summary>
        Right = 4,
        /// <summary>Occupy the remaining available area.</summary>
        Fill = 5
    }
}
