using System;

namespace ImgComparer
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class OrderByAttribute : Attribute
    {
        public string field;

        public OrderByAttribute(string field)
        {
            this.field = field;
        }
    }
}
