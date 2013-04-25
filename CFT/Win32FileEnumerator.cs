using System.Collections.Generic;
using System.IO;
using CSharpTest.Net.IO;

namespace BlogTalkRadio.Tools.CFT
{
    public class Win32FileEnumerator: IFileEnumerator
    {
        public IEnumerable<string> EnumerateFiles(string path, string pattern = null)
        {
            var files = new List<string>();

            var ff = new FindFile(Path.GetFullPath(path))
            {
                IncludeFiles = true,
                IncludeFolders = false,
                Recursive = true
            };
            ff.FileFound += (obj, e) => files.Add(e.FullPath);
            ff.Find(pattern);
            return files;
        }
    }
}
