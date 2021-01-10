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
    }
}
