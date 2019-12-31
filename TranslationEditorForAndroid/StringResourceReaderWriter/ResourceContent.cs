using System.Xml;

namespace Com.MeraBills.StringResourceReaderWriter
{
    public abstract class ResourceContent
    {
        public ResourceContent(XmlReader _)
        { }

        public abstract void Write(XmlWriter writer);
    }
}
