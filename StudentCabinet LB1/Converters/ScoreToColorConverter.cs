using System.Globalization;

namespace StudentCabinet_LB1.Converters
{
    public class ScoreToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double score && score >= 4.0)
                return Colors.Green;
            return Colors.Black;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
