using System;
using System.ComponentModel;
using System.Globalization;

namespace FastReport.Layout
{
    // FRX uses invariant comma-separated integers regardless of the host's UI culture.
    internal sealed class PaddingConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
            sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
            destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string text)
            {
                string[] parts = text.Split(',');
                if (parts.Length != 4)
                    throw new FormatException("Padding requires four integers: left, top, right, bottom.");
                return new Padding(Parse(parts[0]), Parse(parts[1]), Parse(parts[2]), Parse(parts[3]));
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
            Type destinationType)
        {
            if (destinationType == typeof(string) && value is Padding padding)
                return FormattableString.Invariant($"{padding.Left},{padding.Top},{padding.Right},{padding.Bottom}");
            return base.ConvertTo(context, culture, value, destinationType);
        }

        private static int Parse(string value) => int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }
}
