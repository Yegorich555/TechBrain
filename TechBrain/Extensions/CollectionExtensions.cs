using System.Collections.Generic;

namespace TechBrain.Extensions
{
    public static class CollectionExtensions
    {
        public static List<T> GetRangeByIndex<T>(this List<T> lst, int startIndex, int endIndex)
        {
            if (lst == null)
                return null;
            return lst.GetRange(startIndex, endIndex - startIndex + 1);
        }

       public static int LastIndex<T>(this IList<T> lst)
        {
            if (lst == null)
                return -1;
            return lst.Count - 1;
        }

        public static bool IndexExist<T>(this IList<T> lst, int index)
        {
            if (lst == null)
                return false;

            return lst.LastIndex() >= index;
        }

        public static bool TryGetValue<T>(this IList<T> lst, int index, out T value)
        {
            if (lst.IndexExist(index))
            {
                value = lst[index];
                return true;
            }
            value = default(T);
            return false;
        }
    }
}
