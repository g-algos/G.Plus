using System.Globalization;
using System.Windows.Data;

namespace GPlus.UI.Helpers.Converters
{
    public class IsValidElementIdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DBNull.Value) return false;

#if V2023
            if (int.TryParse(value.ToString(), out int id))
#else
            if (long.TryParse(value.ToString(), out long id))
#endif
            {
                var elementId = new ElementId(id);
                return new ElementId(id) == ElementId.InvalidElementId;
            }

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("One-way converter only.");
        }
    }
}
