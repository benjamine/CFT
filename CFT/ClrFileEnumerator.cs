using System.Collections.Generic;
using System.IO;

namespace BlogTalkRadio.Tools.CFT
{
    public class ClrFileEnumerator: IFileEnumerator
    {
        public IEnumerable<string> EnumerateFiles(string path, string pattern = null)
        {
            return Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories);
        }
    }
}
