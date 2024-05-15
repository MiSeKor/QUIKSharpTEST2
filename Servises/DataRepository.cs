using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QUIKSharpTEST2.Servises
{
    internal class DataRepository
    {
        private readonly string Path = $"{Environment.CurrentDirectory}\\IEnumerableListTools.json";
        private readonly Encoding _encoding = Encoding.UTF8;

        public void Save(IEnumerable<Tool> list)
        {
            using (var stream = new StreamWriter(Path, false, _encoding))
            using (var writer = new JsonTextWriter(stream))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(writer, list);
            }
        }

        public IEnumerable<Tool> Load()
        {
            using (var stream = new StreamReader(Path, _encoding))
            using (var reader = new JsonTextReader(stream))
            {
                var serializer = new JsonSerializer();
                var data = serializer.Deserialize<Tool[]>(reader) ?? Array.Empty<Tool>();
                return data;
            }
        }
    }
}
