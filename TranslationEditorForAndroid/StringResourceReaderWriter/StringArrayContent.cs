using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace Com.MeraBills.StringResourceReaderWriter
{
    public sealed class StringArrayContent : ResourceContent, IEquatable<StringArrayContent>
    {
        public StringArrayContent() : base()
        {
            this.Values = new List<string>();
        }

        public StringArrayContent(XmlReader reader) : this()
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

                string value = StringContent.ReadStringValue(reader);
                this.Values.Add(value);
            }

            // Read past the end element
            reader.ReadEndElement();
        }

        public override void Write(XmlWriter writer)
        {
            if ((this.Values == null) || (this.Values.Count <= 0))
                return;

            foreach(string value in this.Values)
            {
                writer.WriteStartElement(ItemElementName);
                StringContent.WriteStringValue(writer, value);
                writer.WriteEndElement();
            }
        }

        public override ResourceContent CreateTargetContent(ResourceContent oldSourceContent, ResourceContent oldTargetContent)
        {
            var oldTarget = (oldTargetContent as StringArrayContent) ?? new StringArrayContent();
            if (oldSourceContent is StringArrayContent oldSource)
            {
                // Keep translations of unchanged items
                var oldSourceToTarget = new Dictionary<string, string>(StringComparer.Ordinal);
                for(int i = oldSource.Values.Count - 1; i >= 0; --i)
                {
                    string oldTargetString = i < oldTarget.Values.Count ? oldTarget.Values[i] : null;
                    oldSourceToTarget.Add(oldSource.Values[i], oldTargetString);
                }

                oldTarget.Values.Clear();
                foreach(var newSourceString in this.Values)
                {
                    oldSourceToTarget.TryGetValue(newSourceString, out var newTargetString);
                    oldTarget.Values.Add(newTargetString);
                }
            }
            else
            {
                // No way of checking which items have changed - just make the target array lenght the same as the source array
                if (oldTarget.Values.Count > this.Values.Count)
                    oldTarget.Values.RemoveRange(this.Values.Count, oldTarget.Values.Count - this.Values.Count);
                else if (oldTarget.Values.Count < this.Values.Count)
                    oldTarget.Values.AddRange(new string[this.Values.Count - oldTarget.Values.Count]);
            }

            return oldTarget;
        }

        public override bool HasNonEmptyContent
        {
            get
            {
                if ((this.Values == null) || (this.Values.Count <= 0))
                    return false;

                foreach (string value in this.Values)
                    if (StringContent.IsValueNonEmpty(value))
                        return true;

                return false;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as StringArrayContent);
        }

        public bool Equals([AllowNull] StringArrayContent other)
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

            for (int i = 0; i < count; ++i)
                if (string.CompareOrdinal(this.Values[i], other.Values[i]) != 0)
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            if (this.Values != null)
                foreach (string value in this.Values)
                    hashCode.Add(value);

            return hashCode.ToHashCode();
        }

        public const string ItemElementName = "item";

        public readonly List<string> Values;
    }
}
