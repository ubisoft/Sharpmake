// Copyright (c) 2017 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sharpmake
{
    public static partial class Util
    {
        public class FakeFileEntry
        {
            public string Path { get; set; }
            public int SizeInBytes { get; set; }
        }

        public class FakeDirEntry
        {
            public FakeDirEntry(string path)
            {
                Path = path;
            }

            public string Path = string.Empty;
            public Dictionary<string, FakeDirEntry> Dirs = new Dictionary<string, FakeDirEntry>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, FakeFileEntry> Files = new Dictionary<string, FakeFileEntry>(StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, FakeDirEntry> s_fakeTree = new Dictionary<string, FakeDirEntry>(StringComparer.OrdinalIgnoreCase);
        private static string s_fakePathPrefix = string.Empty;
        public static string FakePathPrefix
        {
            get { return s_fakePathPrefix; }
            set { s_fakePathPrefix = SimplifyPath(value); }
        }

        public static FakeFileEntry GetFakeFile(string path)
        {
            if (CountFakeFiles() > 0 && !string.IsNullOrEmpty(path))
            {
                string cleanPath = SimplifyPath(path).Replace(WindowsSeparator, UnixSeparator);
                string[] fileFullPathParts = cleanPath.Split(_pathSeparators, StringSplitOptions.RemoveEmptyEntries);
                FakeDirEntry dir;
                string root;
                if (UsesUnixSeparator)
                    root = UnixSeparator + fileFullPathParts[0];
                else
                    root = fileFullPathParts[0] + WindowsSeparator;
                if (s_fakeTree.TryGetValue(root, out dir))
                {
                    for (int i = 1; i < fileFullPathParts.Length - 1; ++i)
                    {
                        if (!dir.Dirs.TryGetValue(fileFullPathParts[i], out dir))
                            return null;
                    }

                    FakeFileEntry file;
                    if (dir.Files.TryGetValue(fileFullPathParts.Last(), out file))
                        return file;
                }
            }

            return null;
        }

        public static int GetFakeFileLength(string fileFullPath)
        {
            return GetFakeFile(fileFullPath)?.SizeInBytes ?? 0;
        }

        public static bool FileExists(string path)
        {
            if (CountFakeFiles() > 0)
                return GetFakeFile(path) != null;

            return File.Exists(path);
        }

        public static void AddNewFakeFile(string fileFullPath, int fileSize)
        {
            string cleanPath = FakePathPrefix + UnixSeparator + SimplifyPath(fileFullPath);
            string[] fileFullPathParts = cleanPath.Split(_pathSeparators, StringSplitOptions.RemoveEmptyEntries);
            lock (s_fakeTree)
            {
                string path;
                if (UsesUnixSeparator)
                    path = UnixSeparator + fileFullPathParts[0];
                else
                    path = fileFullPathParts[0] + WindowsSeparator;
                FakeDirEntry currentDir = s_fakeTree.GetValueOrAdd(path, new FakeDirEntry(path));
                for (int i = 1; i < (fileFullPathParts.Length - 1); ++i)
                {
                    path = Path.Combine(path, fileFullPathParts[i]);
                    currentDir = currentDir.Dirs.GetValueOrAdd(fileFullPathParts[i], new FakeDirEntry(path));
                }
                path = Path.Combine(path, fileFullPathParts[fileFullPathParts.Length - 1]);
                currentDir.Files.Add(fileFullPathParts[fileFullPathParts.Length - 1], new FakeFileEntry { Path = path, SizeInBytes = fileSize });
                ++s_fakeFilesCount;
            }
        }

        private static int s_fakeFilesCount = 0;
        public static int CountFakeFiles()
        {
            return s_fakeFilesCount;
        }

        public static void ClearFakeTree()
        {
            lock (s_fakeTree)
            {
                s_fakeTree.Clear();
                s_fakeFilesCount = 0;
            }
        }

        public static bool DirectoryExists(string directoryPath)
        {
            if (CountFakeFiles() > 0)
            {
                string cleanPath = SimplifyPath(directoryPath).Replace(WindowsSeparator, UnixSeparator);
                string[] fileFullPathParts = cleanPath.Split(_pathSeparators, StringSplitOptions.RemoveEmptyEntries);
                FakeDirEntry dir;
                string root;
                if (UsesUnixSeparator)
                    root = UnixSeparator + fileFullPathParts[0];
                else
                    root = fileFullPathParts[0] + WindowsSeparator;
                if (s_fakeTree.TryGetValue(root, out dir))
                {
                    for (int i = 1; i < fileFullPathParts.Length - 1; ++i)
                    {
                        if (!dir.Dirs.TryGetValue(fileFullPathParts[i], out dir))
                            return false;
                    }
                    return dir.Dirs.ContainsKey(fileFullPathParts[fileFullPathParts.Length - 1]);
                }
                return false;
            }
            return Directory.Exists(directoryPath);
        }

        internal static string ConvertWildcardToRegEx(string wildcard)
        {
            return "^" + Regex.Escape(wildcard).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
        }

        public static string[] DirectoryGetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.AllDirectories)
        {
            if (CountFakeFiles() > 0)
            {
                string cleanPath = SimplifyPath(path).Replace(WindowsSeparator, UnixSeparator);
                string[] fileFullPathParts = cleanPath.Split(_pathSeparators, StringSplitOptions.RemoveEmptyEntries);
                FakeDirEntry dir;

                // Determine if root directory exist in the fake tree
                string root = UsesUnixSeparator ? UnixSeparator + fileFullPathParts[0] : fileFullPathParts[0] + WindowsSeparator;
                if (!s_fakeTree.TryGetValue(root, out dir))
                    return new string[] { };

                // Iterate over the provided path and validate that each part exist in the fake tree
                for (int i = 1; i < fileFullPathParts.Length; ++i)
                {
                    if (!dir.Dirs.TryGetValue(fileFullPathParts[i], out dir))
                        return new string[] { };
                }

                // Setup filter if any
                Regex regexFilter = null;
                if (searchPattern != "*")
                    regexFilter = new Regex(ConvertWildcardToRegEx(searchPattern), RegexOptions.Singleline | RegexOptions.CultureInvariant);

                // Gather file items depending of filter and search option
                // - TopDirectoryOnly (early exit)
                if (searchOption == SearchOption.TopDirectoryOnly)
                    return (regexFilter != null ? dir.Files.Where(e => regexFilter.IsMatch(e.Key)).Select(e => e.Value.Path) : dir.Files.Values.Select(x => x.Path)).ToArray();
                // - AllDirectories
                HashSet<FakeDirEntry> visited = new HashSet<FakeDirEntry>();
                Stack<FakeDirEntry> visiting = new Stack<FakeDirEntry>();
                List<string> files = new List<string>();
                visiting.Push(dir);
                while (visiting.Count > 0)
                {
                    FakeDirEntry visitedDir = visiting.Pop();
                    if (visited.Contains(visitedDir))
                        continue;
                    visited.Add(visitedDir);

                    files.AddRange(regexFilter != null ? visitedDir.Files.Where(e => regexFilter.IsMatch(e.Key)).Select(e => e.Value.Path) : visitedDir.Files.Values.Select(x => x.Path));

                    foreach (var f in visitedDir.Dirs)
                        visiting.Push(f.Value);
                }
                return files.ToArray();
            }

            if (Directory.Exists(path))
                return Directory.GetFiles(path, searchPattern, searchOption);
            return new string[] { };
        }

        public static string[] DirectoryGetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (CountFakeFiles() > 0)
            {
                string cleanPath = SimplifyPath(path).Replace(WindowsSeparator, UnixSeparator);
                string[] fileFullPathParts = cleanPath.Split(_pathSeparators, StringSplitOptions.RemoveEmptyEntries);
                FakeDirEntry dir;

                // Determine if root directory exist in the fake tree
                string root = UsesUnixSeparator ? UnixSeparator + fileFullPathParts[0] : fileFullPathParts[0] + WindowsSeparator;
                if (!s_fakeTree.TryGetValue(root, out dir))
                    return new string[] { };

                // Iterate over the provided path and validate that each part exist in the fake tree
                for (int i = 1; i < fileFullPathParts.Length; ++i)
                {
                    if (!dir.Dirs.TryGetValue(fileFullPathParts[i], out dir))
                        return new string[] { };
                }

                // Setup filter if any
                Regex regexFilter = null;
                if (searchPattern != "*")
                    regexFilter = new Regex(ConvertWildcardToRegEx(searchPattern), RegexOptions.Singleline | RegexOptions.CultureInvariant);

                // Gather file items depending of filter and search option
                // - TopDirectoryOnly (early exit)
                if (searchOption == SearchOption.TopDirectoryOnly)
                    return (regexFilter != null ? dir.Dirs.Where(e => regexFilter.IsMatch(e.Key)).Select(e => e.Value.Path) : dir.Dirs.Values.Select(x => x.Path)).ToArray();
                // - AllDirectories
                HashSet<FakeDirEntry> visited = new HashSet<FakeDirEntry>();
                Stack<FakeDirEntry> visiting = new Stack<FakeDirEntry>();
                List<string> directories = new List<string>();
                visiting.Push(dir);
                while (visiting.Count > 0)
                {
                    FakeDirEntry visitedDir = visiting.Pop();
                    if (visited.Contains(visitedDir))
                        continue;
                    visited.Add(visitedDir);

                    directories.AddRange(regexFilter != null ? visitedDir.Dirs.Where(e => regexFilter.IsMatch(e.Key)).Select(e => e.Value.Path) : visitedDir.Dirs.Values.Select(x => x.Path));

                    foreach (var f in visitedDir.Dirs)
                        visiting.Push(f.Value);
                }
                return directories.ToArray();
            }

            if (Directory.Exists(path))
                return Directory.GetDirectories(path, searchPattern, searchOption);
            return new string[] { };
        }

        public static bool IsPathWithWildcards(string path)
        {
            return path.IndexOfAny(Util.WildcardCharacters) != -1;
        }

        internal static Strings ListFileSystemItemWithWildcardInFolderList(string currentWildcardPart, Strings currentFolderList, Func<string, string, SearchOption, string[]> getItemFunc, Func<string, bool> existItemFunc)
        {
            var result = new Strings();
            if (IsPathWithWildcards(currentWildcardPart))
            {
                foreach (string currentPath in currentFolderList)
                {
                    result.AddRange(getItemFunc(currentPath, currentWildcardPart, SearchOption.TopDirectoryOnly));
                }
            }
            else
            {
                foreach (string currentPath in currentFolderList)
                {
                    string filePath = Path.Combine(currentPath, currentWildcardPart);
                    if (existItemFunc(filePath))
                        result.Add(filePath);
                }
            }

            return result;
        }

        public static string[] DirectoryGetFilesWithWildcards(string path)
        {
            if (!IsPathWithWildcards(path))
                throw new ArgumentException("Path doesn't contains wildcard");

            var currentFolderList = new Strings();
            int firstWildcardIndex = path.IndexOfAny(Util.WildcardCharacters);
            int firstSeparatorIndex = path.LastIndexOfAny(Util._pathSeparators, firstWildcardIndex);
            string[] wildcardsPart = path.Substring(firstSeparatorIndex + 1).Split(Util._pathSeparators, StringSplitOptions.RemoveEmptyEntries);

            currentFolderList.Add(firstSeparatorIndex == -1 ? "." : path.Substring(0, firstSeparatorIndex));

            // Iterate over folders' part
            for (int i = 0; i < wildcardsPart.Length - 1; ++i)
                currentFolderList = ListFileSystemItemWithWildcardInFolderList(wildcardsPart[i], currentFolderList, DirectoryGetDirectories, DirectoryExists);

            // Handle last item of the wildcard part, that is a file
            var result = ListFileSystemItemWithWildcardInFolderList(wildcardsPart.Last(), currentFolderList, DirectoryGetFiles, FileExists);

            // Cleanup path
            var cleanResult = new Strings();
            cleanResult.AddRange(result.Select(SimplifyPath));

            return cleanResult.ToArray();
        }
    }
}
