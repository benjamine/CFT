using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlogTalkRadio.Tools.CFT
{
    public class Conventions
    {
        #region DefaultInstance

        private static Conventions _Default;

        public static Conventions Default
        {
            get
            {
                if (_Default == null)
                {
                    _Default = new Conventions();
                }
                return _Default;
            }
        }

        #endregion

        /// <summary>
        /// Name of the file used as base for transformations. eg. if "Default" a file named "Web.Default.config" will be used
        /// </summary>
        public string DefaultName = "Default";

        /// <summary>
        /// Separator for config names, this allows to apply cascading transformations (eg. "production", "production-amazon", "production-amazon-bside")
        /// </summary>
        public string NameSeparator = "-";

        /// <summary>
        /// Indicates how the config filenames will be constructed
        /// </summary>
        public string FilePattern = ".{0}.";

    }
}
