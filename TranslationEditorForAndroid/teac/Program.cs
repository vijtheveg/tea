using Com.MeraBills.StringResourceReaderWriter;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;
using System.Xml;

namespace teac
{
    class Program
    {
        static void Main(string[] args)
        {
            var rootCommand = new RootCommand(description: "Translactions Editor for Android (Console)")
            {
                TreatUnmatchedTokensAsErrors = true
            };

            {
                var exportCommand = new Command("excel-export");
                exportCommand.AddAlias("ee");
                exportCommand.Description = "Export source language strings and their target language translations to an Excel file";
                exportCommand.TreatUnmatchedTokensAsErrors = true;

                var fileArgument = new Argument<FileInfo>("output-file")
                {
                    Description = "Path to output Excel file",
                    Arity = ArgumentArity.ZeroOrOne
                };

                exportCommand.AddArgument(CreateLanguageCodeArgument("source-language"));
                exportCommand.AddArgument(CreateLanguageCodeArgument("target-language"));
                exportCommand.AddArgument(fileArgument);
                exportCommand.Handler = CommandHandler.Create<string, string, FileInfo>(ExcelExport);

                rootCommand.AddCommand(exportCommand);
            }

            {
                var importCommand = new Command("excel-import");
                importCommand.AddAlias("ei");
                importCommand.Description = "Import target language translations of source language strings from an Excel file";
                importCommand.TreatUnmatchedTokensAsErrors = true;

                var fileArgument = new Argument<FileInfo>("input-file")
                {
                    Description = "Path to input Excel file",
                    Arity = ArgumentArity.ExactlyOne
                };

                importCommand.AddArgument(CreateLanguageCodeArgument("source-language"));
                importCommand.AddArgument(CreateLanguageCodeArgument("target-language"));
                importCommand.AddArgument(fileArgument);
                importCommand.Handler = CommandHandler.Create<string, string, FileInfo>(ExcelImport);

                rootCommand.AddCommand(importCommand);
            }

            {
                var updateCommand = new Command("update-target");
                updateCommand.AddAlias("ut");
                updateCommand.Description = "Update final status of target language translations and delete extra translations";
                updateCommand.TreatUnmatchedTokensAsErrors = true;

                updateCommand.AddArgument(CreateLanguageCodeArgument("source-language"));
                updateCommand.AddArgument(CreateLanguageCodeArgument("target-language"));
                updateCommand.Handler = CommandHandler.Create<string, string>(UpdateTarget);

                rootCommand.AddCommand(updateCommand);
            }

            rootCommand.Invoke(args);
        }

        private static void ExcelExport(string sourceLanguage, string targetLanguage, FileInfo outputFile)
        {
            Console.WriteLine();

            outputFile ??= new FileInfo(string.Format(OutputFileNameTemplate, sourceLanguage, targetLanguage));
            Console.WriteLine("Source language code: {0:s}", sourceLanguage);
            Console.WriteLine("Target language code: {0:s}", targetLanguage);
            Console.WriteLine("Output file: {0:s}", outputFile.FullName);

            if (!FindStringResourceDirectories(sourceLanguage, targetLanguage, out DirectoryInfo sourceLanguageDirectory, out DirectoryInfo targetLanguageDirectory))
                return;

            DirectoryInfo outputFileDirectory = outputFile.Directory;
            if (!outputFileDirectory.Exists)
            {
                Console.WriteLine("\nERROR: Output directory {0:s} does not exist", outputFileDirectory.FullName);
                return;
            }

            StringResources sourceStrings = ParseDirectory(sourceLanguage, true, sourceLanguageDirectory);
            if (sourceStrings == null)
                return; // Something went wrong

            StringResources targetStrings = ParseDirectory(targetLanguage, false, targetLanguageDirectory);
            if (targetStrings == null)
                return; // Something went wrong

            Console.WriteLine("Writing output file ... ");
            try
            {
                ExcelReaderWriter.Write(sourceStrings, targetStrings, outputFile);
                Console.WriteLine("Done!\n");
            }
            catch
            {
                Console.WriteLine("ERROR: Could not create output file. Make sure you have write access and that the file is not open in Excel.");
            }
        }

