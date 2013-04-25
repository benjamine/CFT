#region Copyright 2011-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace CSharpTest.Net.IO
{
    /// <summary>
    /// Provides an efficient file/directory enumeration that behaves several orders faster than Directory.GetXxxx() methods.
    /// </summary>
    public class FindFile
    {
        #region Static data and helpers
        private static readonly char[] InvalidFilePathChars;
        private static readonly char[] InvalidFilePatternChars;
        static FindFile()
        {
            InvalidFilePathChars = Path.GetInvalidPathChars();
            List<char> set = new List<char>(Path.GetInvalidFileNameChars());
            set.Remove('*');
            set.Remove('?');
            InvalidFilePatternChars = set.ToArray();
        }
        /// <summary> Enumerates the files directly in the directory specified </summary>
        public static void FilesIn(string directory, Action<FileFoundEventArgs> e)
        {
            FindFile ff = new FindFile(directory, STAR, false, false, true);
            ff.FileFound = (o, a) => e(a);
            ff.Find();
        }
        /// <summary> Enumerates the folders directly in the directory specified </summary>
        public static void FoldersIn(string directory, Action<FileFoundEventArgs> e)
        {
            FindFile ff = new FindFile(directory, STAR, false, true, false);
            ff.FileFound = (o, a) => e(a);
            ff.Find();
        }
        /// <summary> Enumerates the files and folders directly in the directory specified </summary>
        public static void FilesAndFoldersIn(string directory, Action<FileFoundEventArgs> e)
        {
            FindFile ff = new FindFile(directory, STAR, false, true, true);
            ff.FileFound = (o, a) => e(a);
            ff.Find();
        }
        /// <summary> Enumerates the files anywhere under the directory specified </summary>
        public static void AllFilesIn(string directory, Action<FileFoundEventArgs> e)
        {
            FindFile ff = new FindFile(directory, STAR, true, false, true);
            ff.FileFound = (o, a) => e(a);
            ff.Find();
        }
        /// <summary> Enumerates the folders anywhere under the directory specified </summary>
        public static void AllFoldersIn(string directory, Action<FileFoundEventArgs> e)
        {
            FindFile ff = new FindFile(directory, STAR, true, true, false);
            ff.FileFound = (o, a) => e(a);
            ff.Find();
        }
        /// <summary> Enumerates the files and folders anywhere under the directory specified </summary>
        public static void AllFilesAndFoldersIn(string directory, Action<FileFoundEventArgs> e)
        {
            FindFile ff = new FindFile(directory, STAR, true, true, true);
            ff.FileFound = (o, a) => e(a);
            ff.Find();
        }
        #endregion
        #region Kernel32
        internal static class Kernel32
        {
            internal const int MAX_PATH = 260;
            internal const int MAX_ALTERNATE = 14;
            internal const int ERROR_FILE_NOT_FOUND = 2;
            internal const int ERROR_PATH_NOT_FOUND = 3;
            internal const int ERROR_ACCESS_DENIED = 5;

            [StructLayout(LayoutKind.Sequential)]
            public struct FILETIME
            {
                public uint dwLowDateTime;
                public uint dwHighDateTime;

                public DateTime ToDateTimeUtc()
                {
                    return DateTime.FromFileTimeUtc(dwLowDateTime | ((long) dwHighDateTime << 32));
                }
            };

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct WIN32_FIND_DATA
            {
                public FileAttributes dwFileAttributes;
                public FILETIME ftCreationTime;
                public FILETIME ftLastAccessTime;
                public FILETIME ftLastWriteTime;
                public uint nFileSizeHigh; //changed all to uint from int, otherwise you run into unexpected overflow
                public uint nFileSizeLow; //| http://www.pinvoke.net/default.aspx/Structures/WIN32_FIND_DATA.html
                private uint dwReserved0; //|
                private uint dwReserved1; //v

                [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_PATH)] public char[] cFileName;

                [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_ALTERNATE)] private char[] cAlternateFileName;

                public bool IgnoredByName
                {
                    get
                    {
                        return
                            (cFileName[0] == ZERO) ||
                            (cFileName[0] == '.' && cFileName[1] == ZERO) ||
                            (cFileName[0] == '.' && cFileName[1] == '.' && cFileName[2] == ZERO)
                            ;
                    }
                }
            }

            public enum FINDEX_INFO_LEVELS
            {
                FindExInfoStandard = 0,
                FindExInfoBasic = 1
            }

            public enum FINDEX_SEARCH_OPS
            {
                FindExSearchNameMatch = 0,
                FindExSearchLimitToDirectories = 1,
                FindExSearchLimitToDevices = 2
            }

            [Flags]
            public enum FINDEX_ADDITIONAL_FLAGS
            {
                FindFirstExCaseSensitive = 1,
                FindFirstExLargeFetch = 2,
            }

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr FindFirstFileEx(
                IntPtr lpFileName,
                FINDEX_INFO_LEVELS fInfoLevelId,
                out WIN32_FIND_DATA lpFindFileData,
                FINDEX_SEARCH_OPS fSearchOp,
                IntPtr lpSearchFilter,
                FINDEX_ADDITIONAL_FLAGS dwAdditionalFlags);

            [DllImport("kernel32", CharSet = CharSet.Unicode)]
            public static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

            [DllImport("kernel32.dll")]
            public static extern bool FindClose(IntPtr hFindFile);
        }
        #endregion
        #region FileFoundEventArgs
        /// <summary> Provides a simple struct to capture file info, given by <see cref="FileFoundEventArgs.GetInfo()"/> method </summary>
        public struct Info
        {
            /// <summary> Returns the parent folder's full path </summary>
            public string ParentPath { get { return Path.GetDirectoryName(FullPath); } }
            /// <summary> Gets or sets the full path of the file or folder </summary>
            public string FullPath { get; set; }
            /// <summary> Returns the file or folder name (with extension) </summary>
            public string Name { get { return Path.GetFileName(FullPath); } }
            /// <summary> Returns the extenion or String.Empty </summary>
            public string Extension { get { return Path.GetExtension(FullPath); } }
            /// <summary> Returns the UNC path to the parent folder </summary>
            public string ParentPathUnc { get { return (FullPath.StartsWith(@"\\")) ? ParentPath : (UncPrefix + ParentPath); } }
            /// <summary> Returns the UNC path to the file or folder </summary>
            public string FullPathUnc { get { return (FullPath.StartsWith(@"\\")) ? FullPath : (UncPrefix + FullPath); } }
            /// <summary> Gets or sets the length in bytes </summary>
            public long Length { get; set; }
            /// <summary> Gets or sets the file or folder attributes </summary>
            public FileAttributes Attributes { get; set; }
            /// <summary> Gets or sets the file or folder CreationTime in Utc </summary>
            public DateTime CreationTimeUtc { get; set; }
            /// <summary> Gets or sets the file or folder LastAccessTime in Utc </summary>
            public DateTime LastAccessTimeUtc { get; set; }
            /// <summary> Gets or sets the file or folder LastWriteTime in Utc </summary>
            public DateTime LastWriteTimeUtc { get; set; }
        }

        internal class Win32FindData
        {
            public char[] Buffer;
            public IntPtr BufferAddress;
            public Kernel32.WIN32_FIND_DATA Value;
        }

        /// <summary>
        /// Provides access to the file or folder information durring enumeration, DO NOT keep a reference to this
        /// class as it's meaning will change durring enumeration.
        /// </summary>
        public sealed class FileFoundEventArgs : EventArgs
        {
            private readonly Win32FindData _ff;
            private int _uncPrefixLength;
            private int _parentNameLength;
            private int _itemNameLength;
            private bool _cancelEnumeration;

            internal FileFoundEventArgs(Win32FindData ff)
            {
                _ff = ff;
            }

            internal void SetNameOffsets(int uncPrefixLength, int parentIx, int itemIx)
            {
                _parentNameLength = parentIx;
                _itemNameLength = itemIx;
                _uncPrefixLength = uncPrefixLength;
            }

            /// <summary> Returns the parent folder's full path </summary>
            public string ParentPath { get { return new String(_ff.Buffer, _uncPrefixLength, _parentNameLength - _uncPrefixLength); } }
            /// <summary> Returns the UNC path to the parent folder </summary>
            public string ParentPathUnc { get { return new String(_ff.Buffer, 0, _parentNameLength); } }
            /// <summary> Gets the full path of the file or folder </summary>
            public string FullPath { get { return new String(_ff.Buffer, _uncPrefixLength, _itemNameLength - _uncPrefixLength); } }
            /// <summary> Returns the UNC path to the file or folder </summary>
            public string FullPathUnc { get { return new String(_ff.Buffer, 0, _itemNameLength); } }
            /// <summary> Returns the file or folder name (with extension) </summary>
            public string Name { get { return new String(_ff.Buffer, _parentNameLength, _itemNameLength - _parentNameLength); } }
            /// <summary> Returns the extenion or String.Empty </summary>
            public string Extension
            {
                get 
                {
                    for(int ix = _itemNameLength; ix > _parentNameLength; ix--)
                        if (_ff.Buffer[ix] == '.')
                            return new String(_ff.Buffer, ix, _itemNameLength - ix);
                    return String.Empty;
                }
            }

            /// <summary> Gets the length in bytes </summary>
            public long Length { get { return _ff.Value.nFileSizeLow | ((long)_ff.Value.nFileSizeHigh << 32); } }
            /// <summary> Gets the file or folder attributes </summary>
            public FileAttributes Attributes { get { return _ff.Value.dwFileAttributes; } }
            /// <summary> Gets the file or folder CreationTime in Utc </summary>
            public DateTime CreationTimeUtc { get { return _ff.Value.ftCreationTime.ToDateTimeUtc(); } }
            /// <summary> Gets the file or folder LastAccessTime in Utc </summary>
            public DateTime LastAccessTimeUtc { get { return _ff.Value.ftLastAccessTime.ToDateTimeUtc(); } }
            /// <summary> Gets the file or folder LastWriteTime in Utc </summary>
            public DateTime LastWriteTimeUtc { get { return _ff.Value.ftLastWriteTime.ToDateTimeUtc(); } }
            /// <summary> Returns true if the file or folder is ReadOnly </summary>
            public bool IsReadOnly { get { return (Attributes & FileAttributes.ReadOnly) != 0; } }
            /// <summary> Returns true if the file or folder is Hidden </summary>
            public bool IsHidden { get { return (Attributes & FileAttributes.Hidden) != 0; } }
            /// <summary> Returns true if the file or folder is System </summary>
            public bool IsSystem { get { return (Attributes & FileAttributes.System) != 0; } }
            /// <summary> Returns true if the file or folder is Directory </summary>
            public bool IsDirectory { get { return (Attributes & FileAttributes.Directory) != 0; } }
            /// <summary> Returns true if the file or folder is ReparsePoint </summary>
            public bool IsReparsePoint { get { return (Attributes & FileAttributes.ReparsePoint) != 0; } }
            /// <summary> Returns true if the file or folder is Compressed </summary>
            public bool IsCompressed { get { return (Attributes & FileAttributes.Compressed) != 0; } }
            /// <summary> Returns true if the file or folder is Offline </summary>
            public bool IsOffline { get { return (Attributes & FileAttributes.Offline) != 0; } }
            /// <summary> Returns true if the file or folder is Encrypted </summary>
            public bool IsEncrypted { get { return (Attributes & FileAttributes.Encrypted) != 0; } }
            /// <summary>
            /// Captures the current state as a <see cref="FindFile.Info"/> structure.
            /// </summary>
            public Info GetInfo()
            {
                return new Info
                           {
                               FullPath = FullPath,
                               Length = Length,
                               Attributes = Attributes,
                               CreationTimeUtc = CreationTimeUtc,
                               LastAccessTimeUtc = LastAccessTimeUtc,
                               LastWriteTimeUtc = LastWriteTimeUtc,
                           };
            }
            /// <summary> Gets or sets the Cancel flag to abort the current enumeration </summary>
            public bool CancelEnumeration
            {
                get { return _cancelEnumeration; }
                set { _cancelEnumeration = value; }
            }
        }
        #endregion

        private const string STAR = "*";
        private const char SLASH = '\\';
        private const char ZERO = '\0';
        /// <summary> Returns the Unc path prefix used </summary>
        public const string UncPrefix = @"\\?\";

        private readonly Win32FindData _ff;

        private char[] _fpattern;
        private int _baseOffset;
        private bool _recursive;
        private bool _includeFolders;
        private bool _includeFiles;
        private bool _isUncPath;
        /// <summary> Creates a FindFile instance. </summary>
        public FindFile() : this(UncPrefix, STAR, true, true, true) { }
        /// <summary> Creates a FindFile instance. </summary>
        public FindFile(string rootDirectory) : this(rootDirectory, STAR, true, true, true) { }
        /// <summary> Creates a FindFile instance. </summary>
        public FindFile(string rootDirectory, string filePattern) : this(rootDirectory, filePattern, true, true, true) { }
        /// <summary> Creates a FindFile instance. </summary>
        public FindFile(string rootDirectory, string filePattern, bool recursive) : this(rootDirectory, filePattern, recursive, true, true) { }
        /// <summary> Creates a FindFile instance. </summary>
        public FindFile(string rootDirectory, string filePattern, bool recursive, bool includeFolders) : this(rootDirectory, filePattern, recursive, includeFolders, true) { }
        /// <summary> Creates a FindFile instance. </summary>
        public FindFile(string rootDirectory, string filePattern, bool recursive, bool includeFolders, bool includeFiles)
        {
            if (String.IsNullOrEmpty(rootDirectory) || String.IsNullOrEmpty(filePattern))
                throw new ArgumentException();

            _ff = new Win32FindData();
            _ff.BufferAddress = IntPtr.Zero;
            _ff.Buffer = new char[0x1000];
            _ff.Value = new Kernel32.WIN32_FIND_DATA();

            _recursive = recursive;
            _includeFolders = includeFolders;
            _includeFiles = includeFiles;

            BaseDirectory = rootDirectory;
            FilePattern = filePattern;
        }
        /// <summary>
        /// The event-handler to raise when a file or folder is found.
        /// </summary>
        public event EventHandler<FileFoundEventArgs> FileFound;
        /// <summary>
        /// Gets or sets the maximum number of allowed characters in a complete path, default = 4kb
        /// </summary>
        public int MaxPath
        {
            get { return _ff.Buffer.Length; }
            set { Array.Resize(ref _ff.Buffer, Check.InRange(value, Kernel32.MAX_PATH, 0x100000)); }
        }
        private int UncPrefixLength { get { return _isUncPath ? 4 : 0; } }
        /// <summary> Gets or sets the base directory to search within </summary>
        public string BaseDirectory
        {
            get { return new String(_ff.Buffer, UncPrefixLength, _baseOffset - UncPrefixLength); }
            set
            {
                if (value.IndexOfAny(InvalidFilePathChars) > 0)
                    throw new InvalidOperationException("Invalid characters in path.");

                if (!value.StartsWith(@"\\"))
                    value = UncPrefix + value;
                if (!value.EndsWith(@"\"))
                    value += @"\";

                _isUncPath = value.StartsWith(UncPrefix);
                value.CopyTo(0, _ff.Buffer, 0, _baseOffset = value.Length);
            }
        }
        /// <summary>
        /// Gets or sets the file pattern to match while enumerating files and folders.
        /// </summary>
        public string FilePattern
        {
            get { return new String(_fpattern); }
            set
            {
                if (value.IndexOfAny(InvalidFilePatternChars) >= 0)
                    throw new InvalidOperationException("Invalid characters in pattern.");
                _fpattern = value.TrimStart(SLASH).ToCharArray();
            }
        }
        /// <summary> Gets or sets the Recursive flag </summary>
        public bool Recursive { get { return _recursive; } set { _recursive = value; } }
        /// <summary> Gets or sets the IncludeFiles flag </summary>
        public bool IncludeFiles { get { return _includeFiles; } set { _includeFiles = value; } }
        /// <summary> Gets or sets the IncludeFolders flag </summary>
        public bool IncludeFolders { get { return _includeFolders; } set { _includeFolders = value; } }
        /// <summary> Gets or sets the RaiseOnAccessDenied flag, when set to true an 'Access Denied' can be raised </summary>
        public bool RaiseOnAccessDenied { get; set; }
        /// <summary> Performs the search raising the FileFound event for each entry matching the request </summary>
        public void Find(string pattern)
        {
            FilePattern = pattern;
            Find();
        }
        /// <summary> Performs the search raising the FileFound event for each entry matching the request </summary>
        public void Find()
        {
            Check.NotNull(FileFound);
            GCHandle hdl = GCHandle.Alloc(_ff.Buffer, GCHandleType.Pinned);
            try
            {
                FileFoundEventArgs args = new FileFoundEventArgs(_ff);
                _ff.BufferAddress = hdl.AddrOfPinnedObject();

                FindFileEx(args, _baseOffset);
            }
            finally
            {
                _ff.BufferAddress = IntPtr.Zero;
                hdl.Free();
            }
        }

        private bool IsWild()
        {
            return (_fpattern.Length == 1 && _fpattern[0] == '*')
                   ||
                   (_fpattern.Length == 3 && _fpattern[0] == '*' && _fpattern[1] == '.' && _fpattern[2] == '*')
                ;
        }

        private void FindFileEx(FileFoundEventArgs args, int slength)
        {
            Kernel32.FINDEX_INFO_LEVELS findInfoLevel = Kernel32.FINDEX_INFO_LEVELS.FindExInfoStandard;
            Kernel32.FINDEX_ADDITIONAL_FLAGS additionalFlags = 0;

            if (Environment.OSVersion.Version.Major >= 6)
            {
                //Ignore short-names
                findInfoLevel = Kernel32.FINDEX_INFO_LEVELS.FindExInfoBasic;
                //Use large fetch table
                additionalFlags = Kernel32.FINDEX_ADDITIONAL_FLAGS.FindFirstExLargeFetch;
            }

            _fpattern.CopyTo(_ff.Buffer, slength);
            _ff.Buffer[slength + _fpattern.Length] = ZERO;

            IntPtr hFile = Kernel32.FindFirstFileEx(
                _ff.BufferAddress,
                findInfoLevel,
                out _ff.Value,
                Kernel32.FINDEX_SEARCH_OPS.FindExSearchNameMatch,
                IntPtr.Zero,
                additionalFlags);

            if ((IntPtr.Size == 4 && hFile.ToInt32() == -1) ||
                (IntPtr.Size == 8 && hFile.ToInt64() == -1L))
            {
                Win32Error(Marshal.GetLastWin32Error());
                return;
            }

            bool traverseDirs = _recursive && IsWild();

            try
            {
                do
                {
                    int sposition = slength;
                    for (int ix = 0; ix < Kernel32.MAX_PATH && sposition < _ff.Buffer.Length && _ff.Value.cFileName[ix] != 0; ix++)
                        _ff.Buffer[sposition++] = _ff.Value.cFileName[ix];

                    if (sposition == _ff.Buffer.Length)
                        throw new PathTooLongException();

                    if (!_ff.Value.IgnoredByName)
                    {
                        bool isDirectory = (_ff.Value.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory;

                        if ((_includeFolders && isDirectory) || (_includeFiles && !isDirectory))
                        {
                            args.SetNameOffsets(UncPrefixLength, slength, sposition);
                            FileFound(this, args);
                        }
                        if (traverseDirs && isDirectory)
                        {
                            _ff.Buffer[sposition++] = SLASH;
                            FindFileEx(args, sposition);
                        }
                    }
                } while (!args.CancelEnumeration && Kernel32.FindNextFile(hFile, out _ff.Value));
            }
            finally
            {
                Kernel32.FindClose(hFile);
            }

            // Recursive search for patterns other than '*' and '*.*' requires we enum directories again
            if (_recursive && !traverseDirs)
            {
                _ff.Buffer[slength] = '*';
                _ff.Buffer[slength + 1] = ZERO;

                hFile = Kernel32.FindFirstFileEx(
                    _ff.BufferAddress,
                    findInfoLevel,
                    out _ff.Value,
                    Kernel32.FINDEX_SEARCH_OPS.FindExSearchNameMatch,
                    IntPtr.Zero,
                    additionalFlags);

                if ((IntPtr.Size == 4 && hFile.ToInt32() == -1) ||
                    (IntPtr.Size == 8 && hFile.ToInt64() == -1L))
                {
                    Win32Error(Marshal.GetLastWin32Error());
                    return;
                }

                try
                {
                    do
                    {
                        int sposition = slength;
                        for (int ix = 0; ix < Kernel32.MAX_PATH && sposition < _ff.Buffer.Length && _ff.Value.cFileName[ix] != 0; ix++)
                            _ff.Buffer[sposition++] = _ff.Value.cFileName[ix];

                        if (sposition == _ff.Buffer.Length)
                            throw new PathTooLongException();

                        if (!_ff.Value.IgnoredByName)
                        {
                            bool isDirectory = (_ff.Value.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory;
                            if (isDirectory)
                            {
                                _ff.Buffer[sposition++] = SLASH;
                                FindFileEx(args, sposition);
                            }
                        }
                    } while (!args.CancelEnumeration && Kernel32.FindNextFile(hFile, out _ff.Value));
                }
                finally
                {
                    Kernel32.FindClose(hFile);
                }
            }
        }

        private void Win32Error(int errorCode)
        {
            switch(errorCode)
            {
                case Kernel32.ERROR_FILE_NOT_FOUND:
                case Kernel32.ERROR_PATH_NOT_FOUND:
                    return;
                case Kernel32.ERROR_ACCESS_DENIED:
                    if (!RaiseOnAccessDenied) return;
                    goto default;
                default:
                    throw new Win32Exception(errorCode);
            }
        }
    }
}