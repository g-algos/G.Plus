using System.Globalization;
using System.Windows.Data;

namespace GPlus.UI.Helpers.Converters
{
    public class StorageTypeToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is StorageType type)
            {
                return type == StorageType.Integer || type == StorageType.Double;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
