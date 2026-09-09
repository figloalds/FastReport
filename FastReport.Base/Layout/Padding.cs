using System;
using System.ComponentModel;

namespace FastReport.Layout
{
    /// <summary>
    /// Insets from the edges of a report object's bounds to its content, in report units.
    /// </summary>
    [TypeConverter(typeof(PaddingConverter))]
    public struct Padding : IEquatable<Padding>
    {
        /// <summary>Gets or sets the left inset.</summary>
        public int Left { get; set; }
        /// <summary>Gets or sets the top inset.</summary>
        public int Top { get; set; }
        /// <summary>Gets or sets the right inset.</summary>
        public int Right { get; set; }
        /// <summary>Gets or sets the bottom inset.</summary>
        public int Bottom { get; set; }

        /// <summary>Represents zero insets on every edge.</summary>
        public static readonly Padding Empty = new Padding(0);

        /// <summary>Creates equal insets on every edge.</summary>
        public Padding(int all) : this(all, all, all, all) { }

        /// <summary>Creates insets in left, top, right, bottom order.</summary>
        public Padding(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>Gets the combined left and right insets.</summary>
        public int Horizontal => Left + Right;
        /// <summary>Gets the combined top and bottom insets.</summary>
        public int Vertical => Top + Bottom;

        /// <inheritdoc/>
        public bool Equals(Padding other) => Left == other.Left && Top == other.Top &&
            Right == other.Right && Bottom == other.Bottom;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is Padding other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);

        /// <summary>Compares all four insets for equality.</summary>
        public static bool operator ==(Padding left, Padding right) => left.Equals(right);
        /// <summary>Compares all four insets for inequality.</summary>
        public static bool operator !=(Padding left, Padding right) => !left.Equals(right);
    }
}
