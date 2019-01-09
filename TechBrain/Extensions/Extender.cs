using System.Collections;
using System.Text;

namespace TechBrain.Extensions
{
    public class Extender
    {
        public static string BuildStringSep(string separator, params object[] arr)
        {
            return InBuildString(arr, separator);
        }
        public static string BuildString(params object[] arr)
        {
            return InBuildString(arr, null);
        }

        static string InBuildString(IEnumerable arr, string separator = null)
        {
            if (arr == null)
                return null;

            var str = new StringBuilder();
            foreach (var obj in arr)
            {
                if (!(obj is string) && obj is IEnumerable)
                {
                    foreach (var obj2 in obj as IEnumerable)
                    {
                        str.Append(obj2);
                        if (separator!=null)
                        str.Append(separator);
                    }
                }
                else
                {
                    str.Append(obj);
                    if (separator != null)
                        str.Append(separator);
                }
            }
            return str.ToString();
        }
    }
}
