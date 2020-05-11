using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace Com.MeraBills.StringResourceReaderWriter
{
    public sealed class PluralsContent : ResourceContent, IEquatable<PluralsContent>
    {
        public PluralsContent() : base()
        {
            this.Values = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public PluralsContent(XmlReader reader) : this()
        {
            Read(reader);
        }

        public override void Read(XmlReader reader)
        {
            if (reader.IsEmptyElement)
            {
                // Read past the empty element
                reader.Skip();
                return;
            }

            // Read past the start element
            if (!reader.Read())
                throw new ArgumentException("Reader ended unexpectedly");

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    reader.Skip();
                    continue;   // Ignore everything but elements
                }

                if (string.CompareOrdinal(reader.LocalName, ItemElementName) != 0)
                    throw new ArgumentException("<item> expected");

                string quantity = reader.GetAttribute(QuantityAttributeName);
                if (string.IsNullOrEmpty(quantity))
                    throw new ArgumentException("quantity attriute is required");

                string value = StringContent.ReadStringValue(reader);
                this.Values.Add(quantity, value);
            }

            // Read past the end element
            reader.ReadEndElement();
        }

        public override void Write(XmlWriter writer)
        {
            if ((this.Values == null) || (this.Values.Count <= 0))
                return;

            foreach(var pair in this.Values)
            {
                writer.WriteStartElement(ItemElementName);
                writer.WriteAttributeString(QuantityAttributeName, pair.Key);
                StringContent.WriteStringValue(writer, pair.Value);
                writer.WriteEndElement();
            }
        }

        public override ResourceContent CreateTargetContent(ResourceContent oldSourceContent, ResourceContent oldTargetContent)
        {
            var oldTarget = (oldTargetContent as PluralsContent) ?? new PluralsContent();
            var newTarget = new PluralsContent();
            foreach(var oldSourceItemKey in this.Values.Keys)
            {
                oldTarget.Values.TryGetValue(oldSourceItemKey, out var oldTargetItemValue);
                newTarget.Values.Add(oldSourceItemKey, oldTargetItemValue);
            }
            return newTarget;
        }

        public override bool HasTranslatableContent
        {
            get
            {
                if ((this.Values == null) || (this.Values.Count <= 0))
                    return false;

                foreach (string value in this.Values.Values)
                    if (StringContent.ValueNeedsTranslation(value))
                        return true;

                return false;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PluralsContent);
        }

        public bool Equals([AllowNull] PluralsContent other)
        {
            if (other == null)
                return false;

            if ((this.Values == null) != (other.Values == null))
                return false;

            if (this.Values == null)
                return true;

            int count = this.Values.Count;
            if (count != other.Values.Count)
                return false;

            foreach (var pair in this.Values)
            {
                string otherValue = other.Values[pair.Key];
                if (string.CompareOrdinal(pair.Value, otherValue) != 0)
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            if (this.Values != null)
                foreach (var pair in this.Values)
                {
                    hashCode.Add(pair.Key);
                    hashCode.Add(pair.Value);
                }

            return hashCode.ToHashCode();
        }

        public const string ItemElementName = "item";
        public const string QuantityAttributeName = "quantity";

        public readonly Dictionary<string, string> Values;
    }
}