        private static void ExcelImport(string sourceLanguage, string targetLanguage, FileInfo inputFile)
        {
            Console.WriteLine();

            Console.WriteLine("Source language code: {0:s}", sourceLanguage);
            Console.WriteLine("Target language code: {0:s}", targetLanguage);
            Console.WriteLine("Input file: {0:s}", inputFile.FullName);

            if (!FindStringResourceDirectories(sourceLanguage, targetLanguage, out DirectoryInfo sourceLanguageDirectory, out DirectoryInfo targetLanguageDirectory))
                return;

            if (!inputFile.Exists)
            {
                Console.WriteLine("\nERROR: The input file {0:s} does not exist", inputFile.FullName);
                return;
            }

            StringResources sourceStrings = ParseDirectory(sourceLanguage, true, sourceLanguageDirectory);
            if (sourceStrings == null)
                return; // Something went wrong

            StringResources targetStringsFromXml = ParseDirectory(targetLanguage, false, targetLanguageDirectory);
            if (targetStringsFromXml == null)
                return; // Something went wrong

            StringResources targetStringsFromExcel;
            try
            {
                targetStringsFromExcel = ExcelReaderWriter.Read(sourceLanguage, targetLanguage, inputFile);
            }
            catch (ArgumentException)
            {
                Console.WriteLine("ERROR: The input file does not seem to contain translations");
                return;
            }
            catch(InvalidDataException ide)
            {
                Console.WriteLine("ERROR: The data in the input file is not valid.\n  {0:s}", ide.Message);
                return;
            }
            catch
            {
                Console.WriteLine("ERROR: The input file could not be parsed. Are you sure it is an Excel file?");
                return;
            }

            // Merge the strings from the resource files and Excel file
            var targetStrings = Merge(sourceStrings, targetStringsFromXml, targetStringsFromExcel, out var mergeStatistics);

            Console.WriteLine("Source directory: {0:d} strings = {1:d} translatable + {2:d} untranslatable + {3:d} empty",
                sourceStrings.Strings.Count, sourceStrings.Strings.Count - mergeStatistics.UntranslatableSources - mergeStatistics.EmptySources,
                mergeStatistics.UntranslatableSources, mergeStatistics.EmptySources);

            Console.WriteLine("Target directory: {0:d} translations final, {1:d} not final, {2:d} missing, {3:d} extra\n",
                mergeStatistics.FinalTargets, mergeStatistics.NonFinalTargets, mergeStatistics.MissingTargets, mergeStatistics.ExtraTargets);

            Console.WriteLine("Recreating target directory ... ");
            try
            {
                if (RecreateTargetDirectory(targetLanguageDirectory, targetStrings))
                    Console.WriteLine("Done!\n");
            }
            catch
            {
                Console.WriteLine("ERROR: Could not recreate the resource files in the target directory");
            }
        }

        private static void UpdateTarget(string sourceLanguage, string targetLanguage)
        {
            Console.WriteLine();

            Console.WriteLine("Source language code: {0:s}", sourceLanguage);
            Console.WriteLine("Target language code: {0:s}", targetLanguage);

            if (!FindStringResourceDirectories(sourceLanguage, targetLanguage, out DirectoryInfo sourceLanguageDirectory, out DirectoryInfo targetLanguageDirectory))
                return;

            StringResources sourceStrings = ParseDirectory(sourceLanguage, true, sourceLanguageDirectory);
            if (sourceStrings == null)
                return; // Something went wrong

            StringResources targetStringsFromXml = ParseDirectory(targetLanguage, false, targetLanguageDirectory);
            if (targetStringsFromXml == null)
                return; // Something went wrong

            // Merge the strings from the resource files
            var targetStrings = Merge(sourceStrings, targetStringsFromXml, null, out var mergeStatistics);

            Console.WriteLine("Source directory: {0:d} strings = {1:d} translatable + {2:d} untranslatable + {3:d} empty",
                sourceStrings.Strings.Count, sourceStrings.Strings.Count - mergeStatistics.UntranslatableSources - mergeStatistics.EmptySources,
                mergeStatistics.UntranslatableSources, mergeStatistics.EmptySources);

            Console.WriteLine("Target directory: {0:d} translations final, {1:d} not final, {2:d} missing, {3:d} extra\n",
                mergeStatistics.FinalTargets, mergeStatistics.NonFinalTargets, mergeStatistics.MissingTargets, mergeStatistics.ExtraTargets);

            Console.WriteLine("Recreating target directory ... ");
            try
            {
                if (RecreateTargetDirectory(targetLanguageDirectory, targetStrings))
                    Console.WriteLine("Done!\n");
            }
            catch
            {
                Console.WriteLine("ERROR: Could not recreate the resource files in the target directory");
            }
        }

