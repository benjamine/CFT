using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BlogTalkRadio.Tools.CFT
{
    public class DirectoryProcessor
    {
        public class DryRunFailedException : Exception
        {
            public string FilenameThatWouldChange { get; private set; }

            public DryRunFailedException(string filenameThatWouldChange)
                : base("Dry run failed. This file should change: " + filenameThatWouldChange)
            {
                FilenameThatWouldChange = filenameThatWouldChange;
            }
        }

        private readonly IEnumerable<IFileEnumerator> _fileEnumerators = new IFileEnumerator[] { new ClrFileEnumerator() };

        private readonly Regex _envTokenRegex = new Regex(@"\$env\:([a-z0-9\-_\.]+)\$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public DirectoryProcessor(string baseDirectory, string destinationDirectory)
        {
            BaseDirectory = baseDirectory;
            if (string.IsNullOrWhiteSpace(BaseDirectory))
            {
                BaseDirectory = ".";
            }

            if (!BaseDirectory.EndsWith("\\"))
                BaseDirectory += "\\";

            if (string.IsNullOrWhiteSpace(destinationDirectory))
                destinationDirectory = BaseDirectory;

            DestinationDirectory = destinationDirectory;

            if (!DestinationDirectory.EndsWith("\\"))
                DestinationDirectory += "\\";

        }

        public string BaseDirectory { get; private set; }

        public string DestinationDirectory { get; private set; }

        private string ConventionFileExtension
        {
            get
            {
                return string.Format(Conventions.Default.FilePattern, Conventions.Default.DefaultName).ToLowerInvariant();
            }
        }

        private IEnumerable<string> GetConfigFiles()
        {
            var fileEnumerators = _fileEnumerators.GetEnumerator();

            while (fileEnumerators.MoveNext())
            {
                try
                {
                    return fileEnumerators.Current.EnumerateFiles(BaseDirectory, "*" + ConventionFileExtension + "config");
                }
                catch (Exception ex)
                {
                    Trace.TraceInformation("Error while enumerating files using {0}, error message: {1}", fileEnumerators.Current.GetType(), ex.Message);
                }
            }

            throw new Exception("All the file enumerators failed to retrieve files");
        }

        public void PerformTransformations(string configurationName, bool dry = false)
        {
            Trace.TraceInformation("Starting Transformation Process at {0} with {1} configuration", BaseDirectory, configurationName);

            foreach (var originalFile in GetConfigFiles())
            {
                var sourceFile = originalFile;

                var destinationFile = originalFile.Replace(BaseDirectory, DestinationDirectory).Replace(ConventionFileExtension, ".");
                var destinationFileTemp = destinationFile + ".tmp";
                var transformations = new List<string>();

                if (configurationName.Contains(Conventions.Default.NameSeparator))
                {
                    StringBuilder transformationName = null;
                    foreach (string part in configurationName.Split(new[] { Conventions.Default.NameSeparator }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (transformationName == null)
                        {
                            transformationName = new StringBuilder(part);
                        }
                        else
                        {
                            transformationName.Append(Conventions.Default.NameSeparator);
                            transformationName.Append(part);
                        }
                        var transformFile = originalFile.Replace(ConventionFileExtension, "." + transformationName + ".");
                        if (PerformTransform(sourceFile, transformFile, destinationFileTemp))
                        {
                            transformations.Add(transformationName.ToString());
                        }
                        sourceFile = destinationFileTemp;
                    }
                }
                else
                {
                    var transformFile = originalFile.Replace(ConventionFileExtension, "." + configurationName + ".");
                    if (PerformTransform(sourceFile, transformFile, destinationFileTemp))
                    {
                        transformations.Add(configurationName);
                    }
                }

                ReplaceTokensInFile(destinationFileTemp, configurationName);

                var destinationFileRelative = Path.GetFullPath(destinationFile).Substring(Path.GetFullPath(DestinationDirectory).Length);
                if (!ReplaceWithTempIfChanged(destinationFileTemp, destinationFile, dry))
                {
                    Trace.TraceInformation("Unchanged: {0}.", destinationFileRelative);
                }
                else
                {
                    if (transformations.Count < 1)
                    {
                        transformations.Add(Conventions.Default.DefaultName);
                    }
                    Trace.TraceInformation("Transformed: {0} = {1}.", string.Join(" + ", transformations), destinationFileRelative);
                }
            }
        }

        private void ReplaceTokensInFile(string destinationFileTemp, string configurationName)
        {
            var contents = File.ReadAllText(destinationFileTemp);
            var newContents = ReplaceTokens(contents, configurationName);
            if (contents != newContents)
            {
                File.WriteAllText(destinationFileTemp, newContents);
            }
        }

        private bool ReplaceWithTempIfChanged(string fileTemp, string file, bool dry = false)
        {
            try
            {
                if (!File.Exists(file))
                {
                    if (dry)
                    {
                        throw new DryRunFailedException(file);
                    }
                    File.Move(fileTemp, file);
                    return true;
                }
                var fileInfo = new FileInfo(file);
                var fileTempInfo = new FileInfo(fileTemp);
                if (fileInfo.Length != fileTempInfo.Length || File.ReadAllText(file) != File.ReadAllText(fileTemp))
                {
                    if (dry)
                    {
                        throw new DryRunFailedException(file);
                    }
                    File.Copy(fileTemp, file, true);
                    File.Delete(fileTemp);
                    return true;
                }
                return false;
            }
            finally
            {
                File.Delete(fileTemp);
            }
        }

        private bool PerformTransform(string sourceFile, string transformFile, string destinationFile)
        {
            if (!File.Exists(sourceFile))
                throw new FileNotFoundException("The source file was not found", sourceFile);

            var destinationDirectory = Path.GetDirectoryName(destinationFile);

            if (!Directory.Exists(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            // If a transformation file does not exist, just copy sourceFile as the destinationFile
            if (!File.Exists(transformFile))
            {
                var sourceContent = File.ReadAllText(sourceFile);
                File.WriteAllText(destinationFile, sourceContent);
                return false;
            }

            var transformTask = new TransformationTask(sourceFile, transformFile);
            transformTask.Execute(destinationFile);

            return true;
        }

        private string ReplaceTokens(string input, string configurationName)
        {
            var output = input.Replace("$configurationName$", configurationName);
            output = _envTokenRegex.Replace(output, match => Environment.GetEnvironmentVariable(match.Groups[1].Value));
            return output;
        }

        public void CreateEmptyDestinationFiles(bool dry = false)
        {
            Trace.TraceInformation("Creating Empty Destination Files at {0}", BaseDirectory);

            foreach (var originalFile in GetConfigFiles())
            {
                string destinationFile = originalFile.Replace(ConventionFileExtension, ".");
                var destinationFileRelative = Path.GetFullPath(destinationFile).Substring(Path.GetFullPath(DestinationDirectory).Length);
                if (!File.Exists(destinationFile))
                {
                    if (dry)
                    {
                        throw new DryRunFailedException(destinationFile);
                    }
                    File.WriteAllText(destinationFile, string.Empty);
                    Trace.TraceInformation("Created: {0}", destinationFileRelative);
                }
                else
                {
                    Trace.TraceInformation("Unchanged: {0}", destinationFileRelative);
                }
            }
        }
    }
}
