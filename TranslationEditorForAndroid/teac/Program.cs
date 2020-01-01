using Com.MeraBills.StringResourceReaderWriter;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace teac
{
    class Program
    {
        static void Main(string[] args)
        {
            var rootCommand = new RootCommand(description: "Translactions Editor for Android (Console)");
            rootCommand.TreatUnmatchedTokensAsErrors = true;

            {
                var exportCommand = new Command("excel-export");
                exportCommand.AddAlias("ee");
                exportCommand.Description = "Export source language strings and their target language translations to an Excel file";
                exportCommand.TreatUnmatchedTokensAsErrors = true;

                var fileArgument = new Argument<FileInfo>("output-file");
                fileArgument.Description = "Path to output Excel file";
                fileArgument.Arity = ArgumentArity.ZeroOrOne;

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

                var fileArgument = new Argument<FileInfo>("input-file");
                fileArgument.Description = "Path to input Excel file";
                fileArgument.Arity = ArgumentArity.ExactlyOne;

                importCommand.AddArgument(CreateLanguageCodeArgument("source-language"));
                importCommand.AddArgument(CreateLanguageCodeArgument("target-language"));
                importCommand.AddArgument(fileArgument);
                importCommand.Handler = CommandHandler.Create<string, string, FileInfo>(ExcelImport);

                rootCommand.AddCommand(importCommand);
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

            DirectoryInfo sourceLanguageDirectory;
            DirectoryInfo targetLanguageDirectory;
            if (!FindStringResourceDirectories(sourceLanguage, targetLanguage, out sourceLanguageDirectory, out targetLanguageDirectory))
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
        }

        private static void ExcelImport(string sourceLanguage, string targetLanguage, FileInfo inputFile)
        {
            throw new NotImplementedException();
        }

        private static StringResources ParseDirectory(string language, bool isSourceLanguage, DirectoryInfo languageDirectory)
        {
            StringResources stringResources = new StringResources(language, isSourceLanguage: true);
            var readerSettings = new XmlReaderSettings()
            {
                Async = false,
                CloseInput = true,
                IgnoreWhitespace = true
            };
            uint totalFiles = 0;
            uint errors = 0;
            uint totalStrings = 0;
            Console.WriteLine("\nParsing XML files in {0:s} language directory ...", isSourceLanguage ? "source" : "target");
            foreach (var xmlFile in languageDirectory.GetFiles("*.xml"))
            {
                ++totalFiles;
                Console.Write("  {0:s} ... ", xmlFile.Name);
                try
                {
                    using var reader = XmlReader.Create(new StreamReader(xmlFile.FullName), readerSettings);

                    int before = stringResources.Strings.Count;
                    uint stringCount = stringResources.Read(xmlFile.Name, reader);
                    int after = stringResources.Strings.Count;

                    if ((after - before) != stringCount)
                    {
                        Console.WriteLine("CRITICAL ERROR - this file has strings that are also in previously parsed files! Aborting ...\n");
                        return null;
                    }

                    totalStrings += stringCount;
                    Console.WriteLine("Success - {0:d} string resources parsed", stringCount);
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
            var languageCodeArgument = new Argument<string>(name);
            languageCodeArgument.Description = "A two-letter ISO 639-1 language code ('en' or 'hi', for example)";
            languageCodeArgument.Arity = ArgumentArity.ExactlyOne;
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
                    string language = match.Groups[1].Value;
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
                Console.WriteLine("\nERROR: values directory for language {0:s} not found", sourceLanguage);
                error = true;
            }
            if (targetLanguageDirectory == null)
            {
                Console.WriteLine("\nERROR: values directory for language {0:s} not found", targetLanguage);
                error = true;
            }
            if (error)
                return false;

            Console.WriteLine("Source langauage directory: {0:s}", sourceLanguageDirectory.FullName);
            Console.WriteLine("Target langauage directory: {0:s}", targetLanguageDirectory.FullName);
            return true;
        }

        private const string OutputFileNameTemplate = "{0:s}-to-{1:s}.xlsx";

        private static readonly Regex ValuesDirectoryNameRegex = new Regex(
            @"^values(?:-mcc\d+(?:-mnc\d+)?)?-((?:([a-z]{2})(?:-[A-z]+)?)|(?:b\+([a-z]{2})(?:\+[\w\d]+)?))(?:-[\w\d-]+)?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
    }
}