        private static bool RecreateTargetDirectory(DirectoryInfo targetLanguageDirectory, StringResources targetStrings)
        {
            int count = 0;
            Console.WriteLine("Deleting all resource files in the target directory {0:s} ...", targetLanguageDirectory.FullName);
            foreach (var xmlFile in targetLanguageDirectory.GetFiles("*.xml"))
            {
                try
                {
                    xmlFile.Delete();
                    ++count;
                }
                catch(IOException)
                {
                    Console.WriteLine("ERROR: File {0:s} could not be deleted - it is most probably open in another application", xmlFile.Name);
                    return false;
                }
                catch(SecurityException)
                {
                    Console.WriteLine("ERROR: File {0:s} could not be deleted - you do not have the permissions required to delete it", xmlFile.Name);
                    return false;
                }
            }
            Console.WriteLine("{0:d} resource files deleted", count);

            Console.WriteLine("Recreating resource files in the target directory {0:s} ...", targetLanguageDirectory.FullName);
            var writerSettings = new XmlWriterSettings()
            { 
                Async = false,
                CloseOutput = true,
                Indent = true,
                IndentChars = "    ",
                NewLineOnAttributes = false,
                OmitXmlDeclaration = false
            };

            uint totalFiles = 0;
            uint totalStrings = 0;
            var xmlWriters = new Dictionary<string, XmlWriter>(StringComparer.Ordinal);
            try
            {
                // Write each target string to its corresponding resource file
                foreach(var targetString in targetStrings.Strings.Values)
                {
                    if (!targetString.HasNonEmptyContent)
                        continue;

                    if (!xmlWriters.TryGetValue(targetString.FileName, out var xmlWriter))
                    {
                        // This is the first string in this resource file
                        StreamWriter outputStream = null;
                        XmlWriter temp = null;
                        try
                        {
                            // Create the resource file
                            outputStream = new StreamWriter(Path.Combine(targetLanguageDirectory.FullName, targetString.FileName));
                            temp = XmlWriter.Create(outputStream, writerSettings);
                            xmlWriters.Add(targetString.FileName, temp);
                            xmlWriter = temp;
                            outputStream = null;
                            temp = null;

                            // Write the resources start element
                            xmlWriter.WriteStartElement(StringResources.ResourcesElementName);
                            ++totalFiles;
                        }
                        finally
                        {
                            if (outputStream != null)
                                outputStream.Close();

                            if (temp != null)
                                temp.Close();
                        }
                    }

                    targetString.Write(xmlWriter);
                    ++totalStrings;
                }

                // Write the resources end element in each resource file
                foreach (var xmlWriter in xmlWriters.Values)
                    xmlWriter.WriteEndElement();
            }
            catch
            {
                Console.WriteLine("ERROR: One or more resource files could not be created");
                return false;
            }
            finally
            {
                foreach (var xmlWriter in xmlWriters.Values)
                    if (xmlWriter != null)
                        xmlWriter.Close();
            }

            Console.WriteLine("Created {0:d} resource files with a total of {1:d} string resources.\n", totalFiles, totalStrings);
            return true;
        }

