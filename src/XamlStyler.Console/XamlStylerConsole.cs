// © Xavalon. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Xavalon.XamlStyler.Options;

namespace Xavalon.XamlStyler.Console
{
    public sealed class XamlStylerConsole
    {
        private readonly CommandLineOptions options;
        private readonly Logger logger;
        private readonly StylerService stylerService;

        public XamlStylerConsole(CommandLineOptions options, Logger logger)
        {
            this.options = options;
            this.logger = logger;

            IStylerOptions stylerOptions = new StylerOptions();

            if (this.options.Configuration != null)
            {
                stylerOptions = LoadConfiguration(this.options.Configuration, logger);
            }

            this.ApplyOptionOverrides(options, stylerOptions);

            this.stylerService = new StylerService(stylerOptions, new XamlLanguageOptions
            {
                IsFormatable = true
            });
        }

        private void ApplyOptionOverrides(CommandLineOptions options, IStylerOptions stylerOptions)
        {
            if (options.IndentSize != null)
            {
                stylerOptions.IndentSize = options.IndentSize.Value;
            }

            if (options.IndentWithTabs != null)
            {
                stylerOptions.IndentWithTabs = options.IndentWithTabs.Value;
            }

            if (options.AttributesTolerance != null)
            {
                stylerOptions.AttributesTolerance = options.AttributesTolerance.Value;
            }

            if (options.KeepFirstAttributeOnSameLine != null)
            {
                stylerOptions.KeepFirstAttributeOnSameLine = options.KeepFirstAttributeOnSameLine.Value;
            }

            if (options.MaxAttributeCharactersPerLine != null)
            {
                stylerOptions.MaxAttributeCharactersPerLine = options.MaxAttributeCharactersPerLine.Value;
            }

            if (options.MaxAttributesPerLine != null)
            {
                stylerOptions.MaxAttributesPerLine = options.MaxAttributesPerLine.Value;
            }

            if (options.NoNewLineElements != null)
            {
                stylerOptions.NoNewLineElements = options.NoNewLineElements;
            }

            if (options.PutAttributeOrderRuleGroupsOnSeparateLines != null)
            {
                stylerOptions.PutAttributeOrderRuleGroupsOnSeparateLines = options.PutAttributeOrderRuleGroupsOnSeparateLines.Value;
            }

            if (options.AttributeIndentation != null)
            {
                stylerOptions.AttributeIndentation = options.AttributeIndentation.Value;
            }

            if (options.AttributeIndentationStyle != null)
            {
                stylerOptions.AttributeIndentationStyle = options.AttributeIndentationStyle.Value;
            }

            if (options.RemoveDesignTimeReferences != null)
            {
                stylerOptions.RemoveDesignTimeReferences = options.RemoveDesignTimeReferences.Value;
            }

            if (options.EnableAttributeReordering != null)
            {
                stylerOptions.EnableAttributeReordering = options.EnableAttributeReordering.Value;
            }

            if (options.FirstLineAttributes != null)
            {
                stylerOptions.FirstLineAttributes = options.FirstLineAttributes;
            }

            if (options.OrderAttributesByName != null)
            {
                stylerOptions.OrderAttributesByName = options.OrderAttributesByName.Value;
            }

            if (options.PutEndingBracketOnNewLine != null)
            {
                stylerOptions.PutEndingBracketOnNewLine = options.PutEndingBracketOnNewLine.Value;
            }

            if (options.RemoveEndingTagOfEmptyElement != null)
            {
                stylerOptions.RemoveEndingTagOfEmptyElement = options.RemoveEndingTagOfEmptyElement.Value;
            }

            if (options.RootElementLineBreakRule != null)
            {
                stylerOptions.RootElementLineBreakRule = options.RootElementLineBreakRule.Value;
            }

            if (options.ReorderVSM != null)
            {
                stylerOptions.ReorderVSM = options.ReorderVSM.Value;
            }

            if (options.ReorderGridChildren != null)
            {
                stylerOptions.ReorderGridChildren = options.ReorderGridChildren.Value;
            }

            if (options.ReorderCanvasChildren != null)
            {
                stylerOptions.ReorderCanvasChildren = options.ReorderCanvasChildren.Value;
            }

            if (options.ReorderSetters != null)
            {
                stylerOptions.ReorderSetters = options.ReorderSetters.Value;
            }

            if (options.FormatMarkupExtension != null)
            {
                stylerOptions.FormatMarkupExtension = options.FormatMarkupExtension.Value;
            }

            if (options.NoNewLineMarkupExtensions != null)
            {
                stylerOptions.NoNewLineMarkupExtensions = options.NoNewLineMarkupExtensions;
            }

            if (options.ThicknessStyle != null)
            {
                stylerOptions.ThicknessStyle = options.ThicknessStyle.Value;
            }

            if (options.ThicknessAttributes != null)
            {
                stylerOptions.ThicknessAttributes = options.ThicknessAttributes;
            }

            if (options.CommentSpaces != null)
            {
                stylerOptions.CommentSpaces = options.CommentSpaces.Value;
            }
        }

