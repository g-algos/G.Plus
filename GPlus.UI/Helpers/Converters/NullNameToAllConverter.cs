using System.Globalization;
using System.Windows.Data;
namespace GPlus.UI.Helpers.Converters
{
    public class NullNameToAllConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is null)
                return "All";

            var name = value.GetType().GetProperty("Name");
            if (name == null)
                return value.ToString()??"None";
            else 
                return name!.GetValue(value, null)!.ToString()?? "None";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
