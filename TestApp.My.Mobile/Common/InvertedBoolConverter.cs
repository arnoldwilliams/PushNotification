using System.Globalization;

namespace TestApp.My.Mobile.Common
{
    /// <summary>
    /// Returns the inverse of a boolean. Used to disable buttons while a command runs.
    /// </summary>
    public sealed class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b ? !b : true;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b ? !b : true;
    }
}
