using QuikSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        //  public MainVM()
        // {
        //     СreateQuik();
        //     ListTools = new ObservableCollection<Tool>(); 
        //
        //    var SBER = new Tool(_quik, "SBER");
        //    var VTBR = new Tool(_quik, "VTBR");
        //    var RUAL = new Tool(_quik, "RUAL");
        //    _ListTools.Add(SBER);
        //    _ListTools.Add(VTBR);
        //    _ListTools.Add(RUAL);
        //} 
    }
}
