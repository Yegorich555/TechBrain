using Newtonsoft.Json;
using System.IO;
using TechBrain.IO;

namespace TechBrain
{
    public class Config
    {
        public int DevServer_TcpPort { get; set; } = 1234;
        public int TcpResponseTimeout { get; set; } = 1000;
        public int Esp_TcpPort { get; set; } = 80;

        //public string ComPort { get; set; } = "COM3";
        public string PathDevices { get; set; } = "devices.json";
        public const string PathConfig = "config.json";

        public int DeviceScanTime { get; set; } = 1000;//ms
        public int SensorsScanTime { get; set; } = 5000;//todo change scan to 1 sec //ms
        public int DeviceCacheTime { get; set; } = 500;//ms

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
