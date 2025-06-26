using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GPlus.UI.Helpers.Converters
{
    public class RevitColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Autodesk.Revit.DB.Color revitColor)
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(revitColor.Red, revitColor.Green, revitColor.Blue));
            return System.Windows.Media.Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                var c = brush.Color;
                return new Autodesk.Revit.DB.Color(c.R, c.G, c.B);
            }
            return new Autodesk.Revit.DB.Color(0, 0, 0);
        }
    }
}
