// © Xavalon. All rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xavalon.XamlStyler.Options;

namespace Xavalon.XamlStyler.Console
{
    /// Represents a XAML file to process.
    /// This can either be an actual file (XamlFileByName), or the stdin stream (XamlFileFromStdin).
    public abstract class XamlFile
    {
        public abstract string FileName { get; }
        public abstract string FullPath { get; }

        protected abstract bool CheckCanBeProcessed(Logger logger);

        protected abstract StylerService GetStylerService(CommandLineOptions options, Logger logger, StylerService defaultStyler);

        protected abstract StreamReader CreateReader(Logger logger);

        protected abstract StreamWriter CreateWriter(Encoding encoding);

        public bool TryProcess(CommandLineOptions options, Logger logger, StylerService defaultStyler)
        {
            try
            {
                return Process(options, logger, defaultStyler);
            }
            catch (Exception e)
            {
                logger.Log("Skipping... Error formatting XAML. Increase log level for more details.");
                logger.Log($"Exception: {e.Message}", LogLevel.Verbose);
                logger.Log($"StackTrace: {e.StackTrace}", LogLevel.Debug);
                return false;
            }
        }

        private bool Process(CommandLineOptions options, Logger logger, StylerService defaultStyler)
        {
            logger.Log($"{(options.IsPassive ? "Checking" : "Processing")}: {this.FileName}");

            if (!options.Ignore && !this.CheckCanBeProcessed(logger))
            {
                return false;
            }

            var (originalContent, encoding) = ReadOriginalContent(logger);

            StylerService styler = this.GetStylerService(options, logger, defaultStyler);
            string formattedOutput = styler.StyleDocument(originalContent);

            if (options.IsPassive)
            {
                return ReportFormattedStatus(originalContent, formattedOutput, logger);
            }
            else
            {
                logger.Log($"\nFormatted Output:\n\n{formattedOutput}\n", LogLevel.Insanity);
                if (options.WriteToStdout)
                {
                    WriteFormattedToStdout(formattedOutput, encoding);
                }
                else
                {
                    ApplyFormattingToFile(originalContent, formattedOutput, encoding, logger);
                }
                return true;
            }
        }

        private (string, Encoding) ReadOriginalContent(Logger logger)
        {
            using (var reader = this.CreateReader(logger))
            {
                string originalContent = reader.ReadToEnd();
                Encoding encoding = reader.CurrentEncoding;
                logger.Log($"\nOriginal Content:\n\n{originalContent}\n", LogLevel.Insanity);
                return (originalContent, encoding);
            }
        }

        private bool ReportFormattedStatus(string originalContent, string formattedOutput, Logger logger)
        {
            if (formattedOutput.Equals(originalContent, StringComparison.Ordinal))
            {
                logger.Log($"  PASS");
                return true;
            }
            else
            {
                logger.Log($"  FAIL");
                // Fail fast in passive mode when detecting a file where formatting rules were not followed.
                return false;
            }
        }

        private void WriteFormattedToStdout(string formattedOutput, Encoding encoding)
        {
            var prevEncoding = System.Console.OutputEncoding;
            try
            {
                System.Console.OutputEncoding = encoding;
                System.Console.Out.Write(encoding.GetString(encoding.GetPreamble()));
                System.Console.Out.Write(formattedOutput);
            }
            finally
            {
                System.Console.OutputEncoding = prevEncoding;
            }
        }

        private void ApplyFormattingToFile(string originalContent, string formattedOutput, Encoding encoding, Logger logger)
        {
            // Only modify the file on disk if the content would be changed
            if (!formattedOutput.Equals(originalContent, StringComparison.Ordinal))
            {
                using var writer = this.CreateWriter(encoding);
                writer.Write(formattedOutput);
                logger.Log($"Finished Processing: {this.FileName}", LogLevel.Verbose);
            }
            else
            {
                logger.Log($"Finished Processing (unmodified): {this.FileName}", LogLevel.Verbose);
            }
        }
    }

    public sealed class XamlFileByName : XamlFile
    {
        private readonly string fileName;
        public override string FileName { get { return this.fileName; } }

        private readonly string fullPath;
        public override string FullPath { get { return this.fullPath; } }

        public XamlFileByName(string fileName)
        {
            this.fileName = fileName;
            this.fullPath = Path.GetFullPath(fileName);
        }

        protected override bool CheckCanBeProcessed(Logger logger)
        {
            string extension = Path.GetExtension(this.FileName);
            logger.Log($"Extension: {extension}", LogLevel.Debug);
            if (!extension.Equals(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                logger.Log($"Skipping... Can only process XAML files. Use the --ignore parameter to override.");
                return false;
            }
            return true;
        }

        protected override StylerService GetStylerService(CommandLineOptions options, Logger logger, StylerService defaultStyler)
        {
            // If the options already has a configuration file set, we don't need to go hunting for one
            string configurationPath = String.IsNullOrEmpty(options.Configuration) ? XamlStylerConsole.GetConfigurationFromPath(this.FullPath, logger) : null;

            return String.IsNullOrWhiteSpace(configurationPath) ? defaultStyler : new StylerService(XamlStylerConsole.LoadConfiguration(configurationPath, logger), new XamlLanguageOptions()
            {
                IsFormatable = true
            });
        }

        protected override StreamReader CreateReader(Logger logger)
        {
            logger.Log($"Full Path: {this.FullPath}", LogLevel.Debug);
            return new StreamReader(this.FullPath);
        }

        protected override StreamWriter CreateWriter(Encoding encoding)
        {
            return new StreamWriter(this.FullPath, false, encoding);
        }
    }

    public sealed class XamlFileFromStdin : XamlFile
    {
        public override string FileName { get; } = "<stdin>";

        public override string FullPath { get; } = "<stdin>";

        protected override bool CheckCanBeProcessed(Logger logger)
        {
            return true;
        }

        protected override StylerService GetStylerService(CommandLineOptions options, Logger logger, StylerService defaultStyler)
        {
            return defaultStyler;
        }

        protected override StreamReader CreateReader(Logger logger)
        {
            var stream = System.Console.OpenStandardInput();
            return new StreamReader(stream);
        }

        protected override StreamWriter CreateWriter(Encoding encoding)
        {
            throw new InvalidOperationException("Cannot write to file when reading from stdin");
        }
    }
}
