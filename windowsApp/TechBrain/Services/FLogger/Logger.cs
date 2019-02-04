using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TechBrain.Extensions;

namespace TechBrain.Services.FLogger
{
    public class Logger
    {
        private Logger() { }
        private static Logger _instance;

        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Logger();
                return _instance;
            }
        }

        LoggerSettings _settings;
        public LoggerSettings Settings
        {
            get
            {
                if (_settings == null)
                    _settings = new LoggerSettings();
                return _settings;
            }
            internal set { _settings = value; }
        }

        object lockThread = new object();
        public void Log(LogLevel level, params string[] messages)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            if (Settings.AsyncEnable)
            {
                Task.Run(() => { GoLog(level, threadId, messages); });
            }else
            {
                GoLog(level, threadId, messages);
            }
        }

        void GoLog(LogLevel level, int threadId, params string[] messages)
        {
            lock (lockObj)
            {
                try
                {
                    var now = DateTime.Now;
                    var str = new StringBuilder();
                    str.Append('[');
                    str.Append(now.ToString(Settings.DateTimeFormat));
                    str.Append(']');
                    str.Append(" Thread:");
                    str.Append(threadId.ToString("D3"));
                    str.Append(' ');
                    str.Append(level.ToString().ToUpper());
                    str.Append(" - ");

                    if (messages != null)
                    {
                        foreach (var message in messages)
                        {
                            if (message != null)
                            {
                                str.Append(message);
                                str.Append(' ');
                            }
                        }
                    }
                    System.Diagnostics.Trace.WriteLine(str.ToString());

                    str.Append(Environment.NewLine);
                    str.Append(Environment.NewLine);

                    if (!Settings.FilePath.IsNull())
                        SaveToFile(str.ToString(), now);
                }
                catch { }
            }
        }

        object lockObj = new object();

        bool dirExists;
        void SaveToFile(string text, DateTime now)
        {
            if (!dirExists)
            {
                var dir = Path.GetDirectoryName(Settings.FilePath);
                Directory.CreateDirectory(dir);
                dirExists = true;
            }

            var isMoved = CheckArchive(now);
            try
            {
                File.AppendAllText(Settings.FilePath, text);
            }
            catch (DirectoryNotFoundException)
            {
                dirExists = false;
                SaveToFile(text, now);
            }

            if (isMoved)
                File.SetCreationTime(Settings.FilePath, now);
            lastCreation = now;
        }


        DateTime lastCreation;
        bool CheckArchive(DateTime now)
        {
            bool isMoved = false;
            try
            {
                if (Settings.ArchiveFilePath == null)
                    return isMoved;

                if (lastCreation.Date == now.Date)
                    return isMoved;

                var dir = Path.GetDirectoryName(Settings.ArchiveFilePath);

                if (File.Exists(Settings.FilePath))
                {
                    if (lastCreation == DateTime.MinValue)
                    {
                        lastCreation = File.GetCreationTime(Settings.FilePath);
                        if (lastCreation.Date == now.Date)
                            return isMoved;
                    }
                    Directory.CreateDirectory(dir);
                    var destPath = Settings.ArchiveFilePath.Replace("{#}", now.ToString(Settings.ArchiveDateFormat));
                    if (File.Exists(destPath))
                        File.Delete(destPath);
                    File.Move(Settings.FilePath, destPath);
                    isMoved = true;
                }

                if (Settings.ArchiveMaxFiles != -1)
                {
                    var searchPattern = Path.GetFileName(Settings.ArchiveFilePath.Replace("{#}", "*"));
                    var files = Directory.GetFiles(dir, searchPattern, SearchOption.TopDirectoryOnly);
                    for (int i = files.Length; i > Settings.ArchiveMaxFiles; --i)
                        File.Delete(files[i - 1]);
                }
            }
            catch { }
            return isMoved;
        }

        public static void Info(string message)
        {
            Instance.Log(LogLevel.Info, message);
        }

        public static void Info(string request, string message)
        {
            Instance.Log(LogLevel.Info, request, message);
        }

        public static void Error(Exception ex)
        {
            Instance.Log(LogLevel.Error, ex.ToString());
        }

        public static void Error(string errorMsg, Exception ex)
        {
            Instance.Log(LogLevel.Error, errorMsg, Environment.NewLine, ex.ToString());
        }

        public static void ErrorSync(string errorMsg, Exception ex)
        {
            Instance.GoLog(LogLevel.Error, Thread.CurrentThread.ManagedThreadId, errorMsg, Environment.NewLine, ex.ToString());
        }

        public static void Error(string errorMsg)
        {
            Instance.Log(LogLevel.Error, errorMsg);
        }

        public static void Debug(string message)
        {
            //Instance.Log(LogLevel.Debug, message);
            System.Diagnostics.Trace.WriteLine(message);
        }

    }
}