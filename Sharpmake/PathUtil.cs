// Copyright (c) 2021 Ubisoft Entertainment
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Sharpmake
{
    public static partial class Util
    {
        public static readonly char UnixSeparator = '/';
        public static readonly char WindowsSeparator = '\\';
        private static readonly string s_unixMountPointForWindowsDrives = "/mnt/";

        public static readonly bool UsesUnixSeparator = Path.DirectorySeparatorChar == UnixSeparator;

        public static readonly char OtherSeparator = UsesUnixSeparator ? Util.WindowsSeparator : Util.UnixSeparator;

        public static readonly char[] _pathSeparators = { Util.WindowsSeparator, Util.UnixSeparator };

        public static void PathMakeStandard(IList<string> paths)
        {
            for (int i = 0; i < paths.Count; ++i)
                paths[i] = PathMakeStandard(paths[i]);
        }

        public static string PathMakeStandard(string path)
        {
            return PathMakeStandard(path, !Util.IsRunningOnUnix());
        }

        public static string PathMakeStandard(string path, bool forceToLower)
        {
            // cleanup the path by replacing the other separator by the correct one for this OS
            // then trim every trailing separators
            var standardPath = path.Replace(OtherSeparator, Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
            return forceToLower ? standardPath.ToLower() : standardPath;
        }

        public static string EnsureTrailingSeparator(string path)
        {
            // return the path passed in with only one trailing separator
            return path.TrimEnd(OtherSeparator).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        }

        public static bool PathIsSame(string path1, string path2)
        {
            return PathMakeStandard(path1).Equals(PathMakeStandard(path2), StringComparison.OrdinalIgnoreCase);
        }

        public static List<string> PathGetRelative(string sourceFullPath, Strings destFullPaths, bool ignoreCase = false)
        {
            List<String> result = new List<string>(destFullPaths.Count);

            foreach (string destFullPath in destFullPaths.Values)
            {
                result.Add(PathGetRelative(sourceFullPath, destFullPath, ignoreCase));
            }
            return result;
        }

        public static OrderableStrings PathGetRelative(string sourceFullPath, OrderableStrings destFullPaths, bool ignoreCase = false)
        {
            OrderableStrings result = new OrderableStrings(destFullPaths);

            for (int i = 0; i < result.Count; ++i)
            {
                result[i] = PathGetRelative(sourceFullPath, result[i], ignoreCase);
            }
            return result;
        }

        public static OrderableStrings PathGetRelative(string sourceFullPath, IEnumerable<string> destFullPaths, bool ignoreCase = false)
        {
            OrderableStrings result = new OrderableStrings(destFullPaths);

            for (int i = 0; i < result.Count; ++i)
            {
                result[i] = PathGetRelative(sourceFullPath, result[i], ignoreCase);
            }
            return result;
        }
        public static void PathSplitFileNameFromPath(string fileFullPath, out string fileName, out string pathName)
        {
            string[] fileFullPathParts = fileFullPath.Split(_pathSeparators, StringSplitOptions.RemoveEmptyEntries);

            fileName = "";
            pathName = "";

            for (int i = 0; i < fileFullPathParts.Length; ++i)
            {
                if (i == fileFullPathParts.Length - 1)
                {
                    fileName = fileFullPathParts[i];
                }
                else
                {
                    pathName += fileFullPathParts[i] + Path.DirectorySeparatorChar;
                }
            }

            pathName = pathName.TrimEnd(Path.DirectorySeparatorChar);
        }

        public static string RegexPathCombine(params string[] parts)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < parts.Length; ++i)
            {
                stringBuilder.Append(parts[i]);
                if (i != (parts.Length - 1))
                {
                    stringBuilder.Append(System.Text.RegularExpressions.Regex.Escape(Path.DirectorySeparatorChar.ToString()));
                }
            }
            return stringBuilder.ToString();
        }

        public static string GetConvertedRelativePath(
            string absolutePath,
            string relativePath,
            string newRelativeToFullPath,
            bool ignoreCase,
            string rootPath = null
        )
        {
            string tmpAbsolute = PathGetAbsolute(absolutePath, relativePath);
            string newRelativePath = PathGetRelative(newRelativeToFullPath, tmpAbsolute, ignoreCase);

            if (rootPath != null)
            {
                string cleanPath = Util.SimplifyPath(rootPath);
                if (!tmpAbsolute.StartsWith(cleanPath, StringComparison.OrdinalIgnoreCase))
                    return tmpAbsolute;
            }

            return newRelativePath;
        }

        private sealed unsafe class PathHelper
        {
            public static readonly int MaxPath = 260;
            private int _capacity;

            // Array of stack members.
            private char* _buffer;
            private int _bufferLength;

            public PathHelper(char* buffer, int length)
            {
                _buffer = buffer;
                _capacity = length;
                _bufferLength = 0;
            }

            // This method is called when we find a .. in the path.
            public bool RemoveLastDirectory(int lowestRemovableIndex)
            {
                if (Length == 0)
                    return false;

                Trace.Assert(_buffer[_bufferLength - 1] == Path.DirectorySeparatorChar);

                int lastSlash = -1;

                for (int i = _bufferLength - 2; i >= lowestRemovableIndex; i--)
                {
                    if (_buffer[i] == Path.DirectorySeparatorChar)
                    {
                        lastSlash = i;
                        break;
                    }
                }

                if (lastSlash == -1)
                {
                    if (lowestRemovableIndex == 0)
                    {
                        _bufferLength = 0;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                // Truncate the path.
                _bufferLength = lastSlash;

                return true;
            }

            public void Append(char value)
            {
                if (Length + 1 >= _capacity)
                    throw new PathTooLongException("Path too long:");

                if (value == Path.DirectorySeparatorChar)
                {
                    // Skipping consecutive backslashes.
                    if (_bufferLength > 0 && _buffer[_bufferLength - 1] == Path.DirectorySeparatorChar)
                        return;
                }

                // Important note: Must imcrement _bufferLength at the same time as writing into it as otherwise if
                // you are stepping in the debugger and ToString() is implicitly called by the debugger this could truncate the string
                // before the increment takes place
                _buffer[_bufferLength++] = value;
            }

            // Append a substring path component to the 
            public void Append(string str, int substStringIndex, int subStringLength)
            {
                if (Length + subStringLength >= _capacity)
                    throw new PathTooLongException("Path too long:");

                Trace.Assert(substStringIndex < str.Length);
                Trace.Assert(substStringIndex + subStringLength <= str.Length);


                int endLoop = substStringIndex + subStringLength;
                for (int i = substStringIndex; i < endLoop; ++i)
                {
                    // Important note: Must imcrement _bufferLength at the same time as writing into it as otherwise if
                    // you are stepping in the debugger and ToString() is implicitly called by the debugger this could truncate the string
                    // before the increment takes place
                    char value = str[i];
                    _buffer[_bufferLength++] = value;
                }
            }

            public void RemoveChar(int index)
            {
                Debug.Assert(index < _bufferLength);
                for (int i = index; i < _bufferLength - 1; ++i)
                {
                    _buffer[i] = _buffer[i + 1];
                }
                --_bufferLength;
            }

            public override string ToString()
            {
                return new string(_buffer, 0, _bufferLength);
            }

            public int Length
            {
                get
                {
                    return _bufferLength;
                }
            }

            internal char this[int index]
            {
                get
                {
                    Debug.Assert(index < _bufferLength);
                    return _buffer[index];
                }
            }
        };

        private static ConcurrentDictionary<string, string> s_cachedSimplifiedPaths = new ConcurrentDictionary<string, string>();

        public static unsafe string SimplifyPath(string path)
        {
            if (path.Length == 0)
                return string.Empty;

            if (path == ".")
                return path;

            string simplifiedPath = s_cachedSimplifiedPaths.GetOrAdd(path, s =>
            {
                // First construct a path helper to help with the conversion
                char* arrayPtr = stackalloc char[PathHelper.MaxPath];
                PathHelper pathHelper = new PathHelper(arrayPtr, PathHelper.MaxPath);

                int index = 0;
                int pathLength = path.Length;
                int numDot = 0;
                int lowestRemovableIndex = 0;
                for (; index < pathLength; ++index)
                {
                    char currentChar = path[index];
                    if (currentChar == OtherSeparator)
                        currentChar = Path.DirectorySeparatorChar;

                    if (currentChar == '.')
                    {
                        ++numDot;
                        if (numDot > 2)
                        {
                            throw new ArgumentException($"Invalid path format: {path}");
                        }
                    }
                    else
                    {
                        if (numDot == 1)
                        {
                            if (currentChar == Path.DirectorySeparatorChar)
                            {
                                // Path starts a path of the format .\
                                numDot = 0;
                                continue;
                            }
                            else
                            {
                                pathHelper.Append('.');
                            }
                            numDot = 0;
                            pathHelper.Append(currentChar);
                        }
                        else if (numDot == 2)
                        {
                            if (currentChar != Path.DirectorySeparatorChar)
                                throw new ArgumentException($"Invalid path format: {path}");

                            // Path contains a path of the format ..\
                            bool success = pathHelper.RemoveLastDirectory(lowestRemovableIndex);
                            if (!success)
                            {
                                pathHelper.Append('.');
                                pathHelper.Append('.');
                                lowestRemovableIndex = pathHelper.Length;
                            }
                            numDot = 0;
                            if (pathHelper.Length > 0)
                                pathHelper.Append(currentChar);
                        }
                        else
                        {
                            if (Util.IsRunningOnUnix() &&
                                index == 0 && currentChar == Path.DirectorySeparatorChar && Path.IsPathRooted(path))
                                pathHelper.Append(currentChar);

                            if (currentChar != Path.DirectorySeparatorChar || pathHelper.Length > 0)
                                pathHelper.Append(currentChar);
                        }
                    }
                }
                if (numDot == 2)
                {
                    // Path contains a path of the format \..\
                    if (!pathHelper.RemoveLastDirectory(lowestRemovableIndex))
                    {
                        pathHelper.Append('.');
                        pathHelper.Append('.');
                    }
                }

                return pathHelper.ToString();
            });

            return simplifiedPath;
        }

        // Note: This method assumes that SimplifyPath has been called for the argument.
        internal static unsafe void SplitStringUsingStack(string path, char separator, int* splitIndexes, int* splitLengths, ref int splitElementsUsedCount, int splitArraySize)
        {
            int lastSeparatorIndex = -1;
            int pathLength = path.Length;
            for (int index = 0; index < pathLength; ++index)
            {
                char currentChar = path[index];

                if (currentChar == separator)
                {
                    if (splitElementsUsedCount == splitArraySize)
                        throw new Exception("Too much path separators");

                    int startIndex = lastSeparatorIndex + 1;
                    int length = index - startIndex;
                    if (length > 0)
                    {
                        splitIndexes[splitElementsUsedCount] = startIndex;
                        splitLengths[splitElementsUsedCount] = length;
                        lastSeparatorIndex = index;
                        ++splitElementsUsedCount;
                    }
                }
            }

            if (lastSeparatorIndex < pathLength - 1)
            {
                int startIndex = lastSeparatorIndex + 1;
                splitIndexes[splitElementsUsedCount] = startIndex;
                splitLengths[splitElementsUsedCount] = pathLength - startIndex;
                ++splitElementsUsedCount;
            }
        }

        public static unsafe string PathGetRelative(string sourceFullPath, string destFullPath, bool ignoreCase = false)
        {
            sourceFullPath = SimplifyPath(sourceFullPath);
            destFullPath = SimplifyPath(destFullPath);

            int* sourcePathIndexes = stackalloc int[128];
            int* sourcePathLengths = stackalloc int[128];
            int sourcePathNbrElements = 0;
            SplitStringUsingStack(sourceFullPath, Path.DirectorySeparatorChar, sourcePathIndexes, sourcePathLengths, ref sourcePathNbrElements, 128);

            int* destPathIndexes = stackalloc int[128];
            int* destPathLengths = stackalloc int[128];
            int destPathNbrElements = 0;
            SplitStringUsingStack(destFullPath, Path.DirectorySeparatorChar, destPathIndexes, destPathLengths, ref destPathNbrElements, 128);

            int samePathCounter = 0;

            // Find out common path length.
            //StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            int maxPathLength = Math.Min(sourcePathNbrElements, destPathNbrElements);
            for (int i = 0; i < maxPathLength; i++)
            {
                int sourceLength = sourcePathLengths[i];
                if (sourceLength != destPathLengths[i])
                    break;

                if (string.Compare(sourceFullPath, sourcePathIndexes[i], destFullPath, destPathIndexes[i], sourceLength, StringComparison.OrdinalIgnoreCase) != 0)
                    break;

                samePathCounter++;
            }

            if (samePathCounter == 0)
                return destFullPath;

            if (sourcePathNbrElements == destPathNbrElements && sourcePathNbrElements == samePathCounter)
                return ".";

            char* arrayPtr = stackalloc char[PathHelper.MaxPath];
            PathHelper pathHelper = new PathHelper(arrayPtr, PathHelper.MaxPath);

            for (int i = samePathCounter; i < sourcePathNbrElements; i++)
            {
                if (pathHelper.Length > 0)
                    pathHelper.Append(Path.DirectorySeparatorChar);
                pathHelper.Append('.');
                pathHelper.Append('.');
            }

            for (int i = samePathCounter; i < destPathNbrElements; i++)
            {
                if (pathHelper.Length > 0)
                    pathHelper.Append(Path.DirectorySeparatorChar);
                pathHelper.Append(destFullPath, destPathIndexes[i], destPathLengths[i]);
            }

            return pathHelper.ToString();
        }


        public static List<string> PathGetAbsolute(string sourceFullPath, Strings destFullPaths)
        {
            List<string> result = new List<string>(destFullPaths.Count);

            foreach (string destFullPath in destFullPaths.Values)
            {
                result.Add(PathGetAbsolute(sourceFullPath, destFullPath));
            }
            return result;
        }

        private static ConcurrentDictionary<string, string> s_cachedCombinedToAbsolute = new ConcurrentDictionary<string, string>();

        public static string PathGetAbsolute(string absolutePath, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return absolutePath;

            // Handle environment variables and string that contains more than 1 path
            if (relativePath.StartsWith("$", StringComparison.Ordinal) || relativePath.Count(x => x == ';') > 1)
                return relativePath;

            string cleanRelative = SimplifyPath(relativePath);
            if (Path.IsPathRooted(cleanRelative))
                return cleanRelative;

            string resultPath = s_cachedCombinedToAbsolute.GetOrAdd(string.Format("{0}|{1}", absolutePath, relativePath), combined =>
            {
                string firstPart = PathMakeStandard(absolutePath);
                if (firstPart.Last() == Path.VolumeSeparatorChar)
                    firstPart += Path.DirectorySeparatorChar;

                string result = Path.Combine(firstPart, cleanRelative);
                return Path.GetFullPath(result);
            });

            return resultPath;
        }

        public static void ResolvePath(string root, ref IEnumerable<string> paths)
        {
            paths = paths.Select(path => ResolvePath(root, path));
        }

        internal static void ResolvePathAndFixCase(string root, ref IEnumerable<string> paths)
        {
            paths = paths.Select(path => ResolvePathAndFixCase(root, path));
        }

        [Flags]
        internal enum KeyValuePairResolveType
        {
            ResolveKey = 1 << 0,
            ResolveValue = 1 << 1,
            ResolveAll = ResolveKey | ResolveValue
        }

        internal static void ResolvePathAndFixCase(string root, KeyValuePairResolveType resolveType, ref HashSet<KeyValuePair<string, string>> paths)
        {
            if (paths.Count == 0)
                return;

            foreach (var keyValuePair in paths.ToList())
            {
                string key = keyValuePair.Key;
                string value = keyValuePair.Value;

                if (resolveType.HasFlag(KeyValuePairResolveType.ResolveKey))
                    key = ResolvePathAndFixCase(root, key);

                if (resolveType.HasFlag(KeyValuePairResolveType.ResolveValue))
                    value = ResolvePathAndFixCase(root, value);

                if (keyValuePair.Key != key || keyValuePair.Value != value)
                {
                    paths.Remove(keyValuePair);
                    paths.Add(new KeyValuePair<string, string>(key, value));
                }
            }
        }

        public static void ResolvePath(string root, ref Strings paths)
        {
            List<string> sortedPaths = paths.Values;
            foreach (string path in sortedPaths)
            {
                string resolvedPath = ResolvePath(root, path);
                paths.UpdateValue(path, resolvedPath);
            }
        }

        internal static void ResolvePathAndFixCase(string root, ref Strings paths)
        {
            List<string> sortedPaths = paths.Values;
            foreach (string path in sortedPaths)
            {
                string fixedCase = ResolvePathAndFixCase(root, path);
                paths.UpdateValue(path, fixedCase);
            }
        }

        public static void ResolvePath(string root, ref OrderableStrings paths)
        {
            for (int i = 0; i < paths.Count; ++i)
            {
                string resolvedPath = ResolvePath(root, paths[i]);
                i = paths.SetOrRemoveAtIndex(i, resolvedPath);
            }
            paths.Sort();
        }

        internal static void ResolvePathAndFixCase(string root, ref OrderableStrings paths)
        {
            for (int i = 0; i < paths.Count; ++i)
            {
                string fixedCase = ResolvePathAndFixCase(root, paths[i]);
                i = paths.SetOrRemoveAtIndex(i, fixedCase);
            }
            paths.Sort();
        }

        public static void ResolvePath(string root, ref string path)
        {
            path = ResolvePath(root, path);
        }

        internal static void ResolvePathAndFixCase(string root, ref string path)
        {
            path = ResolvePathAndFixCase(root, path);
        }

        public static string ResolvePath(string root, string path)
        {
            return Util.PathGetAbsolute(root, Util.PathMakeStandard(path));
        }

        internal static string ResolvePathAndFixCase(string root, string path)
        {
            string resolvedPath = ResolvePath(root, path);
            return GetProperFilePathCapitalization(resolvedPath);
        }


        /// <summary>
        /// Gets the absolute path up to the intersection of two specified absolute paths.
        /// </summary>
        /// <param name="absPathA">First absolute path.</param>
        /// <param name="absPathB">Second absolute path.</param>
        /// <returns>Returns an absolute path up to the intersection of both specified paths.</returns>
        public static string GetPathIntersection(string absPathA, string absPathB)
        {
            var builder = new StringBuilder();

            string stdPathA = PathMakeStandard(absPathA);
            string stdPathB = PathMakeStandard(absPathB);

            string[] pathTokensA = stdPathA.Split(Path.DirectorySeparatorChar);
            string[] pathTokensB = stdPathB.Split(Path.DirectorySeparatorChar);

            int maxPossibleCommonChunks = Math.Min(pathTokensA.Length, pathTokensB.Length);
            for (int i = 0; i < maxPossibleCommonChunks; ++i)
            {
                if (pathTokensA[i] != pathTokensB[i])
                    break;

                builder.Append(pathTokensA[i] + Path.DirectorySeparatorChar);
            }

            return builder.ToString();
        }

        private static string GetProperFilePathCapitalization(string filename)
        {
            StringBuilder builder = new StringBuilder();
            FileInfo fileInfo = new FileInfo(filename);
            DirectoryInfo dirInfo = fileInfo.Directory;
            GetProperDirectoryCapitalization(dirInfo, null, ref builder);
            string properFileName = fileInfo.Name;
            if (dirInfo != null && dirInfo.Exists)
            {
                foreach (var fsInfo in dirInfo.EnumerateFileSystemInfos())
                {
                    if (((fsInfo.Attributes & FileAttributes.Directory) != FileAttributes.Directory)
                        && string.Compare(fsInfo.Name, fileInfo.Name, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        properFileName = fsInfo.Name;
                        break;
                    }
                }
            }
            return Path.Combine(builder.ToString(), properFileName);
        }

        private static ConcurrentDictionary<string, string> s_capitalizedPaths = new ConcurrentDictionary<string, string>();

        private static void GetProperDirectoryCapitalization(DirectoryInfo dirInfo, DirectoryInfo childInfo, ref StringBuilder pathBuilder)
        {
            string lowerPath = dirInfo.FullName.ToLower();
            string capitalizedPath;
            if (s_capitalizedPaths.TryGetValue(lowerPath, out capitalizedPath))
            {
                pathBuilder.Append(capitalizedPath);
            }
            else
            {
                if (dirInfo.Parent != null)
                {
                    GetProperDirectoryCapitalization(dirInfo.Parent, dirInfo, ref pathBuilder);
                }
                else
                {
                    // Make root drive always uppercase
                    pathBuilder.Append(dirInfo.Name.ToUpper());
                }
            }
            s_capitalizedPaths.TryAdd(lowerPath, pathBuilder.ToString());

            if (childInfo != null)
            {
                // Note: Avoid double directory separator when at the root.
                if (dirInfo.Parent != null)
                    pathBuilder.Append(Path.DirectorySeparatorChar);
                bool appendChild = true;
                if (dirInfo.Exists)
                {
                    var resultDirs = dirInfo.GetDirectories(childInfo.Name, SearchOption.TopDirectoryOnly);
                    if (resultDirs.Length > 0)
                    {
                        pathBuilder.Append(resultDirs[0].Name);
                        appendChild = false;
                    }
                    else
                    {
                        foreach (var fsInfo in dirInfo.EnumerateFileSystemInfos())
                        {
                            if (string.Compare(fsInfo.Name, childInfo.Name, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                pathBuilder.Append(fsInfo.Name);
                                appendChild = false;
                                break;
                            }
                        }
                    }
                }
                if (appendChild)
                    pathBuilder.Append(childInfo.Name);
            }
        }

        public static OrderableStrings PathGetCapitalized(OrderableStrings fullPaths)
        {
            OrderableStrings result = new OrderableStrings(fullPaths);

            for (int i = 0; i < result.Count; ++i)
            {
                result[i] = GetCapitalizedPath(result[i]);
            }
            return result;
        }

        public static string GetCapitalizedPath(string path)
        {
            if (CountFakeFiles() > 0)
                return path;

            // Don't touch paths starting with ..
            if (path.StartsWith("..", StringComparison.Ordinal))
                return path;
            string pathLC = path.ToLower();
            string capitalizedPath;
            if (s_capitalizedPaths.TryGetValue(pathLC, out capitalizedPath))
            {
                return capitalizedPath;
            }

            if (File.Exists(path))
            {
                capitalizedPath = GetProperFilePathCapitalization(path);
            }
            else
            {
                StringBuilder pathBuilder = new StringBuilder();
                DirectoryInfo dirInfo = new DirectoryInfo(path);
                GetProperDirectoryCapitalization(dirInfo, null, ref pathBuilder);

                capitalizedPath = pathBuilder.ToString();
            }
            s_capitalizedPaths.TryAdd(pathLC, capitalizedPath);
            return capitalizedPath;
        }

        /// <summary>
        /// Returns path with drive letter in lower case.
        /// 
        /// WSL mounts windows drive using lowercase letters: /mnt/c, /mnt/d...
        /// </summary>
        /// <param name="path">The path to be modified.</param>
        /// <returns></returns>
        internal static string DecapitalizeDriveLetter(string path)
        {
            if (path.Length < 2 || path[1] != ':')
                return path;
            return path.Substring(0, 1).ToLower() + path.Substring(1);
        }

        internal static string ConvertToUnixSeparators(string path)
        {
            return path.Replace(WindowsSeparator, UnixSeparator);
        }

        internal static string ConvertToMountedUnixPath(string path)
        {
            return s_unixMountPointForWindowsDrives + ConvertToUnixSeparators(DecapitalizeDriveLetter(EnsureTrailingSeparator(path)).Replace(@":", string.Empty));
        }

        /// <summary>
        /// The input path got its beginning of path matching the inputHeadPath replaced by the replacementHeadPath.
        /// 
        /// Throws if the fullInputPath doesn't start with inputHeadPath.
        /// 
        /// Function is case insensitive but preserves path casing.
        /// </summary>
        /// <param name="fullInputPath">The path to be modified.</param>
        /// <param name="inputHeadPath">The subpath in the head of fullInputPath to replace.</param>
        /// <param name="replacementHeadPath">The subpath that will replace the inputHeadPath</param>
        /// <returns></returns>
        public static string ReplaceHeadPath(this string fullInputPath, string inputHeadPath, string replacementHeadPath)
        {
            // Normalize paths before comparing and combining them, to prevent false mismatch between '\\' and '/'.
            fullInputPath = Util.PathMakeStandard(fullInputPath, false);
            inputHeadPath = Util.PathMakeStandard(inputHeadPath, false);
            replacementHeadPath = Util.PathMakeStandard(replacementHeadPath, false);

            inputHeadPath = EnsureTrailingSeparator(inputHeadPath);

            if (!fullInputPath.StartsWith(inputHeadPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"The subpath to be replaced '{inputHeadPath}'\n is not found at the beginning of the input path '{fullInputPath}'.");
            }

            var pathRelativeToOutput = fullInputPath.Substring(inputHeadPath.Length);
            var modifiedPath = Path.Combine(replacementHeadPath, pathRelativeToOutput);

            return modifiedPath;
        }
    }
}
