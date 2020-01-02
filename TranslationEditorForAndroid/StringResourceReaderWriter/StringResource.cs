using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Xml;

namespace Com.MeraBills.StringResourceReaderWriter
{
    public sealed class StringResource : IEquatable<StringResource>
    {
        public StringResource(ResourceType resouceType)
        {
            if (resouceType == ResourceType.Other)
                throw new ArgumentException("A string resource type must be specified");

            this.ResourceType = resouceType;
            this.IsTranslatable = IsTranslatableDefault;
            this.HasFormatSpecifiers = HasFormatSpecifiersDefault;

            if (resouceType == ResourceType.String)
                this.Content = new StringContent();
            else if (this.ResourceType == ResourceType.StringArray)
                this.Content = new StringArrayContent();
            else
                this.Content = new PluralsContent();
        }

        public StringResource(ResourceType resouceType, XmlReader reader) : this(resouceType)
        {
            if (reader.NodeType != XmlNodeType.Element)
                throw new ArgumentException("Reader is not positioned on an element");

            string attributeValue = reader.GetAttribute(NameAttributeName);
            this.Name = string.IsNullOrEmpty(attributeValue) ? throw new ArgumentException("The name attribute is required") : attributeValue;

            attributeValue = reader.GetAttribute(TranslatableAttributeName);
            this.IsTranslatable = attributeValue == null ? IsTranslatableDefault : (string.CompareOrdinal(attributeValue, FalseValue) != 0);

            attributeValue = reader.GetAttribute(FormattedAttributeName);
            this.HasFormatSpecifiers = attributeValue == null ? HasFormatSpecifiersDefault : (string.CompareOrdinal(attributeValue, FalseValue) != 0);

            this.Content.Read(reader);
        }

        public static ResourceType GetResourceType(string elementName)
        {
            if (string.CompareOrdinal(elementName, StringElementName) == 0)
                return ResourceType.String;

            if (string.CompareOrdinal(elementName, StringArrayElementName) == 0)
                return ResourceType.StringArray;

            if (string.CompareOrdinal(elementName, PluralsElementName) == 0)
                return ResourceType.Plurals;

            return ResourceType.Other;
        }

        public void Write(XmlWriter writer)
        {
            if (this.Source != null)
                writer.WriteComment(DoNotModify + this.Source.ToString());

            string startElementName;
            if (this.ResourceType == ResourceType.String)
                startElementName = StringElementName;
            else if (this.ResourceType == ResourceType.StringArray)
                startElementName = StringArrayElementName;
            else
                startElementName = PluralsElementName;
            writer.WriteStartElement(startElementName);

            writer.WriteAttributeString(NameAttributeName, this.Name);

            if (this.IsTranslatable != IsTranslatableDefault)
                writer.WriteAttributeString(TranslatableAttributeName, this.IsTranslatable ? TrueValue : FalseValue);

            if (this.HasFormatSpecifiers != HasFormatSpecifiersDefault)
                writer.WriteAttributeString(FormattedAttributeName, this.HasFormatSpecifiers ? TrueValue : FalseValue);

            this.Content.Write(writer);

            writer.WriteEndElement();
        }

        public bool HasNonEmptyContent
        {
            get => (this.Content != null) && this.Content.HasNonEmptyContent;
        }

        public override string ToString()
        {
            var writerSettings = new XmlWriterSettings
            {
                Async = false,
                Indent = false,
                NewLineChars = "",
                OmitXmlDeclaration = true
            };

            using(var memoryStream = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(memoryStream, writerSettings))
                    this.Write(writer);

                return System.Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        public static StringResource FromString(string serialized)
        {
            if (string.IsNullOrEmpty(serialized))
                throw new ArgumentNullException(nameof(serialized));

            var readerSettings = new XmlReaderSettings
            {
                Async = false,
                IgnoreComments = true
            };

            using(var memoryStream = new MemoryStream(System.Convert.FromBase64String(serialized)))
            {
                using var reader = XmlReader.Create(memoryStream, readerSettings);
                if (!reader.Read())
                    throw new ArgumentException("Reader ended unexpectedly");

                var resourceType = GetResourceType(reader.LocalName);
                return new StringResource(resourceType, reader);
            }
        }

        public void SetContent(ResourceContent newContent)
        {
            bool updateRequired = false;
            if (this.Content == null)
            {
                if (newContent != null)
                    updateRequired = true;
                // else: nothing has changed
            }
            else
            {
                if (!this.Content.Equals(newContent))
                    updateRequired = true;
                // else: nothing has changed
            }

            if (updateRequired)
            {
                this.Content = newContent;

                // Reset source whenever content changes
                // The presence of source indicates that the content was finalized by a human
                this.Source = null;
            }
        }

        public void TrySetSourceFromComment(string comment)
        {
            if (string.IsNullOrEmpty(comment))
                return;

            if (!comment.StartsWith(DoNotModify, StringComparison.Ordinal))
                return;

            try
            {
                this.Source = StringResource.FromString(comment.Substring(DoNotModify.Length));
            }
            catch
            {
                // Ignore any errors during deserialization from comment - just don't set the source in this case
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as StringResource);
        }

        public bool Equals([AllowNull] StringResource other)
        {
            return other != null &&
                   ResourceType == other.ResourceType &&
                   string.CompareOrdinal(Name, other.Name) == 0 &&
                   ResourceContent.Equals(this.Content, other.Content);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ResourceType, Name, Content);
        }

        public readonly ResourceType ResourceType;

        public string Name { get; set; }

        public bool HasFormatSpecifiers { get; set; }

        public bool IsTranslatable { get; set; }

        public ResourceContent Content { get; private set; }

        public StringResource Source { get; set; }

        public String FileName { get; set; }

        public List<String> CommentLines { get; set; }

        public const string StringElementName = "string";
        public const string StringArrayElementName = "string-array";
        public const string PluralsElementName = "plurals";
        public const string NameAttributeName = "name";
        public const string TranslatableAttributeName = "translatable";
        public const string FormattedAttributeName = "formatted";
        public const string TrueValue = "true";
        public const string FalseValue = "false";
        public const string DoNotModify = "**DO NOT EDIT**";

        public const bool IsTranslatableDefault = true;
        public const bool HasFormatSpecifiersDefault = true;
    }
}
