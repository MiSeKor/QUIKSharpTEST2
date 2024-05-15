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
            //СlassSaveLoadFiles = new СlassSaveLoad(Path);

            try
            {
                toolList = new BindingList<Tool>();
                var Lst = new List<string>();

                using (var reader = File.OpenText(Path))
                {
                    var Files = reader.ReadToEnd();
                    Lst = JsonConvert.DeserializeObject<List<string>>(Files);
                }

                foreach (var i in Lst)
                {
                    toolList.Add(new Tool(_quik, i));
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(" LOAD - "+ex.Message);
                //Close();
            }

            if (toolList != null)
            {
                //DataGrdTools.ItemsSource = toolList;
            } 
        }
         
        private void MainWind_Closed(object sender, EventArgs e)
        {
            var Lst = new List<string>();
            foreach (var item in toolList)
            {
                Lst.Add(item.SecurityCode);
            }
            using (StreamWriter writer = File.CreateText(Path))
            {
                string output = JsonConvert.SerializeObject(Lst); 
                writer.WriteLine(output);
            }
            //СlassSaveLoadFiles.SaveData(toolList); 
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // if (TextBoxAdd.Text != "")
            // {
            //     toolList.Add(new Tool(_quik, TextBoxAdd.Text));
            //     TextBoxAdd.Text = "";
            // } 
        } 

        void Demonsracia(object sender, RoutedEventArgs e)
        { 
            //СlassSaveLoadFiles.SaveData(toolList);
            MessageBox.Show("Абра-Кадабра");
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
