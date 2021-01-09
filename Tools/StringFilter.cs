using System;
using System.Collections.Generic;

namespace ImgComparer.Tools
{
    public class StringFilter
    {
        private enum Op
        {
            Exact,
            StartWith,
            EndWith,
            Contains
        }

        private List<(Op, string)> ops = new List<(Op, string)>();

        public bool Required => ops.Count > 0;

        public StringFilter(string filter)
        {
            string[] parts = filter.Trim().Split(new char[] { ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return;

            foreach (string part in parts)
            {
                string str = part;
                bool startStar = false, endStar = false;
                if (str.StartsWith("*"))
                {
                    startStar = true;
                    str = str.Substring(1);
                }
                if (str.EndsWith("*"))
                {
                    endStar = true;
                    str = str.Substring(0, str.Length - 1);
                }
                if (str.Length == 0)
                    continue;

                Op op;
                if (startStar)
                {
                    if (endStar)
                        op = Op.Contains;
                    else
                        op = Op.EndWith;
                }
                else if (endStar)
                    op = Op.StartWith;
                else
                    op = Op.Exact;
                ops.Add((op, str));
            }
        }

        public bool Filter(string item)
        {
            foreach ((Op op, string str) in ops)
            {
                switch (op)
                {
                case Op.Exact:
                    if (item == str)
                        return true;
                    break;
                case Op.StartWith:
                    if (item.StartsWith(str))
                        return true;
                    break;
                case Op.EndWith:
                    if (item.EndsWith(str))
                        return true;
                    break;
                case Op.Contains:
                    if (item.Contains(str))
                        return true;
                    break;
                }
            }
            return false;
        }
    }
}
