namespace Com.MeraBills.StringResourceReaderWriter
{
    public sealed class DuplicateStringResourceException : StringResourceException
    {
        public DuplicateStringResourceException(string stringResourceName) : base()
        {
            this.StringResourceName = stringResourceName;
        }

        public readonly string StringResourceName;
    }
}
