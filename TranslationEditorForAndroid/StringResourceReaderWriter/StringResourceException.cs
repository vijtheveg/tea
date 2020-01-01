using System;
namespace Com.MeraBills.StringResourceReaderWriter
{
    public abstract class StringResourceException : Exception
    {
        public StringResourceException() : base()
        { }

        public StringResourceException(string message) : base(message)
        { }
    }
}