        private static StringResources Merge(
            StringResources sourceStrings, StringResources targetStringsFromXml, StringResources targetStringsFromExcel, out MergeStatistics mergeStatistics)
        {
            mergeStatistics = new MergeStatistics();
            var targetStrings = new StringResources(targetStringsFromXml.Language, isSourceLanguage: false);
            foreach(var sourceString in sourceStrings.Strings.Values)
            {
                if (!sourceString.IsTranslatable)
                {
                    ++mergeStatistics.UntranslatableSources;
                    continue; // A translation is not required
                }
                else if (!sourceString.HasNonEmptyContent)
                {
                    ++mergeStatistics.EmptySources;
                    continue; // A translation is not required
                }

                var targetStringFromXml = GetTargetString(sourceString, targetStringsFromXml);
                var targetStringFromExcel = (targetStringsFromExcel != null) ? GetTargetString(sourceString, targetStringsFromExcel) : null;

                if (targetStringFromXml != null)
                {
                    if (sourceString.Equals(targetStringFromXml.Source))
                        ++mergeStatistics.FinalTargets;
                    else
                        ++mergeStatistics.NonFinalTargets; // The translation in the XML files is out of date
                }
                else
                    ++mergeStatistics.MissingTargets; // A translation is not found in the XML files

                StringResource targetString;
                if (targetStringFromExcel == null)
                {
                    // Target string does not exist in Excel. Use the one from the XML, if any
                    targetString = targetStringFromXml ?? null;
                }
                else if (targetStringFromXml == null)
                {
                    // Target string exists in Excel, but not in XML
                    targetString = targetStringFromExcel;
                }
                else
                {
                    // Target string exists in both Excel and XML
                    if ((targetStringFromExcel.Source != null) == (targetStringFromXml.Source != null))
                    {
                        // Both strings are either final or not final - prefer the one from the Excel file
                        targetString = targetStringFromExcel;
                    }
                    else
                    {
                        // Only one of the translations is marked as final - keep the one that is marked as final
                        targetString = targetStringFromExcel.Source != null ? targetStringFromExcel : targetStringFromXml;
                    }
                }

                if (targetString != null)
                    targetStrings.Strings.Add(targetString.Name, targetString);
            }

            foreach(var targetStringFromXml in targetStringsFromXml.Strings.Values)
            {
                if (!sourceStrings.Strings.TryGetValue(targetStringFromXml.Name, out var sourceString) || (sourceString.ResourceType != targetStringFromXml.ResourceType))
                    ++mergeStatistics.ExtraTargets; // The target string doesn't have a source string of same resource type
            }

            return targetStrings;
        }

        private static StringResource GetTargetString(StringResource sourceString, StringResources targetStrings)
        {
            if (targetStrings.Strings.TryGetValue(sourceString.Name, out var targetString) && (sourceString.ResourceType == targetString.ResourceType))
            {
                targetString.FileName = sourceString.FileName;
                targetString.HasFormatSpecifiers = sourceString.HasFormatSpecifiers;
                targetString.IsTranslatable = true;
                if (!sourceString.Equals(targetString.Source))
                    targetString.Source = null; // The source string is no longer the same as the source of the translation
                targetString.CommentLines = null;
                return targetString;
            }

            // A translation is not found, or the resource type of the source is no longer the same as that of the target
            return null;
        }

        private static StringResources ParseDirectory(string language, bool isSourceLanguage, DirectoryInfo languageDirectory)
        {
            StringResources stringResources = new StringResources(language, isSourceLanguage);
            var readerSettings = new XmlReaderSettings()
            {
                Async = false,
                CloseInput = true,
                IgnoreWhitespace = true
            };
            uint totalFiles = 0;
            uint errors = 0;
            uint totalStrings = 0;
            Console.WriteLine("\nParsing resource files in {0:s} language directory ...", isSourceLanguage ? "source" : "target");
            foreach (var xmlFile in languageDirectory.GetFiles("*.xml"))
            {
                ++totalFiles;
                Console.Write("  {0:s} ... ", xmlFile.Name);
                try
                {
                    using var reader = XmlReader.Create(new StreamReader(xmlFile.FullName), readerSettings);
                    uint stringCount = stringResources.Read(xmlFile.Name, reader);
                    totalStrings += stringCount;
                    Console.WriteLine("Success - {0:d} string resources parsed", stringCount);
                }
                catch(DuplicateStringResourceException dsre)
                {
                    Console.WriteLine("CRITICAL ERROR - this file has a string (name=\"{0:s}\") that is also in a previously parsed file! Aborting ...\n", dsre.StringResourceName);
                    return null;
                }
                catch
                {
                    Console.WriteLine("ERROR - are you sure this is a resources file?");
                    ++errors;
                }
            }
            Console.WriteLine("Parsed {0:d} resource files with {1:d} string resources. {2:d} files had errors.\n", totalFiles, totalStrings, errors);

            return stringResources;
        }

