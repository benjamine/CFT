using System.Collections.Generic;

namespace BlogTalkRadio.Tools.CFT
{
    public interface IFileEnumerator
    {
        IEnumerable<string> EnumerateFiles(string path, string pattern = null);
    }
}
