using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace QUIKSharpTEST2.Servises
{
    internal class СlassSaveLoad
    {
        private readonly string Path;

        public СlassSaveLoad(string path)
        {
            Path = path;
        }
        public BindingList<Tool> LoadData()
        {
            var FilesExists = File.Exists(Path);
            if (!FilesExists)
            {
                File.CreateText(Path).Dispose();
                return new BindingList<Tool>();
            }

            using (var reader = File.OpenText(Path))
            {
                 var Files = reader.ReadToEnd();
                //var Files = reader.ReadLine();
                return JsonConvert.DeserializeObject<BindingList<Tool>>(Files);
            }
        }
         
        public void SaveData(object toolList)
        {
            using (StreamWriter writer = File.CreateText(Path))
            {
                string output = JsonConvert.SerializeObject(toolList);
                //writer.Write(output);
                writer.WriteLine(output);
            }
        }
    }
}
