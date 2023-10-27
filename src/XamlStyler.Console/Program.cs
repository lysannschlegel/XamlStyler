// © Xavalon. All rights reserved.

using CommandLine;
using System;
using System.IO;

namespace Xavalon.XamlStyler.Console
{
    public sealed partial class Program
    {
        public static int Main(string[] args)
        {
            var writer = new StringWriter();
            var parser = new CommandLine.Parser(_ => _.HelpWriter = writer);
            ParserResult<CommandLineOptions> result = parser.ParseArguments<CommandLineOptions>(args);

            result.WithNotParsed(_ =>
            {
                System.Console.Error.WriteLine(writer.ToString());
                Environment.Exit(1);
            })
            .WithParsed(options =>
            {
                Logger logger = new Logger(options.WriteToStdout ? System.Console.Error : System.Console.Out, options.LogLevel);

                ProcessType processType;
                if (!CheckOptions(options, logger, out processType))
                {
                    Environment.Exit(1);
                }

                var xamlStylerConsole = new XamlStylerConsole(options, logger);
                xamlStylerConsole.Process(processType);
            });

            return 0;
        }

        private static bool CheckOptions(CommandLineOptions options, Logger logger, out ProcessType processType)
        {
            logger.Log($"File Parameter: '{options.File}'", LogLevel.Debug);
            logger.Log($"File Count: {options.File?.Count ?? -1}", LogLevel.Debug);
            logger.Log($"File Directory: '{options.Directory}'", LogLevel.Debug);
            logger.Log($"Stdin?: '{options.ReadFromStdin}'", LogLevel.Debug);

            bool result = true;

            int numFilesSpecified = options.File?.Count ?? 0;
            bool isFileOptionSpecified = numFilesSpecified != 0;
            bool isDirectoryOptionSpecified = !String.IsNullOrEmpty(options.Directory);
            if (!(isFileOptionSpecified ^ isDirectoryOptionSpecified ^ options.ReadFromStdin))
            {
                System.Console.Error.WriteLine($"\nError: Must specify exactly one --file, --directory, or --read-from-stdin\n");
                result = false;
            }

            if (options.WriteToStdout && (isDirectoryOptionSpecified || (numFilesSpecified != 1 && !options.ReadFromStdin)))
            {
                System.Console.Error.WriteLine($"\nError: When using --write-to-stdout you must specify exactly one file or --read-from-stdin\n");
                result = false;
            }

            if (options.WriteToStdout && options.IsPassive)
            {
                System.Console.Error.WriteLine($"\nError: Cannot specify both --passive and --write-to-stdout\n");
                result = false;
            }

            if (options.ReadFromStdin && !options.WriteToStdout)
            {
                System.Console.Error.WriteLine($"\nError: --read-from-stdin requires --write-to-stdout\n");
                result = false;
            }

            processType = isFileOptionSpecified ? ProcessType.File : (isDirectoryOptionSpecified ? ProcessType.Directory : ProcessType.Stdin);
            return result;
        }
    }
}
