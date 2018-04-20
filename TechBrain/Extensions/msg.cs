using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace TechBrain.Extensions
{
    public class msg
    {
        static StringBuilder StackTraceResult
        {
            get
            {
                var st = new StackTrace(true);
                var sf = st.GetFrame(3);

                var str = new StringBuilder();
                str.Append("Method: ");
                str.AppendLine(sf.GetMethod().ToString());
                str.Append("LineNumber: ");
                str.AppendLine(sf.GetFileLineNumber().ToString());
                str.AppendLine("---");
                return str;
            }
        }

        public static string ShowSpeedWatch(Action method, string description = null)
        {
            var str = StackTraceResult;
            str.Append("speed = ");
            str.Append(SpeedWatch(method));
            str.Append("ms");
            if (description != null)
            {
                str.Append("(");
                str.Append(description);
                str.Append(")");
            }
            str.AppendLine();

            return WriteText(str);
        }

        public static double SpeedWatch(Action method)
        {
            var st = new Stopwatch();
            st.Start();
            method();
            st.Stop();
            return st.Elapsed.TotalMilliseconds;
        }

        public static string Show(params object[] objArr)
        {
            return _Show(false, objArr);
        }

        public static string ShowWithTypeNames(params object[] objArr)
        {
            return _Show(true, objArr);
        }

        static string _Show(bool withTypeNames, params object[] objArr)
        {
            try
            {
                var str = StackTraceResult;
                var settings = new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    TypeNameHandling = withTypeNames ? TypeNameHandling.Auto : TypeNameHandling.None
                };
                settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                var json = JsonConvert.SerializeObject(objArr, settings);
                str.Append(json);

                return WriteText(str);
            }
            catch (Exception ex)
            {
                return WriteText("Error: " + MethodBase.GetCurrentMethod().Name + "()\n" + ex.ToString());
            }
        }

        public static string ShowStackTrace(object obj = null)
        {
            try
            {
                var str = new StringBuilder();
                if (obj != null)
                    str.AppendLine(obj.ToString());

                int index = -1;
                foreach (StackFrame sf in (new StackTrace(true)).GetFrames())
                {
                    ++index;
                    if (index == 0) continue;
                    str.Append(index);
                    str.Append("; LineNumber:");
                    str.Append(sf.GetFileLineNumber());
                    str.Append("; Method: ");
                    str.AppendLine(sf.GetMethod().ToString());
                }
                return WriteText(str);
            }
            catch (Exception ex)
            {
                return WriteText("Error: " + MethodBase.GetCurrentMethod().Name + "()\n" + ex);
            }
        }

        static string WriteText(StringBuilder stringBuilder)
        {
            return WriteText(stringBuilder.ToString());
        }
        static string WriteText(string text)
        {
            Trace.WriteLine(text);
            return text;

        }
    }
}
