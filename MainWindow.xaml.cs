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

        private readonly string Path = $"{Environment.CurrentDirectory}\\ListTools.json";
        private СlassSaveLoad СlassSaveLoadFiles;
        public BindingList<Tool> toolList;
        //List<Tool> toolList;
        public Quik _quik; 
        private Tool Sber, Vtbr, Rosn; 
        private Tool _tool;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWind_Loaded(object sender, RoutedEventArgs e)
        {
            СreateQuik();
            СlassSaveLoadFiles = new СlassSaveLoad(Path);

            try
            {
                toolList = new BindingList<Tool>();
                var Lst = new List<string>();

                using (var reader = File.OpenText(Path))
                {
                    var Files = reader.ReadToEnd();
                    //var Files = reader.ReadLine();
                    Lst = JsonConvert.DeserializeObject<List<string>>(Files);
                }

                foreach (var i in Lst)
                {
                    toolList.Add(new Tool(_quik, i));
                }

                //toolList = new List<Tool>();
                // toolList = new BindingList<Tool>();
                // toolList = СlassSaveLoadFiles.LoadData();
                // foreach (var i in toolList)
                // {
                //     new Tool(_quik, i.SecurityCode);
                // }

            }
            catch (Exception ex)
            {
                MessageBox.Show(" LOAD - "+ex.Message);
                //Close();
            }

            try
            {
                //toolList = new List<Tool>()
                 // toolList = new BindingList<Tool>()
                 // {
                 //     new Tool(_quik,"SBER"),
                 //     new Tool(_quik,"VTBR"),
                 //     new Tool(_quik,"ROSN"),
                 //     new Tool(_quik,"RUAL"),
                 // }; 
            }
            catch (Exception СreateTool)
            {
                Console.WriteLine("СreateTool "+СreateTool); 
            }

            if (toolList != null)
            {
                DataGrdTools.ItemsSource = toolList;
                //toolList.ListChanged += ToolList_ListChanged;
            } 
        }

        private void ToolList_ListChanged(object sender, ListChangedEventArgs e)
        {
            /*if (e.ListChangedType == ListChangedType.ItemAdded
                || e.ListChangedType == ListChangedType.ItemDeleted
                || e.ListChangedType == ListChangedType.ItemChanged)
            {
                try
                {
                    СlassSaveLoadFiles.SaveData(sender);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Close();
                }
            }*/
        }

        private void MainWind_Closed(object sender, EventArgs e)
        {
            //СlassSaveLoadFiles.SaveData(toolList); 
        }

        void Demonsracia(object sender, RoutedEventArgs e)
        {
            var Lst = new List<string>();
            foreach (var item in toolList)
            {
                Lst.Add(item.SecurityCode);
            }
            using (StreamWriter writer = File.CreateText(Path))
            {
                string output = JsonConvert.SerializeObject(Lst);
                //writer.Write(output);
                writer.WriteLine(output);
            }

            //СlassSaveLoadFiles.SaveData(toolList);
            //MessageBox.Show("Абра-Кадабра");
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
 
    }
}
