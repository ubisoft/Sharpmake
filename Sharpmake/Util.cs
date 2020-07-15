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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Setup.Configuration;
using Microsoft.Win32;

namespace Sharpmake
{
    public static partial class Util
    {
        public static readonly char UnixSeparator = '/';
        public static readonly char WindowsSeparator = '\\';

        public static readonly bool UsesUnixSeparator = Path.DirectorySeparatorChar == UnixSeparator;

        public static readonly char OtherSeparator = UsesUnixSeparator ? Util.WindowsSeparator : Util.UnixSeparator;

        public const string DoubleQuotes = @"""";
        public const string EscapedDoubleQuotes = @"\""";

        // A better type name (for generic classes)
        public static string ToNiceTypeName(this Type type)
        {
            string resultName = (type.Namespace == null ? "" : (type.Namespace + ".")) + type.Name;
            if (type.GenericTypeArguments.Length == 0)
                return resultName;
            if (type.GenericTypeArguments.Length > 0)
            {
                if (resultName.LastIndexOf('`') >= 0)
                    resultName = resultName.Substring(0, resultName.LastIndexOf('`'));
                resultName += "<";
                resultName += ToNiceTypeName(type.GenericTypeArguments[0]);
                for (int i = 1; i < type.GenericTypeArguments.Length; ++i)
                {
                    resultName += "," + ToNiceTypeName(type.GenericTypeArguments[i]);
                }
                resultName += ">";
            }
            return resultName;
        }


        public static bool FlagsTest<T>(T value, T flags)
        {
            int intValue = (int)(object)value;
            int intflag = (int)(object)flags;
            return ((intValue & intflag) == intflag);
        }

        public static void PathMakeStandard(IList<string> paths)
        {
            for (int i = 0; i < paths.Count; ++i)
                paths[i] = PathMakeStandard(paths[i]);
        }

        public static string PathMakeStandard(string path)
        {
            return PathMakeStandard(path, !Util.IsRunningInMono());
        }

        /// <summary>
        /// Finds the first occurrence of directive and returns the 
        /// requested param value. Ex:
        /// GetTextTemplateDirectiveParam(ttPath, "output", "extension")
        /// will match:
        ///  <#@ output extension=".txt" #>
        /// and return ".txt"
        /// </summary>
        public static string GetTextTemplateDirectiveParam(string filePath, string directive, string paramName)
        {
            string[] templateText = File.ReadAllLines(filePath);

            Regex regex = new Regex(@"<#@\s*?" + directive + @"\s+?.*?" + paramName + @"=""(?<paramValue>.*?)"".*?#>");

            foreach (var line in templateText)
            {
                Match m = regex.Match(line);
                Group g = m.Groups["paramValue"];
                if (g != null && g.Success)
                    return g.Value;
            }

            return null;
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

        public static readonly char[] _pathSeparators = { Util.WindowsSeparator, Util.UnixSeparator };
        internal static readonly char[] WildcardCharacters = { '*', '?' };

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
                            if (Util.IsRunningInMono() &&
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

        [System.Runtime.InteropServices.DllImport("msvcrt.dll", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private static extern int memcmp(byte[] b1, byte[] b2, long count);

        private static bool AreStreamsEqual(Stream stream1, Stream stream2)
        {
            const int BufferSize = 4096;
            var buffer1 = new byte[BufferSize];
            var buffer2 = new byte[BufferSize];

            stream1.Position = 0;
            stream2.Position = 0;

            while (true)
            {
                int count1 = stream1.Read(buffer1, 0, BufferSize);
                int count2 = stream2.Read(buffer2, 0, BufferSize);

                if (count1 != count2)
                    return false;

                if (count1 == 0)
                    return true;

                if (memcmp(buffer1, buffer2, count1) != 0)
                    return false;
            }
        }

        public static bool IsFileDifferent(FileInfo file, Stream stream, bool compareEndLineCharacters = false)
        {
            if (!file.Exists)
                return true;

            using (var fstream = file.OpenRead())
            {
                if (compareEndLineCharacters)
                {
                    return !AreStreamsEqual(stream, fstream);
                }
                else
                {
                    stream.Position = 0;
                    fstream.Position = 0;

                    StreamReader streamReader = new StreamReader(stream);
                    StreamReader fstreamReader = new StreamReader(fstream);

                    int c0 = streamReader.Read();
                    int c1 = fstreamReader.Read();

                    while (c0 != -1 || c1 != -1)
                    {
                        // skip end of line for comparison
                        while ((char)c0 == '\n' || (char)c0 == '\r')
                            c0 = streamReader.Read();

                        while ((char)c1 == '\n' || (char)c1 == '\r')
                            c1 = fstreamReader.Read();

                        if (c0 != c1)
                            return true;

                        c0 = streamReader.Read();
                        c1 = fstreamReader.Read();
                    }
                    return false;
                }
            }
        }

        // Note: We don't need the value but the ConcurrentBag interface is bad and is allowing duplicate keys and is more intended to be
        // like an unordered queue.
        private static ConcurrentDictionary<string, DateTime> s_writtenFiles = new ConcurrentDictionary<string, DateTime>(StringComparer.InvariantCultureIgnoreCase);

        public static GenerationOutput FileWriteIfDifferent(string outputFilePath, MemoryStream stream, GenerationOutput generationOutput = null)
        {
            if (generationOutput == null)
                generationOutput = new GenerationOutput();

            if (FileWriteIfDifferent(new FileInfo(outputFilePath), stream))
                generationOutput.Generated.Add(outputFilePath);
            else
                generationOutput.Skipped.Add(outputFilePath);

            return generationOutput;
        }

        public static bool FileWriteIfDifferent(FileInfo file, MemoryStream stream)
        {
            return Builder.Instance.Context.WriteGeneratedFile(null, file, stream);
        }

        internal static bool FileWriteIfDifferentInternal(FileInfo file, MemoryStream stream, bool bypassAutoCleanupDatabase = false)
        {
            if (!bypassAutoCleanupDatabase)
                s_writtenFiles.TryAdd(file.FullName, DateTime.Now);

            if (file.Exists)
            {
                if (!IsFileDifferent(file, stream, true))
                    return false;

                if (file.IsReadOnly)
                    file.IsReadOnly = false;
            }
            else
            {
                // make sure target directory exist
                if (!file.Directory.Exists)
                    file.Directory.Create();
            }

            // write the file
            using (FileStream outStream = file.Open(FileMode.Create))
            {
                outStream.Write(stream.ToArray(), 0, (int)stream.Length);
            }

            return true;
        }

        public static void LogWrite(string msg, params object[] args)
        {
            string message = args.Length > 0 ? string.Format(msg, args) : msg;

            Console.WriteLine(message);
            if (Debugger.IsAttached)
                Trace.WriteLine(message);
        }

        public static List<string> FilesAlternatesAutoCleanupDBSuffixes = new List<string>(); // The alternates db suffixes using by other context
        public static string FilesAutoCleanupDBPath = string.Empty;
        public static string FilesAutoCleanupDBSuffix = string.Empty;   // Current auto-cleanup suffix for the database.
        internal static bool s_forceFilesCleanup = false;
        internal static string s_overrideFilesAutoCleanupDBPath;
        public static bool FilesAutoCleanupActive = false;
        public static TimeSpan FilesAutoCleanupDelay = TimeSpan.Zero;
        public static HashSet<string> FilesToBeExplicitlyRemovedFromDB = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        public static HashSet<string> FilesAutoCleanupIgnoredEndings = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        private const string s_filesAutoCleanupDBPrefix = "sharpmakeautocleanupdb";
        private enum DBVersion { Version = 2 };

        private static Dictionary<string, DateTime> ReadCleanupDatabase(string databaseFilename)
        {
            Dictionary<string, DateTime> dbFiles = null;
            if (File.Exists(databaseFilename))
            {
                try
                {
                    // Read database - This is simply a simple binary file containing the list of file and a version number.
                    using (Stream readStream = new FileStream(databaseFilename, FileMode.Open, FileAccess.Read, FileShare.None))
                    using (BinaryReader binReader = new BinaryReader(readStream))
                    {
                        // Validate version number
                        int version = binReader.ReadInt32();
                        if (version == (int)DBVersion.Version)
                        {
                            // Read the list of files.
                            IFormatter formatter = new BinaryFormatter();
                            var tmpDbFiles = (Dictionary<string, DateTime>)formatter.Deserialize(readStream);
                            dbFiles = tmpDbFiles.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.InvariantCultureIgnoreCase);
                        }
                        else if (version == 1)
                        {
                            IFormatter formatter = new BinaryFormatter();
                            ConcurrentDictionary<string, bool> dbFilesV1 = (ConcurrentDictionary<string, bool>)formatter.Deserialize(readStream);
                            DateTime now = DateTime.Now;
                            dbFiles = dbFilesV1.ToDictionary(kvp => kvp.Key, kvp => now);
                        }

                        readStream.Close();
                    }
                }
                catch (SerializationException)
                {
                    // File is likely corrupted.
                    // This is no big deal except that cleanup won't occur.
                    dbFiles = null;
                }
            }
            return dbFiles;
        }

        private static string GetDatabaseFilename(string dbSuffix)
        {
            if (!string.IsNullOrWhiteSpace(s_overrideFilesAutoCleanupDBPath))
                return s_overrideFilesAutoCleanupDBPath;

            string databaseFilename = Path.Combine(FilesAutoCleanupDBPath, $"{s_filesAutoCleanupDBPrefix}{dbSuffix}.bin");
            return databaseFilename;
        }


        /// <summary>
        /// This method is used to execute files auto-cleanup. Which means deleting files that are no longer saved.
        /// When auto-cleanup is active, Sharpmake will save file paths of saved files into a simple database and then when we re-execute sharpmake
        /// it will delete old files(files that are in the database but not written in the current process).
        /// </summary>
        ///
        /// <remarks>
        /// - Auto cleanup is disabled by default and must be enabled explicitly.
        /// - You can have many auto cleanup database by setting the AutoCleanupDBSuffix to a string that identify your sharpmake running context.
        /// This is useful when you execute sharpmake with more than one setup configuration. For example on ACE, we have two setups:
        /// - Engine and Tools and both are running different scripts but have the same .sharpmake file entry point. In that case we would
        /// set the suffix with different value depending on the context we are running sharpmake with.
        /// - Generally you should also disable the cleanup when running with changelist filters(used typically by Submit Assistant).
        /// </remarks>
        ///
        /// <example>
        /// This is the way the auto-cleanup is configured on ACE. This code is in our main.
        /// Util.AutoCleanupDBPath = sharpmakeFileDirectory;
        /// Util.FilesAutoCleanupActive = Arguments.Filter != Filter.Changelist && arguments.Builder.BlobOnly == false;
        /// if (Arguments.GenerateTools)
        ///    Util.AutoCleanupDBSuffix = "_tools";
        /// </example>
        public static void ExecuteFilesAutoCleanup()
        {
            if (!FilesAutoCleanupActive && !s_forceFilesCleanup)
                return; // Auto cleanup not active. Nothing to do.

            if (string.IsNullOrWhiteSpace(s_overrideFilesAutoCleanupDBPath) && !Directory.Exists(FilesAutoCleanupDBPath))
                throw new Exception($"Unable to find directory {FilesAutoCleanupDBPath} used to store auto-cleanup database. Is proper path set?");

            string databaseFilename = GetDatabaseFilename(FilesAutoCleanupDBSuffix);
            Dictionary<string, DateTime> dbFiles = ReadCleanupDatabase(databaseFilename);

            // Note: We must take into account all databases when doing the cleanup otherwise we might end up deleting files still used in other contexts.
            List<Dictionary<string, DateTime>> alternateDatabases = new List<Dictionary<string, DateTime>>();
            foreach (string alternateDBSuffix in FilesAlternatesAutoCleanupDBSuffixes)
            {
                string alternateDatabaseFilename = GetDatabaseFilename(alternateDBSuffix);
                Dictionary<string, DateTime> alternateDBFiles = ReadCleanupDatabase(alternateDatabaseFilename);
                if (alternateDBFiles != null)
                    alternateDatabases.Add(alternateDBFiles);
            }

            Dictionary<string, DateTime> newDbFiles = new Dictionary<string, DateTime>(s_writtenFiles, StringComparer.InvariantCultureIgnoreCase);

            if (dbFiles != null)
            {
                // Deleting all files that are no longer used.
                DateTime now = DateTime.Now;
                foreach (KeyValuePair<string, DateTime> filenameDate in dbFiles)
                {
                    if (s_forceFilesCleanup)
                    {
                        if (!File.Exists(filenameDate.Key))
                            continue;

                        LogWrite(@"Force deleting '{0}'", filenameDate.Key);
                        if (!TryDeleteFile(filenameDate.Key, removeIfReadOnly: true))
                        {
                            // Failed to delete the file... Keep it for now... Maybe later we will be able to delete it!
                            LogWrite(@"Failed to delete '{0}'", filenameDate.Key);
                        }
                        continue;
                    }

                    if (newDbFiles.ContainsKey(filenameDate.Key))
                        continue;

                    if (FilesAutoCleanupIgnoredEndings.Any(x => filenameDate.Key.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    TimeSpan diff = now - filenameDate.Value;
                    if (diff >= FilesAutoCleanupDelay)
                    {
                        if (!File.Exists(filenameDate.Key))
                            continue;

                        bool foundFileInAlternateDatabase = false;
                        // Check alternateDatabases files
                        foreach (var alternateDB in alternateDatabases)
                        {
                            if (alternateDB.ContainsKey(filenameDate.Key))
                            {
                                // Another database still uses the file. Simply remove the file from the current database and don't attempt to delete this file.
                                foundFileInAlternateDatabase = true;
                                break;
                            }
                        }
                        if (!foundFileInAlternateDatabase)
                        {
                            if (!FilesToBeExplicitlyRemovedFromDB.Contains(filenameDate.Key))
                            {
                                LogWrite(@"Deleting old file: {0}", filenameDate.Key);
                                if (!TryDeleteFile(filenameDate.Key))
                                {
                                    // Failed to delete the file... Keep it for now... Maybe later we will be able to delete it!
                                    LogWrite(@"Failed to delete {0}. Keep it in the database.", filenameDate.Key);
                                    newDbFiles.Add(filenameDate.Key, filenameDate.Value);
                                }
                            }
                        }
                    }
                    else
                    {
                        LogWrite(@"Skip deleting old file (delayed): {0}", filenameDate.Key);
                        newDbFiles.Add(filenameDate.Key, filenameDate.Value);
                    }
                }
            }

            // Write database if needed
            if (newDbFiles.Count > 0)
            {
                using (Stream writeStream = new FileStream(databaseFilename, FileMode.Create, FileAccess.Write, FileShare.None))
                using (BinaryWriter binWriter = new BinaryWriter(writeStream))
                {
                    // Write version number
                    int version = (int)DBVersion.Version;
                    binWriter.Write(version);
                    binWriter.Flush();

                    // Write the list of files.
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(writeStream, newDbFiles);
                }
            }
            else
            {
                TryDeleteFile(databaseFilename);
            }
        }

        public static string WinFormSubTypesDbPath = string.Empty;
        private static readonly string s_winFormSubTypesDbPrefix = "winformssubtypesdb";

        public static string GetWinFormSubTypeDbPath()
        {
            return Path.Combine(WinFormSubTypesDbPath, $@"{s_winFormSubTypesDbPrefix}.bin");
        }

        public static void SerializeAllCsprojSubTypes(object allCsProjSubTypes)
        {
            // If DbPath is not specify, do not save C# subtypes information
            if (string.IsNullOrEmpty(WinFormSubTypesDbPath))
                return;

            if (!Directory.Exists(WinFormSubTypesDbPath))
                Directory.CreateDirectory(WinFormSubTypesDbPath);

            string winFormSubTypesDbFullPath = GetWinFormSubTypeDbPath();

            using (Stream writeStream = new FileStream(winFormSubTypesDbFullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(writeStream, allCsProjSubTypes);
            }
        }

        public static object DeserializeAllCsprojSubTypes()
        {
            string winFormSubTypesDbFullPath = GetWinFormSubTypeDbPath();

            if (!File.Exists(winFormSubTypesDbFullPath))
                return null;

            try
            {
                using (Stream readStream = new FileStream(winFormSubTypesDbFullPath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    return binaryFormatter.Deserialize(readStream);
                }
            }
            catch
            {
                TryDeleteFile(winFormSubTypesDbFullPath);
            }
            return null;
        }

        public static bool TryDeleteFile(string filename, bool removeIfReadOnly = false)
        {
            try
            {
                var fileInfo = new FileInfo(filename);
                if (fileInfo.Exists)
                {
                    if (fileInfo.IsReadOnly)
                    {
                        if (!removeIfReadOnly)
                            return false;
                        fileInfo.IsReadOnly = false;
                    }
                    File.Delete(filename);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static StackFrame GetStackFrameTopMostTypeOf(Type type, bool allowEmptyFileName = true)
        {
            if (type.IsGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }

            StackTrace stackTrace = new StackTrace(true);
            if (type.IsConstructedGenericType)
                type = type.GetGenericTypeDefinition();
            for (int i = stackTrace.FrameCount - 1; i >= 0; --i)
            {
                StackFrame stackFrame = stackTrace.GetFrame(i);
                MethodBase method = stackFrame.GetMethod();

                if (method.DeclaringType == type && (allowEmptyFileName || !string.IsNullOrEmpty(stackFrame.GetFileName())))
                    return stackFrame;
            }
            return null;
        }

        public static bool GetStackSourceFileTopMostTypeOf(Type type, out string sourceFile)
        {
            // If the sought StackFrame was found, don't return it if its associated file name is unknown.
            // This could happen in Mono when Sharpmake is invoked with /generateDebugSolution. In that case,
            // sharpmake_sharpmake's ctor would be looked up and found in the call stack, but the StackFrame would
            // refer to a null file name. On Windows, however, sharpmake_sharpmake's ctor simply does not appear in
            // the call stack (as though its call is implicit or omitted)...
            StackFrame projectStackframe = GetStackFrameTopMostTypeOf(type, allowEmptyFileName: false);
            if (projectStackframe != null)
            {
                sourceFile = projectStackframe.GetFileName();

                // Class does not declare a constructor, so impossible to guess file name
                if (sourceFile == null)
                {
                    throw new Error(
                        "The type \"" + type + "\" does not declare a constructor, " +
                        "please add one to allow Sharpmake to detect source file from construction callstack.");
                }

                return true;
            }

            if (type.BaseType != null)
            {
                // Due to compiler optimizations, sometimes the constructor can be inlined... To avoid crashes in that
                // case we search for the base class instead
                return GetStackSourceFileTopMostTypeOf(type.BaseType, out sourceFile);
            }

            sourceFile = null;
            return false;
        }

        public static FileInfo GetCurrentSharpmakeFileInfo()
        {
            StackTrace stackTrace = new StackTrace(true);
            for (int i = 0; i < stackTrace.FrameCount - 1; ++i)
            {
                StackFrame stackFrame = stackTrace.GetFrame(i);
                MethodBase method = stackFrame.GetMethod();
                if (method.DeclaringType == typeof(Util))
                {
                    stackFrame = stackTrace.GetFrame(i + 1);
                    return new FileInfo(stackFrame.GetFileName());
                }
            }
            throw new Error("error in Sharpmake.Util.GetThisSharpmakeFile()");
        }

        /// <summary>
        ///  Search the call stack and return the info about the topmost frame from a file that is edited by the user (.sharpmake or .sharpmake.cs)
        /// </summary>
        /// <returns> Caller info in a format clickable in the output console if a .sharpmake frame is found. Otherwise return an empty string. </returns>
        public static string GetCurrentSharpmakeCallerInfo()
        {
            StackTrace stackTrace = new StackTrace(true);
            foreach (var stackFrame in stackTrace.GetFrames())
            {
                if (stackFrame.GetFileName() != null &&
                    (stackFrame.GetFileName().EndsWith(".sharpmake", StringComparison.OrdinalIgnoreCase) ||
                     stackFrame.GetFileName().EndsWith(".sharpmake.cs", StringComparison.OrdinalIgnoreCase)))
                {
                    return FormatCallerInfo(stackFrame.GetFileName(), stackFrame.GetFileLineNumber());
                }
            }
            return "";
        }

        /// <summary>
        ///  Lookup the callstack and return the info about the 2nd topmost frame (topmost being where this method is called from) .
        /// </summary>
        /// <returns> Caller info in a format clickable in the output console.</returns>
        public static string GetCallerInfoFromStack()
        {
            StackTrace stackTrace = new StackTrace(true);
            var stackFrame = stackTrace.GetFrames()[2];
            if (stackFrame != null && stackFrame.GetFileName() != null)
            {
                return FormatCallerInfo(stackFrame.GetFileName(), stackFrame.GetFileLineNumber());
            }
            return "";
        }

        /// <summary>
        /// Build a fake Guid from input string value.
        /// </summary>
        /// <param name="value">String value to generate guid from.</param>
        /// <returns></returns>
        public static Guid BuildGuid(string value)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider provider = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] md5 = provider.ComputeHash(Encoding.ASCII.GetBytes(value));
            return new Guid(md5);
        }

        private class SameObjectComparer : EqualityComparer<object>
        {
            public override bool Equals(object x, object y)
            {
                return x.Equals(y);
            }

            public override int GetHashCode(object obj)
            {
                return obj.GetHashCode();
            }
        }

        public static string MakeDifferenceString(object obj1, object obj2)
        {
            var messageWriter = new StringWriter();
            MakeDifferenceString(obj1, obj2, messageWriter, "", new HashSet<object>(new SameObjectComparer()));
            return messageWriter.ToString();
        }

        public static void MakeDifferenceString(object obj1, object obj2, TextWriter messageWriter, string currentObjPath, HashSet<object> visited)
        {
            if (visited.Contains(obj1))
                return;
            visited.Add(obj1);
            var type = obj1.GetType();
            foreach (PropertyInfo propertyInfo in type.GetProperties())
            {
                if (!propertyInfo.CanRead)
                    continue;
                if (propertyInfo.PropertyType.IsAbstract)
                    continue;
                object firstValue = propertyInfo.GetValue(obj1, null);
                object secondValue = propertyInfo.GetValue(obj2, null);
                var propertyType = propertyInfo.PropertyType;
                if (propertyType.IsValueType || propertyType.IsPrimitive || propertyType == typeof(string) || firstValue == null || secondValue == null)
                {
                    if (!object.Equals(firstValue, secondValue))
                    {
                        messageWriter.WriteLine(propertyInfo.Name + ": \"" + firstValue + "\" and \"" + secondValue + "\"");
                    }
                }
                else
                {
                    var interfaces = propertyInfo.PropertyType.GetInterfaces();
                    if (interfaces.Contains(typeof(IEnumerable)))
                    {
                        if (SequenceCompare(firstValue as IEnumerable, secondValue as IEnumerable) != 0)
                        {
                            messageWriter.WriteLine(propertyInfo.Name + ": \"" + firstValue + "\" and \"" + secondValue + "\"");
                        }
                    }
                    else
                    {
                        MakeDifferenceString(firstValue, secondValue, messageWriter, currentObjPath + propertyInfo.Name + ".", visited);
                    }
                }
            }
        }

        private static int SequenceCompare(IEnumerable source1, IEnumerable source2)
        {
            var iterator1 = source1.GetEnumerator();
            var iterator2 = source2.GetEnumerator();
            while (true)
            {
                bool next1 = iterator1.MoveNext();
                bool next2 = iterator2.MoveNext();
                if (!next1 && !next2) // Both sequences finished
                {
                    return 0;
                }
                if (!next1) // Only the first sequence has finished
                {
                    return -1;
                }
                if (!next2) // Only the second sequence has finished
                {
                    return 1;
                }
                // Both are still going, compare current elements
                int comparison = Comparer.Default.Compare(iterator1.Current.ToString(), iterator2.Current.ToString());
                // If elements are non-equal, we're done
                if (comparison != 0)
                {
                    return comparison;
                }
            }
        }
        public static string JoinStrings(ICollection<string> container, string separator, bool escapeXml = false)
        {
            return JoinStrings(container, separator, "", "", escapeXml);
        }

        public static string JoinStrings(ICollection<string> container, string separator, string prefix, bool escapeXml = false)
        {
            return JoinStrings(container, separator, prefix, "", escapeXml);
        }

        public static string JoinStrings(ICollection<string> container, string separator, string prefix, string suffix, bool escapeXml = false)
        {
            int count = container.Count;
            StringBuilder builder = new StringBuilder(count * 128);
            bool isFirst = true;
            foreach (string str in container)
            {
                if (!isFirst)
                    builder.Append(separator);
                else
                    isFirst = false;

                string value = str;
                if (escapeXml)
                    value = Util.EscapeXml(value);
                builder.Append(prefix);
                builder.Append(value);
                builder.Append(suffix);
            }
            return builder.ToString();
        }

        public static string EscapeXml(string value)
        {
            // Visual Studio only escapes what it absolutely has to for XML to be valid.

            // escape the < and >
            value = value.Replace("<", "&lt;").Replace(">", "&gt;").ToString();

            // any & that is not part of an escape needs to be escaped with &amp;
            value = Regex.Replace(value, @"(\&(?![a-zA-Z0-9#]+;))", "&amp;");

            return value;
        }

        /**
        * A simple json serializer.
        * Does not serialize objects properties using reflection, data must be prepared by the caller.
        * Supported types : IEnumerable, IDictionnary, string and system value types.
        */
        public class JsonSerializer : IDisposable
        {
            public const string NullString = "null";

            // Must escape " / \ and control characters.
            // The website : http://json.org/
            // The rfc : https://tools.ietf.org/html/rfc8259#section-7
            public static string EscapeJson(string value, bool quote = false, string nullValue = "null")
            {
                if (value == null)
                    return nullValue;

                StringBuilder sb = new StringBuilder(value.Length);

                if (quote)
                    sb.Append('"');

                foreach (char c in value)
                {
                    switch (c)
                    {
                        case '\\':
                        case '/':
                        case '"':
                            sb.Append('\\');
                            sb.Append(c);
                            break;
                        case '\b':
                            sb.Append("\\b");
                            break;
                        case '\f':
                            sb.Append("\\f");
                            break;
                        case '\n':
                            sb.Append("\\n");
                            break;
                        case '\r':
                            sb.Append("\\r");
                            break;
                        case '\t':
                            sb.Append("\\t");
                            break;
                        default:
                            // Control characters range from 0x0000 to 0x001F (0x0020 is ' '/space)
                            // The \n \t above also fall into that range, but json accepts them in the friendlier \\n \\t form.
                            // Others are 'escaped' by printing their code point in hex.
                            if (c < 0x0020)
                            {
                                // The Json format specifies 4 digit hex : \uXXXX
                                sb.AppendFormat("\\u{0:X4}", (ushort)c);
                            }
                            else
                            {
                                sb.Append(c);
                            }
                            break;
                    }
                }

                if (quote)
                    sb.Append('"');

                return sb.ToString();
            }

            private static bool IsFloat(object o)
            {
                return o is float || o is double || o is decimal;
            }

            private static bool IsInteger(object o)
            {
                return o is sbyte || o is short || o is int || o is long ||
                       o is byte || o is ushort || o is uint || o is ulong;
            }

            private TextWriter _writer;
            private ISet<IEnumerable> _parents;

            private string IndentationString => new string('\t', _parents.Count);
            public bool IsOutputFormatted { get; set; }

            public JsonSerializer(TextWriter writer)
            {
                _writer = writer;
                _parents = new HashSet<IEnumerable>();
            }

            public void Flush()
            {
                _writer.Flush();
            }

            public void Dispose()
            {
                _writer.Dispose();
            }

            public void Serialize(object value)
            {
                if (value == null)
                {
                    _writer.Write(NullString);
                }
                else if (value is string || value is char)
                {
                    // String is IEnumerable, avoid serializing it as an array!
                    _writer.Write(EscapeJson(value.ToString(), quote: true));
                }
                else if (value is IDictionary)
                {
                    // IDictionary is IEnumerable, avoid serializing it as an array!
                    SerializeDictionary((IDictionary)value);
                }
                else if (value is IEnumerable)
                {
                    SerializeArray((IEnumerable)value);
                }
                else if (value is bool)
                {
                    _writer.Write(value.ToString().ToLower());
                }
                else if (IsFloat(value))
                {
                    // This *should* be safe without Escaping
                    _writer.Write(Convert.ToDouble(value).ToString(CultureInfo.InvariantCulture));
                }
                else if (IsInteger(value))
                {
                    // This *should* be safe without Escaping
                    _writer.Write(EscapeJson(value.ToString().ToLower()));
                }
                else
                {
                    throw new ArgumentException(string.Format("Unsupported type '{0}'", value.GetType()));
                }
            }

            private void SerializeArray(IEnumerable array)
            {
                SerializeSequence<object>(array, Tuple.Create('[', ']'), Serialize);
            }

            private void SerializeDictionary(IDictionary dict)
            {
                SerializeSequence<DictionaryEntry>(dict, Tuple.Create('{', '}'), e =>
                {
                    if (!(e.Key is string))
                        throw new InvalidDataException(string.Format("Dictionary key '{0}' is not a string.", e.Key));

                    Serialize(e.Key);
                    _writer.Write(':');
                    Serialize(e.Value);
                });
            }

            private void SerializeSequence<T>(IEnumerable sequence, Tuple<char, char> delimiters, Action<T> serializeElement)
            {
                if (_parents.Contains(sequence))
                    throw new InvalidDataException("Cycle detected during json serialization.");

                _writer.Write(delimiters.Item1);
                _parents.Add(sequence);

                // IDictionary returns different enumerators depending on which interface GetEnumerator called from.
                // IDictionary.GetEnumerator enumerates DictionaryEntry 
                // IEnumerable.GetEnumerator enumerates KeyValuePair<>
                bool first = true;
                IEnumerator enumerator = (sequence as IDictionary)?.GetEnumerator() ?? sequence.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (!first)
                        _writer.Write(',');

                    // Write each element on its own line
                    if (IsOutputFormatted)
                    {
                        _writer.WriteLine();
                        _writer.Write(IndentationString);
                    }

                    serializeElement((T)enumerator.Current);

                    first = false;
                }

                _parents.Remove(sequence);

                // Extra newline after the last element, puts the closing delimiter on its own line
                if (IsOutputFormatted)
                {
                    _writer.WriteLine();
                    _writer.Write(IndentationString);
                }

                _writer.Write(delimiters.Item2);
            }
        }

        public static bool CreateSymbolicLink(string source, string target, bool isDirectory)
        {
            bool success = false;
            try
            {
                // In case the file is marked as readonly
                if (File.Exists(source))
                {
                    File.SetAttributes(source, FileAttributes.Normal);
                    File.Delete(source);
                }
                else if (Directory.Exists(source))
                {
                    Directory.Delete(source);
                }

                int releaseId = int.Parse(GetRegistryLocalMachineSubKeyValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "0"));

                int flags = isDirectory ? SYMBOLIC_LINK_FLAG_DIRECTORY : SYMBOLIC_LINK_FLAG_FILE;
                if (releaseId >= 1703) // Verify that the Windows build is equal or above 1703, as SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE was introduced at that version. Using it on older version will cause an error 87 and symlinks won't be created
                    flags |= SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE;

                success = CreateSymbolicLink(source, target, flags);
            }
            catch { }
            return success;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);
        private const int SYMBOLIC_LINK_FLAG_FILE = 0x0;
        private const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;
        private const int SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 0x2;

        public static bool IsSymbolicLink(string path)
        {
            var fileInfo = new FileInfo(path);
            // If the file has one reparse point, we assume it is a symlink
            return fileInfo.Exists && fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }

        public static string GetEnvironmentVariable(string variableName, string fallbackValue, ref string outputValue, bool silent = false)
        {
            if (outputValue == null)
            {
                string tmp = Environment.GetEnvironmentVariable(variableName);
                if (string.IsNullOrEmpty(tmp))
                {
                    if (!silent)
                        LogWrite("Environment variable [" + variableName + "] is not set, fallback to default: " + fallbackValue);
                    outputValue = fallbackValue;
                }
                else
                    outputValue = tmp;
            }
            return outputValue;
        }

        private static bool? s_isVisualStudio2017Installed = null;
        public static bool IsVisualStudio2017Installed()
        {
            if (!s_isVisualStudio2017Installed.HasValue)
                s_isVisualStudio2017Installed = IsVisualStudioInstalled(DevEnv.vs2017);

            return s_isVisualStudio2017Installed.Value;
        }

        private static bool? s_isVisualStudio2015Installed = null;
        public static bool IsVisualStudio2015Installed()
        {
            if (!s_isVisualStudio2015Installed.HasValue)
                s_isVisualStudio2015Installed = IsVisualStudioInstalled(DevEnv.vs2015);

            return s_isVisualStudio2015Installed.Value;
        }

        private static bool? s_isVisualStudio2013Installed = null;
        public static bool IsVisualStudio2013Installed()
        {
            if (!s_isVisualStudio2013Installed.HasValue)
                s_isVisualStudio2013Installed = IsVisualStudioInstalled(DevEnv.vs2013);

            return s_isVisualStudio2013Installed.Value;
        }

        private static bool? s_isVisualStudio2012Installed = null;
        public static bool IsVisualStudio2012Installed()
        {
            if (!s_isVisualStudio2012Installed.HasValue)
                s_isVisualStudio2012Installed = IsVisualStudioInstalled(DevEnv.vs2012);

            return s_isVisualStudio2012Installed.Value;
        }

        private static bool? s_isVisualStudio2010Installed = null;
        public static bool IsVisualStudio2010Installed()
        {
            if (!s_isVisualStudio2010Installed.HasValue)
                s_isVisualStudio2010Installed = IsVisualStudioInstalled(DevEnv.vs2010);

            return s_isVisualStudio2010Installed.Value;
        }

        private static bool IsVisualStudioInstalled(DevEnv devEnv)
        {
            string registryKeyString = string.Format(
                @"SOFTWARE{0}\Microsoft\VisualStudio\SxS\VS7",
                Environment.Is64BitProcess ? @"\Wow6432Node" : string.Empty
            );

            string key = string.Empty;
            try
            {
                using (RegistryKey localMachineKey = Registry.LocalMachine.OpenSubKey(registryKeyString))
                    key = (localMachineKey != null) ? (string)localMachineKey.GetValue(devEnv.GetVisualVersionString()) : null;
            }
            catch { }

            return !string.IsNullOrEmpty(key);
        }

        public static string GetDefaultLLVMInstallDir()
        {
            string registryKeyString = string.Format(
                @"SOFTWARE{0}\LLVM\LLVM",
                Environment.Is64BitProcess ? @"\Wow6432Node" : string.Empty
            );

            return GetRegistryLocalMachineSubKeyValue(registryKeyString, null, @"C:\Program Files\LLVM"); // null to get default
        }

        public static string GetClangVersionFromLLVMInstallDir(string llvmInstallDir)
        {
            if (!DirectoryExists(llvmInstallDir))
                throw new Error($"Couldn't find {llvmInstallDir} to lookup version");

            string libDir = Path.Combine(llvmInstallDir, "lib", "clang");

            var versionFolder = DirectoryGetDirectories(libDir);
            if (versionFolder.Length == 0)
                throw new Error($"Couldn't find a version number folder for clang in {llvmInstallDir}");

            if (versionFolder.Length != 1)
                throw new NotImplementedException($"More than one version folder found in {llvmInstallDir}, the code doesn't handle that (yet).");

            return Path.GetFileName(versionFolder[0]);
        }

        public class VsInstallation
        {
            public VsInstallation(ISetupInstance2 setupInstance)
            {
                Version = new Version(setupInstance.GetInstallationVersion());

                var catalog = setupInstance as ISetupInstanceCatalog;
                IsPrerelease = catalog?.IsPrerelease() ?? false;

                InstallationPath = setupInstance.GetInstallationPath();

                if ((setupInstance.GetState() & InstanceState.Registered) == InstanceState.Registered)
                {
                    ProductID = setupInstance.GetProduct().GetId();
                    Components = (from package in setupInstance.GetPackages()
                                  where string.Equals(package.GetType(), "Component", StringComparison.OrdinalIgnoreCase)
                                  select package.GetId()).ToArray();
                    Workloads = (from package in setupInstance.GetPackages()
                                 where string.Equals(package.GetType(), "Workload", StringComparison.OrdinalIgnoreCase)
                                 select package.GetId()).ToArray();
                }
            }

            public Version Version { get; }

            public string InstallationPath { get; }

            public bool IsPrerelease { get; }

            /// <summary>
            /// The full list of products can be found here: https://docs.microsoft.com/en-us/visualstudio/install/workload-and-component-ids
            /// </summary>
            public string ProductID { get; } = null;

            /// <summary>
            /// This can be used to check and limit by specific installed workloads.
            /// 
            /// What is a Workload?
            /// In the VS installer, a 'Workload' is a section that you see in the UI such as 'Desktop development with C++' or '.NET desktop development'.
            /// 
            /// The full list of products is here: https://docs.microsoft.com/en-us/visualstudio/install/workload-and-component-ids
            /// 
            /// For each product, clicking it will bring up a page of all of the possible Workloads.
            /// For example: https://docs.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-professional
            /// </summary>
            public string[] Workloads { get; } = new string[] { };

            /// <summary>
            /// This can be used to check and limit by specific installed components.
            /// What is a Component?
            /// In the Visual Studio Installer, the 'Components' are individual components associated with each Workload (and some just on the side), 
            /// that you can see in the Summary on the right.
            /// Each workflow contains a number of mandatory components, but also a list of optional ones.
            /// An example would be: 'NuGet package manager' or 'C++/CLI support'.
            /// 
            /// The full list of products is here: https://docs.microsoft.com/en-us/visualstudio/install/workload-and-component-ids
            /// 
            /// For each product, clicking it will bring up a page of all of the possible Workloads.
            /// For example: https://docs.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-professional
            /// </summary>
            public string[] Components { get; } = new string[] { };
        }

        public static List<VsInstallation> s_VisualStudioInstallations { get; private set; } = null;

        private static object s_vsInstallScanLock = new object();
        public static List<VsInstallation> GetVisualStudioInstalledVersions()
        {
            lock (s_vsInstallScanLock)
            {
                if (s_VisualStudioInstallations != null)
                    return s_VisualStudioInstallations;

                var installations = new List<VsInstallation>();

                try
                {
                    var query = (ISetupConfiguration2)new SetupConfiguration();
                    var e = query.EnumAllInstances();

                    int fetched;
                    var instances = new ISetupInstance[1];
                    do
                    {
                        e.Next(1, instances, out fetched);
                        if (fetched > 0)
                        {
                            var setupInstance2 = (ISetupInstance2)instances[0];
                            if ((setupInstance2.GetState() & InstanceState.Local) == InstanceState.Local)
                            {
                                installations.Add(new VsInstallation(setupInstance2));
                            }
                        }
                    } while (fetched > 0);
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // Ignore
                }

                s_VisualStudioInstallations = installations;
                return s_VisualStudioInstallations;
            }
        }

        /// <summary>
        /// The supported visual studio products, in order by priority in which Sharpmake will choose them.
        /// We want to block products like the standalone Team Explorer, which is in the Visual Studio
        /// family yet isn't a variant of Visual Studio proper.
        ///
        /// The list of Product IDs can be found here: https://docs.microsoft.com/en-us/visualstudio/install/workload-and-component-ids
        /// </summary>
        private static readonly string[] s_supportedVisualStudioProducts = {
            "Microsoft.VisualStudio.Product.Enterprise",
            "Microsoft.VisualStudio.Product.Professional",
            "Microsoft.VisualStudio.Product.Community",
            "Microsoft.VisualStudio.Product.BuildTools"
        };

        private class VisualStudioVersionSorter : IComparer<VsInstallation>
        {
            public int Compare(VsInstallation x, VsInstallation y)
            {
                // Order by Product ID priority (the order they appear in s_supportedVisualStudioProducts).
                int xProductIndex = s_supportedVisualStudioProducts.IndexOf(x.ProductID, StringComparison.OrdinalIgnoreCase);
                int yProductIndex = s_supportedVisualStudioProducts.IndexOf(y.ProductID, StringComparison.OrdinalIgnoreCase);

                int versionComparison = xProductIndex.CompareTo(yProductIndex);
                if (versionComparison != 0)
                    return versionComparison;

                // If they have the same Product ID, then compare their versions and return the highest one.
                return y.Version.CompareTo(x.Version); // Swap x and y so that the comparison is inversed (higher values first).
            }
        }

        public static List<VsInstallation> GetVisualStudioInstallationsFromQuery(
            DevEnv visualVersion,
            bool allowPrereleaseVersions = false,
            string[] requiredComponents = null,
            string[] requiredWorkloads = null
        )
        {
            // Fetch all installed products
            var installedVersions = GetVisualStudioInstalledVersions();

            // Limit to our major version + the supported products, and order by priority.
            int majorVersion = visualVersion.GetVisualMajorVersion();

            var candidates = installedVersions.Where(i =>
                    i.Version.Major == majorVersion
                    && (!i.IsPrerelease || allowPrereleaseVersions)
                    && s_supportedVisualStudioProducts.Contains(i.ProductID, StringComparer.OrdinalIgnoreCase)
                    && (requiredComponents == null || !requiredComponents.Except(i.Components).Any())
                    && (requiredWorkloads == null || !requiredWorkloads.Except(i.Workloads).Any()))
                .OrderBy(x => x, new VisualStudioVersionSorter()).ToList();

            return candidates;
        }

        public static string GetVisualStudioInstallPathFromQuery(
            DevEnv visualVersion,
            bool allowPrereleaseVersions = false,
            string[] requiredComponents = null,
            string[] requiredWorkloads = null
        )
        {
            if (IsRunningInMono())
                return null;

            var vsInstallations = GetVisualStudioInstallationsFromQuery(visualVersion, allowPrereleaseVersions, requiredComponents, requiredWorkloads);
            VsInstallation priorityInstallation = vsInstallations.FirstOrDefault();
            return priorityInstallation != null ? SimplifyPath(priorityInstallation.InstallationPath) : null;
        }

        /// <summary>
        /// Generate a pseudo Guid base on relative path from the Project GuidReference path to the generated files
        /// Need to do it that way because many vcproj may be generated from the same Project.
        /// </summary>
        public static string BuildGuid(string outputProjectFile, string referencePath)
        {
            string relativeToCsProjectFile = PathGetRelative(referencePath, outputProjectFile);
            return BuildGuid(relativeToCsProjectFile.ToLower()).ToString().ToUpper();
        }

        public static string GetDotNetTargetString(DotNetFramework framework)
        {
            string version = framework.ToVersionString();
            if (string.IsNullOrEmpty(version))
                return string.Empty;

            return string.Format("v{0}", version);
        }

        public static string GetToolVersionString(DevEnv env, DotNetFramework desiredFramework)
        {
            switch (env)
            {
                case DevEnv.vs2010:
                    if (desiredFramework > DotNetFramework.v4_5clientprofile)
                        throw new Exception(string.Format("The target framework ({0}) isn't supported in the target environment({1})", desiredFramework, env));
                    return DotNetFramework.v4_0.ToVersionString(); //"Both Visual Studio 2010 and Visual Studio 2012 use a ToolsVersion of 4.0" ref:http://msdn.microsoft.com/en-us/LIbrary/bb383796%28v=vs.110%29.aspx
                case DevEnv.vs2012:
                    return DotNetFramework.v4_0.ToVersionString();
                case DevEnv.vs2013:
                    return DotNetFramework.v4_5.ToVersionString();
                case DevEnv.vs2015:
                case DevEnv.vs2017:
                case DevEnv.vs2019:
                    return env.GetVisualProjectToolsVersionString();
                case DevEnv.xcode4ios:
                    throw new NotSupportedException("XCode does not support Tool Version. ");
                case DevEnv.eclipse:
                    throw new NotSupportedException("Eclipse does not support Tool Version. ");
                default:
                    throw new NotImplementedException(String.Format("ToolVersion not set for Visual Studio {0}", env));
            }
        }

        public enum FileCopyDestReadOnlyPolicy : byte
        {
            Preserve,
            SetReadOnly,
            UnsetReadOnly
        }

        public static void ForceCopy(string source, string destination)
        {
            ForceCopy(source, destination, FileCopyDestReadOnlyPolicy.Preserve);
        }

        public static void ForceCopy(string source, string destination, FileCopyDestReadOnlyPolicy destinationReadOnlyPolicy)
        {
            if (File.Exists(destination))
            {
                FileAttributes attributes = File.GetAttributes(destination);
                attributes &= ~FileAttributes.ReadOnly;
                File.SetAttributes(destination, attributes);
            }

            File.Copy(source, destination, true);

            if (destinationReadOnlyPolicy != FileCopyDestReadOnlyPolicy.Preserve)
            {
                FileAttributes attributes = File.GetAttributes(destination);
                if (destinationReadOnlyPolicy == FileCopyDestReadOnlyPolicy.SetReadOnly)
                {
                    attributes |= FileAttributes.ReadOnly;
                }
                else
                {
                    attributes &= ~FileAttributes.ReadOnly;
                }
                File.SetAttributes(destination, attributes);
            }
        }

        public static bool IsDotNet(Project.Configuration conf)
        {
            Options.Vc.General.CommonLanguageRuntimeSupport clrSupportOption = Options.GetObject<Options.Vc.General.CommonLanguageRuntimeSupport>(conf);
            return clrSupportOption != Options.Vc.General.CommonLanguageRuntimeSupport.NoClrSupport ||
                   conf.Output == Project.Configuration.OutputType.DotNetClassLibrary ||
                   conf.Output == Project.Configuration.OutputType.DotNetConsoleApp ||
                   conf.Output == Project.Configuration.OutputType.DotNetWindowsApp;
        }

        public static bool IsCpp(Project.Configuration conf)
        {
            string extension = Path.GetExtension(conf.ProjectFullFileNameWithExtension);
            return (String.Compare(extension, ".vcxproj", StringComparison.OrdinalIgnoreCase) == 0);
        }

        public static string GetProjectFileExtension(Project.Configuration conf)
        {
            string extension;
            if (conf.Project is CSharpProject)
                extension = ".csproj";
            else if (conf.Project is PythonProject)
                extension = ".pyproj";
            else if (conf.Project is AndroidPackageProject)
                extension = ".androidproj";
            else
            {
                switch (conf.Target.GetFragment<DevEnv>())
                {
                    case DevEnv.vs2010:
                    case DevEnv.vs2012:
                    case DevEnv.vs2013:
                    case DevEnv.vs2015:
                    case DevEnv.vs2017:
                    case DevEnv.vs2019:
                        {
                            extension = ".vcxproj";
                        }
                        break;

                    case DevEnv.xcode4ios:
                        return ".xcodeproj";

                    case DevEnv.eclipse:
                        return ".mk";

                    case DevEnv.make:
                        return ".make";

                    default:
                        throw new NotImplementedException("GetProjectFileExtension called with unknown DevEnv: " + conf.Target.GetFragment<DevEnv>());
                }
            }
            return extension;
        }

        public static string GetAppxManifestFileName(Project.Configuration conf)
        {
            return Path.GetFullPath(PathMakeStandard(conf.AppxManifestFilePath));
        }

        /// <summary>
        /// Extension GetValueOrAdd gets the value at the given key or adds at the given key the value provided
        /// </summary>
        /// <typeparam name="Key">Type of the key</typeparam>
        /// <typeparam name="Value">Type of the value</typeparam>
        /// <param name="dictionary">dictionary in which to search</param>
        /// <param name="key">key of the value</param>
        /// <param name="addValue">value created</param>
        /// <returns>the value at the given key (created or not in this call)</returns>
        public static Value GetValueOrAdd<Key, Value>(this IDictionary<Key, Value> dictionary, Key key, Value addValue)
        {
            Value value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }

            dictionary.Add(key, addValue);
            return addValue;
        }

        public static Value AddOrUpdateValue<Key, Value>(this IDictionary<Key, Value> dictionary, Key key, Value newValue, Func<Key, Value, Value, Value> update)
        {
            Value currentValue;
            if (dictionary.TryGetValue(key, out currentValue))
            {
                newValue = update(key, currentValue, newValue);
            }

            dictionary[key] = newValue;
            return newValue;
        }

        // From Alexandrie
        private static void InternPrintCompleteExceptionTraceToStream(Exception e, int level, bool showStack, TextWriter writer, string indent)
        {
            string levelStr = (level == 0 && e.InnerException == null) ? ":" : " (level " + level + "):";
            if (level == 0)
            {
                writer.WriteLine(indent + "While running " + Process.GetCurrentProcess().MainModule.FileName);
                writer.WriteLine(indent + "@" + DateTime.Now + ": Exception message" + levelStr);
            }
            else
            {
                writer.WriteLine(indent + "Inner exception message" + levelStr);
            }
            writer.WriteLine(indent + e.Message);
            if (e.InnerException == null)
            {
                if (showStack)
                {
                    writer.WriteLine(Environment.NewLine + indent + "Stack trace:");
                }
            }
            else
            {
                InternPrintCompleteExceptionTraceToStream(e.InnerException, level + 1, showStack, writer, indent);
            }
            if (showStack)
            {
                if (level == 0)
                {
                    writer.WriteLine(indent + "Root stack trace" + levelStr);
                }
                else
                {
                    writer.WriteLine(indent + "Inner exception stack trace" + levelStr);
                }
                writer.WriteLine(e.StackTrace);
            }
        }
        public static void PrintCompleteExceptionMessageToConsole(Exception e, TextWriter writer, string indent)
        {
            InternPrintCompleteExceptionTraceToStream(e, 0, true, writer, indent);
        }

        public static string GetCompleteExceptionMessage(Exception e, string indent)
        {
            var writer = new StringWriter();
            PrintCompleteExceptionMessageToConsole(e, writer, indent);
            return writer.ToString();
        }

        public static string GetSimplePlatformString(Platform platform)
        {
            return PlatformRegistry.Query<IPlatformDescriptor>(platform)?.SimplePlatformString ?? platform.ToString();
        }

        public static string GetPlatformString(Platform platform, Project project, ITarget target, bool isForSolution = false)
        {
            if (project is CSharpProject)
            {
                switch (platform)
                {
                    case Platform.win32: return "x86";
                    case Platform.win64: return "x64";
                    case Platform.anycpu: return isForSolution ? "Any CPU" : "AnyCPU";
                    default:
                        throw new Exception(String.Format("This platform: {0} is not supported", platform));
                }
            }
            else if (project is PythonProject)
            {
                return isForSolution ? "Any CPU" : "AnyCPU";
            }

            return PlatformRegistry.Query<IPlatformDescriptor>(platform)?.GetPlatformString(target) ?? platform.ToString();
        }

        [Obsolete("GetPlatformString() now requires a `target` parameter.")]
        public static string GetPlatformString(Platform platform, Project project, bool isForSolution = false)
        {
            return GetPlatformString(platform, project, null, isForSolution);
        }

        public static string CallerInfoTag = "CALLER_INFO: ";
        public static string FormatCallerInfo(string sourceFilePath, int sourceLineNumber)
        {
            return string.Format("{0}{1}({2},0): ", CallerInfoTag, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Look up 2 callerInfo string.
        /// </summary>
        /// <param name="callerInfo1"></param>
        /// <param name="callerInfo2"></param>
        /// <returns>
        /// 1.if they are both referring to file edited by sharpmake user (.sharpmake): concatenation of both separated by a line return
        /// 2.if only callerInfo2 refer to file edited by sharpmake user (.sharpmake): callerInfo2
        /// 3.otherwise: callerInfo1
        /// </returns>
        public static string PickOrConcatCallerInfo(string callerInfo1, string callerInfo2)
        {
            if ((callerInfo1.IndexOf(".sharpmake", StringComparison.OrdinalIgnoreCase) >= 0) &&
               (callerInfo2.IndexOf(".sharpmake", StringComparison.OrdinalIgnoreCase) >= 0))
                return callerInfo1 + Environment.NewLine + callerInfo2;

            if (callerInfo2.IndexOf(".sharpmake", StringComparison.OrdinalIgnoreCase) >= 0)
                return callerInfo2;

            return callerInfo1;
        }

        public static MemoryStream RemoveLineTags(MemoryStream inputMemoryStream, string removeLineTag)
        {
            //
            // TODO: This method should be deprecated and/or removed.
            //       Use FileGenerator or a derived class to build your output files, and call its
            //       RemoveTaggedLines method.
            //


            // remove all line that contain RemoveLineTag
            inputMemoryStream.Seek(0, SeekOrigin.Begin);

            StreamReader streamReader = new StreamReader(inputMemoryStream);

            MemoryStream cleanMemoryStream = new MemoryStream((int)inputMemoryStream.Length);
            StreamWriter cleanWriter = new StreamWriter(cleanMemoryStream, new UTF8Encoding(true));

            string readline = streamReader.ReadLine();
            while (readline != null)
            {
                if (!readline.Contains(removeLineTag))
                    cleanWriter.WriteLine(readline);
                readline = streamReader.ReadLine();
            }

            cleanWriter.Flush();

            // removes the end of line from the last WriteLine to be consistent with VS project
            // output; this will stop the pointless "Do you want to refresh?" prompts because of a
            // new line.
            if (cleanMemoryStream.Length != 0)
                cleanMemoryStream.SetLength(cleanMemoryStream.Length - Environment.NewLine.Length);

            return cleanMemoryStream;
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

        public static uint Rotl32(uint x, int r)
        {
            return (x << r) | (x >> (32 - r));
        }

        public static Object ReadRegistryValue(string key, string value, Object defaultValue = null)
        {
            return Registry.GetValue(key, value, defaultValue);
        }

        public static string[] GetRegistryLocalMachineSubKeyNames(string path)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(path);
            if (key != null)
                return key.GetSubKeyNames();
            return new string[] { };
        }

        private static ConcurrentDictionary<Tuple<string, string>, string> s_registryCache = new ConcurrentDictionary<Tuple<string, string>, string>();

        public static string GetRegistryLocalMachineSubKeyValue(string registrySubKey, string value, string fallbackValue)
        {
            return GetRegistryLocalMachineSubKeyValue(registrySubKey, value, fallbackValue, enableLog: true);
        }

        public static string GetRegistryLocalMachineSubKeyValue(string registrySubKey, string value, string fallbackValue, bool enableLog)
        {
            var subKeyValueTuple = new Tuple<string, string>(registrySubKey, value);
            string registryValue;
            if (s_registryCache.TryGetValue(subKeyValueTuple, out registryValue))
                return registryValue;

            string key = string.Empty;
            try
            {
                using (RegistryKey localMachineKey = Registry.LocalMachine.OpenSubKey(registrySubKey))
                {
                    if (localMachineKey != null)
                    {
                        key = (string)localMachineKey.GetValue(value);
                        if (enableLog && string.IsNullOrEmpty(key))
                            LogWrite("Value '{0}' under registry subKey '{1}' is not set, fallback to default: '{2}'", value ?? "(Default)", registrySubKey, fallbackValue);
                    }
                    else if (enableLog)
                        LogWrite("Registry subKey '{0}' is not found, fallback to default for value '{1}': '{2}'", registrySubKey, value ?? "(Default)", fallbackValue);
                }
            }
            catch { }

            if (string.IsNullOrEmpty(key))
                key = fallbackValue;

            s_registryCache.TryAdd(subKeyValueTuple, key);

            return key;
        }

        public class StopwatchProfiler : IDisposable
        {
            private readonly Stopwatch _stopWatch;
            private readonly Action<long> _disposeAction;
            private readonly long _minThresholdMs;

            public StopwatchProfiler(Action<long> disposeAction)
                : this(disposeAction, 0)
            {
            }

            public StopwatchProfiler(Action<long> disposeAction, long minThresholdMs)
            {
                _disposeAction = disposeAction;
                _stopWatch = Stopwatch.StartNew();
                _minThresholdMs = minThresholdMs;
            }

            public void Dispose()
            {
                long elapsed = _stopWatch.ElapsedMilliseconds;
                if (elapsed > _minThresholdMs)
                    _disposeAction(elapsed);
            }
        }

        public class VersionStringComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                if (x == y) return 0;
                var version = new { First = GetVersion(x), Second = GetVersion(y) };
                int limit = Math.Max(version.First.Length, version.Second.Length);
                for (int i = 0; i < limit; i++)
                {
                    int first = version.First.ElementAtOrDefault(i);
                    int second = version.Second.ElementAtOrDefault(i);
                    if (first > second) return 1;
                    if (second > first) return -1;
                }
                return 0;
            }

            private int[] GetVersion(string version)
            {
                return (from part in version.Split('.')
                        select Parse(part)).ToArray();
            }

            private int Parse(string version)
            {
                int result;
                int.TryParse(version, out result);
                return result;
            }
        }

        // http://www.mono-project.com/docs/faq/technical/#how-can-i-detect-if-am-running-in-mono
        private static readonly bool s_monoRuntimeExists = (Type.GetType("Mono.Runtime") != null);
        public static bool IsRunningInMono() => s_monoRuntimeExists;

        public static Platform GetExecutingPlatform() => s_executingPlatform;

        private static readonly Platform s_executingPlatform = DetectExecutingPlatform();
        private static Platform DetectExecutingPlatform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                    return (Environment.Is64BitOperatingSystem) ? Platform.win64 : Platform.win32;
                case PlatformID.MacOSX:
                    return Platform.mac;
                case PlatformID.Unix: // could be mac or linux
                    {
                        bool isMacOs = false;
                        try
                        {
                            var p = new System.Diagnostics.Process();
                            p.StartInfo.UseShellExecute = false;
                            p.StartInfo.RedirectStandardOutput = true;
                            p.StartInfo.FileName = "uname";
                            p.Start();
                            string output = p.StandardOutput.ReadToEnd().Trim();
                            p.WaitForExit();

                            isMacOs = string.CompareOrdinal(output, "Darwin") == 0;
                        }
                        catch { }

                        return isMacOs ? Platform.mac : Platform.linux;
                    }
            }
            LogWrite("Warning: Couldn't determine running platform");
            return Platform.win64; // arbitrary
        }
    }
}
