using Newtonsoft.Json;
using System.IO;
using TechBrain.IO;

namespace TechBrain
{
    public class Config
    {
        public int DevServer_TcpPort { get; private set; } = 1234;
        public int TcpResponseTimeout { get; private set; } = 1000;
        public int Esp_TcpPort { get; private set; } = 80;

        //public string ComPort { get; private set; } = "COM3";
        public string PathDevices { get; private set; } = "devices.json";

        public const string PathConfig = "config.json";

        public void SaveToFile()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(PathConfig, json);
        }

        public static Config ReadFromFile()
        {
            if (FileSystem.TryRead(PathConfig, out string text))
                return JsonConvert.DeserializeObject<Config>(text);
            else
            {
                var v = new Config();
                v.SaveToFile();
                return v;
            }
        }


    }
}
