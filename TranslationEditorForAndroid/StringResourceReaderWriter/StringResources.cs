using System;
using System.Collections.Generic;
using System.Xml;

namespace Com.MeraBills.StringResourceReaderWriter
{
    public sealed class StringResources
    {
        public StringResources(string language, bool isSourceLanguage)
        {
            this.Language = string.IsNullOrEmpty(language) ? throw new ArgumentNullException(nameof(language)) : language;
            this.IsSourceLanguage = isSourceLanguage;
            this.Strings = new Dictionary<string, StringResource>(StringComparer.Ordinal);
        }

        public uint Read(string fileName, XmlReader reader)
        {
            uint count = 0;

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (!reader.ReadToFollowing(ResourcesElementName) || reader.IsEmptyElement)
                return count; // Not a resources file or an empty resources file

            if (!reader.Read())
                throw new ArgumentException("Reader ended unexpectedly");

            // This is a resources file - read the resources
            List<string> commentLines = null;
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType == XmlNodeType.Comment)
                {
                    if (commentLines == null)
                        commentLines = new List<string>();

                    commentLines.Add(reader.Value);
                    reader.Skip();
                    continue;
                }

                ResourceType resourceType = ResourceType.Other;
                if (reader.NodeType == XmlNodeType.Element)
                    resourceType = StringResource.GetResourceType(reader.LocalName);

                if (resourceType == ResourceType.Other)
                {
                    // We don't care about this XML - skip it
                    if (commentLines != null)
                        commentLines.Clear(); // We don't care about comments that are not before strings

                    reader.Skip();
                    continue;
                }

                // This is a string resource
                var stringResource = new StringResource(resourceType, reader)
                {
                    FileName = fileName
                };
                if ((commentLines != null) && (commentLines.Count > 0))
                {
                    if (this.IsSourceLanguage)
                    {
                        // If this is a string resource in the source language, save the comments
                        stringResource.CommentLines = new List<string>(commentLines);
                    }
                    else
                    {
                        // This is a string resource in a translated language
                        // See if we can get the source string for this string from the last comment
                        stringResource.TrySetSourceFromComment(commentLines[commentLines.Count - 1]);
                    }
                    commentLines.Clear();
                }

                ++count;
                this.Strings.Add(stringResource.Name, stringResource);
            }

            reader.ReadEndElement();

            return count;
        }

        public const string ResourcesElementName = "resources";

        public readonly string Language;
        public readonly bool IsSourceLanguage;
        public readonly Dictionary<string, StringResource> Strings;
    }
}
