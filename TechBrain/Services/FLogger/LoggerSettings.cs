using System.IO;
using System.Reflection;

namespace TechBrain.Services.FLogger
{
    public class LoggerSettings
    {
        public LoggerSettings() { }

        public string DateTimeFormat { get; set; } = "yyyy-MM-dd hh:mm:ss.fff";
        public string FilePath { get; set; }
        public string ArchiveFilePath { get; set; }
        public string ArchiveDateFormat { get; set; } = "yyyyMMdd";
        public int ArchiveMaxFiles { get; set; } = 10;
        public bool EnableHttpTraceLog { get; set; }
    }
}

