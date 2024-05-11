using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                return JsonConvert.DeserializeObject<BindingList<Tool>>(Files);
            }
        }

        public void SaveData(object litoolListst)
        {
            using (StreamWriter writer = File.CreateText(Path))
            {
                string output = JsonConvert.SerializeObject(litoolListst);
                writer.WriteLine(output);
            }
        }
    }
}
