using System;

namespace ImgComparer
{
    public static class Utility
    {
        private static Random rnd = new Random();

        public static int Rand => rnd.Next();
    }
}
