using System.Collections.Generic;

namespace ImgComparer
{
    public static class Extensions
    {
        public static string TrimExt(this string str, int length)
        {
            if (str.Length > length)
            {
                int pos = str.LastIndexOf('.');
                if (pos == -1)
                    return $"{str.Substring(0, length)}...";
                else
                {
                    string ext = str.Substring(pos, str.Length - pos);
                    return $"{str.Substring(0, length - ext.Length - 2)}..{ext}";
                }
            }
            else
                return str;
        }

        public static T GetValueOptional<T>(this Dictionary<string, T> dict, string key)
        {
            if (dict.TryGetValue(key, out T value))
                return value;
            return default;
        }
    }
}
