using System.Xml;

namespace Com.MeraBills.StringResourceReaderWriter
{
    public abstract class ResourceContent
    {
        public ResourceContent()
        { }

        public abstract void Read(XmlReader reader);

        public abstract void Write(XmlWriter writer);

        public abstract bool HasNonEmptyContent { get; }

        public static bool Equals(ResourceContent lhs, ResourceContent rhs)
        {
            if (lhs == null)
                return rhs == null;
            else
                return lhs.Equals(rhs);
        }
    }
}