        public void Process(ProcessType processType)
        {
            int successCount = 0;
            IList<XamlFile> files;

            switch (processType)
            {
                case ProcessType.File:
                    files = CreateXamlFiles(this.options.File);
                    break;
                case ProcessType.Directory:
                    SearchOption searchOption = this.options.IsRecursive
                        ? SearchOption.AllDirectories
                        : SearchOption.TopDirectoryOnly;
                    IList<string> fileNames = File.GetAttributes(this.options.Directory).HasFlag(FileAttributes.Directory)
                        ? Directory.GetFiles(this.options.Directory, "*.xaml", searchOption).ToList()
                        : new List<string>();
                    files = CreateXamlFiles(fileNames);
                    break;
                default:
                    throw new ArgumentException("Invalid ProcessType");
            }

            foreach (XamlFile file in files)
            {
                if (file.TryProcess(this.options, this.logger, this.stylerService))
                {
                    successCount++;
                }
            }

            if (this.options.IsPassive)
            {
                this.logger.Log($"\n{successCount} of {files.Count} files pass format check.", LogLevel.Minimal);

                if (successCount != files.Count)
                {
                    Environment.Exit(1);
                }
            }
            else
            {
                this.logger.Log($"\nProcessed {successCount} of {files.Count} files.", LogLevel.Minimal);
            }
        }

        private IList<XamlFile> CreateXamlFiles(IEnumerable<string> fileNames)
        {
            return fileNames.Select(fileName => new XamlFileByName(fileName)).ToList<XamlFile>();
        }

        public static IStylerOptions LoadConfiguration(string path, Logger logger)
        {
            var stylerOptions = new StylerOptions(path);
            logger.Log(JsonConvert.SerializeObject(stylerOptions), LogLevel.Insanity);
            logger.Log(JsonConvert.SerializeObject(stylerOptions.AttributeOrderingRuleGroups), LogLevel.Debug);
            return stylerOptions;
        }

        public static string GetConfigurationFromPath(string path, Logger logger)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(path))
                {
                    return null;
                }

                bool isSolutionRoot = false;

                while (!isSolutionRoot && ((path = Path.GetDirectoryName(path)) != null))
                {
                    isSolutionRoot = Directory.Exists(Path.Combine(path, ".vs"));
                    logger.Log($"In solution root: {isSolutionRoot}", LogLevel.Debug);
                    string configFile = Path.Combine(path, "Settings.XamlStyler");
                    logger.Log($"Looking in: {path}", LogLevel.Debug);

                    if (File.Exists(configFile))
                    {
                        logger.Log($"Configuration Found: {configFile}", LogLevel.Verbose);
                        return configFile;
                    }
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
