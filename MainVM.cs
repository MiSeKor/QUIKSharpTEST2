using QuikSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace QUIKSharpTEST2
{
    internal class MainVM : ViewModelBase
    {
        //MainWindow wnd = (MainWindow)App.Current.MainWindow; 

        private ObservableCollection<Tool> _ListTools;
        private Tool _SelectedTool;

        public ObservableCollection<Tool> ListTools
        {
            get => _ListTools;
            set => SetField(ref _ListTools, value);
        }

        #region SelectedItem
        public Tool SelectedTool
        {
            get => _SelectedTool;
            set => SetField(ref _SelectedTool, value);
        }
        #endregion
        public enum Operation // Ваш Enum
        {
            Buy,
            Sell
        }
        public class EnumToArrayConverter : IValueConverter
        {
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
}
