using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace QUIKSharpTEST2
{
    public class EnumToArrayConverter : IValueConverter
    {
        //https://www.cyberforum.ru/wpf-silverlight/thread3167641.html
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var rr = Enum.GetValues(value as Type);
            return rr;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null; // I don't care about this
        }
    }
}
