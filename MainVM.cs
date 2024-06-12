using QuikSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;

namespace QUIKSharpTEST2
{
    //https://www.cyberforum.ru/wpf-silverlight/thread3167641.html
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

        // команда удаления  
        public void Remove()
        {
            
            if (!SelectedTool.Isactiv)
            {
                SelectedTool.Log(SelectedTool.Name + " - Удаление и чистка");
                SelectedTool.KillOperationOrders();
                SelectedTool.Unsubscribe();
                ListTools.Remove(SelectedTool); 
            }
            else
            {
                SelectedTool.Log("Удаление и чистка" + SelectedTool.Name + " НЕ ВОЗМОЖНА при включенной стратегии");
            }
        }
        public void KillOperationOrders()
        {
            _SelectedTool.Isactiv = false;
           // _SelectedTool.KillOperationOrders(); 
        }
        public void ClosPositions()
        {
            _SelectedTool.Isactiv = false; 
            _SelectedTool.CloseAllpositions();
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

    internal class RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        : ICommand
    {
        event EventHandler ICommand.CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;

            remove => CommandManager.RequerySuggested -= value;
        }

        bool ICommand.CanExecute(object parameter)
        {
            return canExecute == null || canExecute(parameter);
        }

        void ICommand.Execute(object parameter)
        {
            execute(parameter);
        }
    }
}
