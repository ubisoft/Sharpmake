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
            if (CountFakeFiles() > 0)
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

        public static string[] DirectoryGetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.AllDirectories)
        {
            if (CountFakeFiles() > 0)
            {
                List<string> files = new List<string>();

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
                    for (int i = 1; i < fileFullPathParts.Length; ++i)
                    {
                        if (!dir.Dirs.TryGetValue(fileFullPathParts[i], out dir))
                            return new string[] { };
                    }

                    HashSet<FakeDirEntry> visited = new HashSet<FakeDirEntry>();
                    Stack<FakeDirEntry> visiting = new Stack<FakeDirEntry>();
                    visiting.Push(dir);
                    while (visiting.Count > 0)
                    {
                        FakeDirEntry visitedDir = visiting.Pop();
                        if (visited.Contains(visitedDir))
                            continue;

                        visited.Add(visitedDir);
                        files.AddRange(visitedDir.Files.Values.Select(x => x.Path));

                        foreach (var f in visitedDir.Dirs)
                            visiting.Push(f.Value);
                    }
                    return files.ToArray();
                }
                return new string[] { };
            }

            if (Directory.Exists(path))
                return Directory.GetFiles(path, searchPattern, searchOption);
            return new string[] { };
        }

        public static string[] DirectoryGetDirectories(string path)
        {
            if (CountFakeFiles() > 0)
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
                    for (int i = 1; i < fileFullPathParts.Length; ++i)
                    {
                        if (!dir.Dirs.TryGetValue(fileFullPathParts[i], out dir))
                            return new string[] { };
                    }
                    return dir.Dirs.Values.Select(x => x.Path).ToArray();
                }
                return new string[] { };
            }
            return Directory.GetDirectories(path);
        }
    }
}
