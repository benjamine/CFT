using System;
using System.Diagnostics;

namespace BlogTalkRadio.Tools.CFT
{
    class Program
    {
        private string _baseDirectory;
        private string _configurationName;
        private string _destinationDirectory;

        private bool _isInteractiveMode;

        public void Run(string[] args)
        {
            if (args.Length < 1 || args.Length > 3)
            {
                ShowUsage();
                return;
            }

            if (args.Length == 1 && args[0].ToLowerInvariant() == "-i")
            {
                _isInteractiveMode = true;
                GetInteractiveParameters();
            }
            else
            {
                GetParametersFromArguments(args);
            }


            var directoryProcessor = new DirectoryProcessor(_baseDirectory, _destinationDirectory);

            if (args.Length == 1 && !_isInteractiveMode)
            {
                directoryProcessor.CreateEmptyDestinationFiles();
            }
            else
            {
                directoryProcessor.PerformTransformations(_configurationName);
            }

            if (_isInteractiveMode)
            {
                Console.WriteLine("Press any key to finish");
                Console.ReadKey();
            }
        }

        private void GetParametersFromArguments(string[] args)
        {
            if (args.Length > 0)
            {
                _baseDirectory = args[0];
                _destinationDirectory = args[0];
            }

            if (args.Length > 1)
            {
                _configurationName = args[1];
            }

            if (args.Length > 2)
            {
                _destinationDirectory = args[2];
            }
        }

        private void GetInteractiveParameters()
        {
            Console.Write("Base Directory: ");
            _baseDirectory = Console.ReadLine();
            Console.Write("Configuration Name: ");
            _configurationName = Console.ReadLine();
            Console.Write("Destination Directory: ");
            _destinationDirectory = Console.ReadLine();
        }

        [STAThread]
        private static void Main(string[] args)
        {
            // log to console by default
            Trace.AutoFlush = true;
            Trace.Listeners.Add(new ConsoleTraceListener());

            new Program().Run(args);
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Usage: ");
            Console.WriteLine("	- In order to create empty destination files use cft.exe baseDirectory");
            Console.WriteLine("	- In order to perform transformation use cft.exe baseDirectory solutionConfig");
            Console.WriteLine(" - In order to deploy to another directory use cft.exe baseDirectory solutionConfig destinationDirectory");
        }
    }
}
