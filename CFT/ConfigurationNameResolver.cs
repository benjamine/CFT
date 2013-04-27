using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlogTalkRadio.Tools.CFT
{
    public class ConfigurationNameResolver
    {
        public string Filename { get; set; }

        public string EnvironmentVariable { get; set; }

        public string Default { get; set; }

        public string GetConfigurationName(out string choice)
        {
            string name;
            if (!string.IsNullOrWhiteSpace(Filename) && File.Exists(Filename))
            {
                name = File.ReadLines(Filename).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    choice = "file " + Filename;
                    return name;
                }
            }
            if (!string.IsNullOrWhiteSpace(EnvironmentVariable))
            {
                name = Environment.GetEnvironmentVariable(EnvironmentVariable);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    choice = "environment variable " + EnvironmentVariable;
                    return name;
                }
            }
            choice = "default value";
            return Default;
        }

    }
}
