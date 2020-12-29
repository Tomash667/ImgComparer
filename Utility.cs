using System;

namespace ImgComparer
{
    public static class Utility
    {
        private static Random rnd = new Random();

        public static int Rand => rnd.Next();

        public static int NextPow2(int value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value++;
            return value;
        }

        // 2->1
        // 4->2
        // 8->3
        // 16->4
        // etc
        public static int Pow2Exponent(int value)
        {
            int exp = 0;
            while (value > 1)
            {
                ++exp;
                value /= 2;
            }
            return exp;
        }
    }
}
