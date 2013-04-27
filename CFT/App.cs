using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CLAP;

namespace BlogTalkRadio.Tools.CFT
{
    public class App
    {
        [Verb(IsDefault = true, Description = "Transforms *.config files found recursively in a folder")]
        public static void Transform(
            string path = null,
            [Description("Configuration Name (eg. Dev, Production)")]string configuration = null,
            string destination = null,
            [Description("A file to get Configuration name from")]string configurationFile = null,
            [Description("An environment variable to get Configuration name from")]string configurationEnv = null,
            [Description("Default Configuration name to use when not obtained from other source")]string configurationDefault = null,
            [Description("Stops with an error if a file would change. Doesn't touch any file")]bool dry = false
            )
        {
            try
            {
                var directoryProcessor = new DirectoryProcessor(path, destination);
                if (string.IsNullOrWhiteSpace(configuration))
                {
                    string choice;
                    configuration = new ConfigurationNameResolver
                        {
                            Filename = configurationFile,
                            EnvironmentVariable = configurationEnv,
                            Default = configurationDefault
                        }.GetConfigurationName(out choice);
                    if (!string.IsNullOrWhiteSpace(configuration))
                    {
                        Trace.TraceInformation(string.Format("Using {0} configuration obtained from {1}", configuration, choice));
                    }
                }
                if (string.IsNullOrWhiteSpace(configuration))
                {
                    directoryProcessor.CreateEmptyDestinationFiles(dry);
                }
                else
                {
                    directoryProcessor.PerformTransformations(configuration, dry);
                }
            }
            catch
            {
                Environment.ExitCode = 1;
                throw;
            }
        }

        [Verb(Description = "Creates missing *.config files empty (without applying any transformation)")]
        public static void CreateMissing(
            string path = null,
            string destination = null,
            [Description("Stops with an error if a file would change. Doesn't touch any file")]bool dry = false
            )
        {
            try
            {
                var directoryProcessor = new DirectoryProcessor(path, destination);
                directoryProcessor.CreateEmptyDestinationFiles(dry);
            }
            catch
            {
                Environment.ExitCode = 1;
                throw;
            }
        }

        [Empty, Help]
        public static void Help(string help)
        {
            // this is an empty handler that prints
            // the automatic help string to the console.

            Console.WriteLine(help);
        }
    }
}