        private static Argument<string> CreateLanguageCodeArgument(string name)
        {
            var languageCodeArgument = new Argument<string>(name)
            {
                Description = "A two-letter ISO 639-1 language code ('en' or 'hi', for example)",
                Arity = ArgumentArity.ExactlyOne
            };
            languageCodeArgument.AddValidator((argument) => {
                string value = argument.Token?.Value;
                if (string.IsNullOrEmpty(value) || value.Length != 2)
                    return "A two-letter language code ('en' or 'hi', for example) is required";

                return null; // No error
            });

            return languageCodeArgument;
        }

        private static bool FindStringResourceDirectories(string sourceLanguage, string targetLanguage, out DirectoryInfo sourceLanguageDirectory, out DirectoryInfo targetLanguageDirectory)
        {
            sourceLanguageDirectory = null;
            targetLanguageDirectory = null;

            var valuesSubdirectories = Directory.GetDirectories(Directory.GetCurrentDirectory(), "values*", SearchOption.TopDirectoryOnly);
            foreach(var valuesSubdirectory in valuesSubdirectories)
            {
                DirectoryInfo current = new DirectoryInfo(valuesSubdirectory);
                var match = ValuesDirectoryNameRegex.Match(current.Name);
                if (match.Success)
                {
                    // Get the language of the current values directory
                    string language = null;
                    foreach (Group group in match.Groups)
                        if (string.CompareOrdinal(group.Name, LanguageCodeSubexpressionName) == 0)
                        {
                            language = group.Value;
                            break;
                        }

                    if (string.Compare(sourceLanguage, language, StringComparison.OrdinalIgnoreCase) == 0)
                        sourceLanguageDirectory = current;
                    else if (string.Compare(targetLanguage, language, StringComparison.OrdinalIgnoreCase) == 0)
                        targetLanguageDirectory = current;
                    // else - some other values directory - we don't care about this directory
                }
                else
                {
                    if ((sourceLanguageDirectory == null) &&
                        (string.Compare(current.Name, "values", StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        // If we haven't found an exact match for the source language directory, assume it to be the "values" subdirectory
                        sourceLanguageDirectory = current;
                    }
                }
            }

            bool error = false;
            if (sourceLanguageDirectory == null)
            {
                Console.WriteLine("\nERROR: values directory for source language {0:s} not found", sourceLanguage);
                error = true;
            }
            if (targetLanguageDirectory == null)
            {
                Console.WriteLine("\nERROR: values directory for target language {0:s} not found. Please create it and then try again", targetLanguage);
                error = true;
            }
            if (error)
                return false;

            Console.WriteLine("Source langauage directory: {0:s}", sourceLanguageDirectory.FullName);
            Console.WriteLine("Target langauage directory: {0:s}", targetLanguageDirectory.FullName);
            return true;
        }

        private sealed class MergeStatistics
        {
            internal uint UntranslatableSources = 0;
            internal uint EmptySources = 0;
            internal uint FinalTargets = 0;
            internal uint NonFinalTargets = 0;
            internal uint MissingTargets = 0;
            internal uint ExtraTargets = 0;
        };

        private const string OutputFileNameTemplate = "{0:s}-to-{1:s}.xlsx";
        private const string LanguageCodeSubexpressionName = "lc";

        private static readonly Regex ValuesDirectoryNameRegex = new Regex(
            @"^values(?:-mcc\d+(?:-mnc\d+)?)?-(?:(?:(?<" + LanguageCodeSubexpressionName + @">[a-z]{2})(?:-[A-z]+)?)|(?:b\+(?<" + LanguageCodeSubexpressionName + @">[a-z]{2})(?:\+[\w\d]+)?))(?:-[\w\d-]+)?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
    }
}
