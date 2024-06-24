using QuikSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using QUIKSharpTEST2.Servises;
using System.IO;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using QuikSharp.DataStructures;

namespace QUIKSharpTEST2
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    { 
        //      https://youtu.be/Mb3S2IK3NzI?si=jD-0e_BTP6PYbaOb 
        //СПИСОК ДЕЛ НА C# WPF ОТ НАЧАЛА ДО КОНЦА | DATAGRID | JSON ПАРСИНГ РАБОТА С ФАЙЛАМИ
        //
        public Quik _quik;
        private readonly string Path = $"{Environment.CurrentDirectory}\\ListTools.json";
        private СlassSaveLoad СlassSaveLoadFiles;
        //public List<Tool> toolList = [];

        //private Tool Sber, Vtbr, Rosn; 
        //private Tool _tool;

        private MainVM VM = new(); 
        public MainWindow()
        {
            InitializeComponent();
            DataContext = VM;
        }

        public Tool AddTool(string SecKod)
        {
            var T = new Tool(_quik, SecKod);
            return T;
        }

        private void MainWind_Loaded(object sender, RoutedEventArgs e)
        {
            СreateQuik();
            //MV.ListTools = [AddTool("SBER"), AddTool("VTBR"), AddTool("RUAL"), AddTool("GDM4")];

            //
            СlassSaveLoadFiles = new СlassSaveLoad(Path);

            try
            {
                List<string> Lst = [];

                using (var reader = File.OpenText(Path))
                {
                    var Files = reader?.ReadToEnd();
                    Lst = JsonConvert.DeserializeObject<List<string>>(Files);
                }
                //MV = new MainVM();
                foreach (var i in Lst)
                {
                    VM.ListTools.Add(new Tool(_quik, i));
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(" LOAD - "+ex.Message);
                //Close();
            }

            // if (toolList != null)
            // {
            //     //DataGrdTools.ItemsSource = toolList;
            // } 
        }

        async void MainWind_Closed(object sender, EventArgs e)
        {
            /*var orders = _quik.Orders.GetOrders().Result;
            if (orders.Count != 0)
            {
                foreach (var order in orders)
                {
                    if (order.State == State.Active)
                    {
                        await _quik.Orders.KillOrder(order).ConfigureAwait(true);
                    }
                }
            }

            var Stoporders = _quik.StopOrders.GetStopOrders().Result;
            if (Stoporders.Count != 0)
            {
                foreach (var stoporder in Stoporders)
                {
                    if (stoporder.State == State.Active)
                    {
                        await _quik.StopOrders.KillStopOrder(stoporder).ConfigureAwait(false);
                    }
                }
            }*/

            //_quik.StopService();
            var Lst = new List<string>();
            foreach (var item in VM.ListTools)
            {
                Lst.Add(item.SecurityCode);
            }
            using (StreamWriter writer = File.CreateText(Path))
            {
                string output = JsonConvert.SerializeObject(Lst);
                writer.WriteLine(output);
            }
            //СlassSaveLoadFiles.SaveData(toolList);
            СlassSaveLoadFiles.SaveData(Lst);
        }

        private void KillOperationOrders(object sender, RoutedEventArgs e)
        {
            VM.KillOperationOrders();
        }
        private void ClosPositions(object sender, RoutedEventArgs e)
        {
            VM.ClosPositions();
        }

        void Demonsracia(object sender, RoutedEventArgs e)
        { 
            //СlassSaveLoadFiles.SaveData(toolList);
            MessageBox.Show("Абра-Кадабра");
        }

        private void Button_Click_AddTool(object sender, RoutedEventArgs e)
        {
            if (txBoxAddTool.Text != "")
            {
                VM.ListTools.Add(AddTool(txBoxAddTool.Text));
                txBoxAddTool.Text = "";
            }
        }

        private Quik СreateQuik()
        {
            try
            {
                _quik = new Quik(Quik.DefaultPort, new InMemoryStorage());

                if (_quik != null)
                {
                    if (_quik.Service.IsConnected().Result)
                    {
                        MainWind.Title += " Ok";
                        MainWind.Background = Brushes.Aqua;
                    }
                    else
                    {
                        MainWind.Content += "НЕ Ok";
                        MainWind.Background = Brushes.Crimson;
                    }

                } 
            } 
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return _quik;
        }

        private void Button_Remove_Tool_OnClick(object sender, RoutedEventArgs e)
        {
            VM.Remove();
        }

        private void CloseApp(object sender, MouseButtonEventArgs e)
        {
            try
            { 
                Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message); 
            }
        }

        private void MinimizApp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.WindowState = WindowState.Minimized;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        //private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        //{
            
        //}

        //private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        //{
        //    MV.SelectedTool.Сheck_Isactiv();
        //}

        //private void ChekStrategys(object sender, SelectionChangedEventArgs e)
        //{
        //    MV.SelectedTool.Сheck();
        //}
        private void DataGrid1_OnBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (VM.SelectedTool.Isactiv) e.Cancel = true;
        }
    }
}
