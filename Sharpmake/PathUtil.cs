// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Sharpmake
{
    public static partial class Util
    {
        public const char UnixSeparator = '/';
        public const char WindowsSeparator = '\\';
        private const string s_unixMountPointForWindowsDrives = "/mnt/";

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

        /// <summary>
        /// Cleanup the path by replacing the other separator by the correct one for the current OS
        /// then trim every trailing separators, except if <paramref name="path"/> is a root (i.e. 'C:\' or '/')
        /// </summary>
        /// <remarks>Note that if the given <paramref name="path"/> is a drive letter with volume separator,
        /// without slash/backslash, a directory separator will be added to make the path fully qualified.
        /// <br>But if the given <paramref name="path"/> is not just a drive letter and also has missing slash/backslah
        /// after volume separator (for example "C:toto/tata/"), then the return path won't be fully qualified
        /// (see here for more information on drive relative paths <see href="https://learn.microsoft.com/en-us/dotnet/standard/io/file-path-formats"/>)</br>
        /// <para>Note that Windows paths on Unix will have slashes (and vice versa)</para>
        /// <para>Note that network paths (like NAS) starting with "\\" are not supported</para>
        /// </remarks>
        public static string PathMakeStandard(string path, bool forceToLower)
        {
            ArgumentNullException.ThrowIfNull(path, nameof(path));

            var standardPath = path.Replace(OtherSeparator, Path.DirectorySeparatorChar);

            // C#11 is currently disable until Sharpmake is fully ported to .net8
            //standardPath = standardPath switch
            //{
            //    [WindowsSeparator or UnixSeparator] => standardPath,
            //    [_, ':'] => IsRunningOnUnix() ? standardPath : standardPath + Path.DirectorySeparatorChar,
            //    [_, ':', WindowsSeparator or UnixSeparator] => standardPath,
            //    _ => standardPath.TrimEnd(Path.DirectorySeparatorChar),
            //};

            if (standardPath.Length == 1 && (standardPath[0] == WindowsSeparator || standardPath[0] == UnixSeparator))
            {
                // Nothing to do to make the path standard
            }
            else if (standardPath.Length == 2 && standardPath[1] == ':')
            {
                standardPath = IsRunningOnUnix() ? standardPath : standardPath + Path.DirectorySeparatorChar;
            }
            else if (standardPath.EndsWith($":{WindowsSeparator}", StringComparison.Ordinal) || standardPath.EndsWith($":{UnixSeparator}", StringComparison.Ordinal))
            {
                // Nothing to do to make the path standard
            }
            else
            {
                standardPath = standardPath.TrimEnd(Path.DirectorySeparatorChar);
            }

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
            List<string> result = new List<string>(destFullPaths.Count);

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
                string cleanPath = SimplifyPath(rootPath);
                if (!tmpAbsolute.StartsWith(cleanPath, StringComparison.OrdinalIgnoreCase))
                    return tmpAbsolute;
            }

            return newRelativePath;
        }

        private static readonly ConcurrentDictionary<string, string> s_cachedSimplifiedPaths = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Take a path and compute a canonical version of it. It removes any extra: "..", ".", directory separators...
        /// Note that symbolic links are not expanded, path does not need to exist on the file system.
        /// </summary>
        /// Basic implementation details:
        /// - We take for granted that the simplified path will always be smaller that the input path.
        ///   - This allow to allocate a working buffer only once, and work inside it.
        /// - The logic starts from the end of the input path and walks it backward.
        ///   - This allow to simply count ".." occurrences and skip folder accordingly as we move toward the root of the path.
        ///   - Characters are written in the working buffer starting by its end, and an index is always written once (no back tracking).
        /// - At the end, if there is remaining "..", they are added back to the path.
        /// - At the end, the working buffer may still have some room at its beginning (when the simplified path is smaller than the input path).
        ///   - A string is created occordingly to skip this unused/uninitialized space
        public static unsafe string SimplifyPathImpl(string path)
        {
            // First construct a path helper to help with the conversion
            char* arrayPtr = stackalloc char[path.Length + 1];

            int consecutiveDotsCounter = 0;
            bool pathSeparatorWritePending = IsPathSeparator(path[^1]); // Explicitly handle path with a trailing path separator (we want to keep it)
            int writePosition = path.Length;
            int dotDotCounter = 0;

            // Start by the end of the path to easily handle '..' case
            for (int index = path.Length - 1; index >= 0; --index)
            {
                char currentChar = path[index];
                if (IsPathSeparator(currentChar))
                {
                    pathSeparatorWritePending = pathSeparatorWritePending
                        || (consecutiveDotsCounter == -1 && dotDotCounter == 0); // We want to consider this path separator only if we are not on a '.' or '..'

                    HandleDotDotCounter(ref consecutiveDotsCounter, ref dotDotCounter, path);
                }
                else
                {
                    // Count consecutive dots (if there is only dots in the name)
                    if (currentChar == '.' && consecutiveDotsCounter != -1)
                    {
                        ++consecutiveDotsCounter;
                        continue;
                    }

                    if (dotDotCounter == 0)
                    {
                        // Write pending path separator
                        if (pathSeparatorWritePending)
                        {
                            arrayPtr[writePosition--] = Path.DirectorySeparatorChar;
                            pathSeparatorWritePending = false;
                        }

                        // Write held back '.'
                        for (int i = 0; i < consecutiveDotsCounter; ++i)
                        {
                            arrayPtr[writePosition--] = '.';
                        }

                        arrayPtr[writePosition--] = currentChar;
                    }

                    // We encountered something else than '.', now we don't care about them until next path separator
                    consecutiveDotsCounter = -1;
                }
            }

            // Handle additional '..' that was not taken into account nor consummed
            HandleDotDotCounter(ref consecutiveDotsCounter, ref dotDotCounter, path);
            for (int i = 0; i < dotDotCounter; ++i)
            {
                if (pathSeparatorWritePending)
                    arrayPtr[writePosition--] = Path.DirectorySeparatorChar;
                arrayPtr[writePosition--] = '.';
                arrayPtr[writePosition--] = '.';
                pathSeparatorWritePending = true;
            }

            // Handle rooted path on Unix platforms
            if (path[0] == '/')
                arrayPtr[writePosition--] = '/';

            return new string(arrayPtr, writePosition + 1, path.Length - writePosition);

            static bool IsPathSeparator(char c) => c == Path.DirectorySeparatorChar || c == OtherSeparator;

            static void HandleDotDotCounter(ref int consecutiveDotsCounter, ref int dotDotCounter, string path)
            {
                switch (consecutiveDotsCounter)
                {
                    case -1:
                        // We encountered a real folder name (not made exclusively of dots), and it have been skipped if dotDotCounter was not 0, so we decrement it
                        if (dotDotCounter > 0)
                            --dotDotCounter;
                        break;
                    case 0:
                        break;
                    case 1:
                        // skip: "./"
                        break;
                    case 2:
                        ++dotDotCounter;
                        break;
                    default:
                        throw new ArgumentException($"Invalid path format: '{path}' (folder made of three or more consecutive dots detected)");
                }

                // We are on a path separator, consecutive dots counter must be reset
                consecutiveDotsCounter = 0;
            }
        }

        public static unsafe string SimplifyPath(string path)
        {
            if (path.Length == 0)
                return string.Empty;

            if (path == ".")
                return path;

            string simplifiedPath = s_cachedSimplifiedPaths.GetOrAdd(path, s => SimplifyPathImpl(s));

            return simplifiedPath;
        }

        // Note: This method assumes that SimplifyPath has been called for the argument.
        internal static unsafe void SplitStringUsingStack(string path, int* splitIndexes, int* splitLengths, ref int splitElementsUsedCount, int splitArraySize)
        {
            int lastSeparatorIndex = -1;
            int pathLength = path.Length;
            for (int index = 0; index < pathLength; ++index)
            {
                char currentChar = path[index];

                if (currentChar == Path.DirectorySeparatorChar)
                {
                    if (splitElementsUsedCount == splitArraySize)
                        throw new Exception($"Too much path separators in path '{path}'");

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

        /// <summary>
        /// Return a relative version of a path from another.
        /// </summary>
        /// <param name="relativeTo">The path from which to compute the relative path.</param>
        /// <param name="path">The path to make relative.</param>
        /// <param name="ignoreCase">WARNING: this argument is never used. Whatever the value provided, case will always be ignored.</param>
        /// <returns></returns>
        public static unsafe string PathGetRelative(string relativeTo, string path, bool ignoreCase = false)
        {
            // ------------------ THIS IS NOT CORRECT ----------------
            // Force to always ignore case, whatever the user ask
            // This keep the legacy Sharpmake behavior. It may be fixed at a later date to reduce modification scope.
            ignoreCase = true;
            // ------------------ THIS IS NOT CORRECT ----------------

            relativeTo = SimplifyPath(relativeTo);
            path = SimplifyPath(path);

            // Check different root
            var relativeToLength = relativeTo.Length;
            var pathLength = path.Length;
            if (relativeToLength == 0 || pathLength == 0 || !IsCharEqual(relativeTo[0], path[0], ignoreCase))
                return path;

            // Compute common part length
            var lastCommonDirSepPosition = 0;
            var commonPartLength = 0;
            while (commonPartLength < relativeToLength && commonPartLength < pathLength && IsCharEqual(relativeTo[commonPartLength], path[commonPartLength], ignoreCase))
            {
                if (relativeTo[commonPartLength] == Path.DirectorySeparatorChar)
                    lastCommonDirSepPosition = commonPartLength;
                ++commonPartLength;
            }

#if NET7_0_OR_GREATER
            [Obsolete("Directly use 'char.IsAsciiLetter()' in 'IsCharEqual()' bellow (char.IsAsciiLetter() is available starting net7)")]
#endif
            static bool IsAsciiLetter(char c) => (uint)((c | 0x20) - 'a') <= 'z' - 'a';
            static bool IsCharEqual(char a, char b, bool ignoreCase) => a == b || (ignoreCase && (a | 0x20) == (b | 0x20) && IsAsciiLetter(a));

            // Check if both paths are the same (ignoring the last directory separator if any)
            if ((relativeToLength == commonPartLength && pathLength == commonPartLength)
                || (relativeToLength == commonPartLength && pathLength == commonPartLength + 1 && path[commonPartLength] == Path.DirectorySeparatorChar)
                || (pathLength == commonPartLength && relativeToLength == commonPartLength + 1 && relativeTo[commonPartLength] == Path.DirectorySeparatorChar))
            {
                return ".";
            }

            // Adjust 'commonPartLength' in case we stopped the comparison in the middle of an entry name:
            //   -> we went too far, we must move back 'commonPartLength' to the 'lastCommonDirSepPosition' position '+ 1'
            //      - /abc_def and /abc_xyz
            //      - /abc_    and /abc
            if (commonPartLength < relativeToLength && commonPartLength < pathLength
                || (relativeToLength == commonPartLength && path[commonPartLength] != Path.DirectorySeparatorChar)
                || (pathLength == commonPartLength && relativeTo[commonPartLength] != Path.DirectorySeparatorChar))
            {
                commonPartLength = lastCommonDirSepPosition + 1;
            }

            // Compute the number of ".." to add (to get out of the 'relativeTo' path)
            var dotDotCount = 0;
            var relativeToLengthWithoutTrailingDirSep = relativeTo[^1] == Path.DirectorySeparatorChar ? relativeToLength - 1 : relativeToLength;
            if (relativeToLengthWithoutTrailingDirSep > commonPartLength)
            {
                dotDotCount = 1;
                for (int i = commonPartLength + 1; i < relativeToLengthWithoutTrailingDirSep; i++)
                {
                    if (relativeTo[i] == Path.DirectorySeparatorChar)
                        ++dotDotCount;
                }
            }

            // Compute the length of the two parts to write
            // - The sequences that looks like this: ".." or "../.."  or ...
            var dotDotCountSequenceLength = dotDotCount == 0 ? 0 : dotDotCount * 2 + dotDotCount - 1;

            // - The remaining from the 'path' not yet added (skip the starting directory separator if any)
            var remainingStartPosition = commonPartLength < pathLength && path[commonPartLength] == Path.DirectorySeparatorChar ? commonPartLength + 1 : commonPartLength;
            var remainingLength = pathLength - remainingStartPosition;

            // This 'if' deviate from the standard .net behavior and is here to keep legacy Sharpmake behavior
            // Trim last directory separator if any
            if (remainingLength > 0 && path[^1] == Path.DirectorySeparatorChar)
                --remainingLength;

            // - The directory separator between the two parts (in case there is both dotDots and remains)
            var needToInsertDirSepBeforeRemainingPart = dotDotCountSequenceLength > 0 && remainingLength > 0;

            var finalLength =
                  dotDotCountSequenceLength
                + remainingLength
                + (needToInsertDirSepBeforeRemainingPart ? 1 : 0);

            // Allocate and write in the buffer
            Span<char> arrayPtr = stackalloc char[finalLength];
            int writePosition = 0;
            if (dotDotCount > 0)
            {
                arrayPtr[writePosition++] = '.';
                arrayPtr[writePosition++] = '.';
                for (int i = 0; i < dotDotCount - 1; i++)
                {
                    arrayPtr[writePosition++] = Path.DirectorySeparatorChar;
                    arrayPtr[writePosition++] = '.';
                    arrayPtr[writePosition++] = '.';
                }
            }

            if (needToInsertDirSepBeforeRemainingPart)
                arrayPtr[writePosition++] = Path.DirectorySeparatorChar;

            if (remainingLength > 0)
            {
                var remainAsSpan = path.AsSpan(remainingStartPosition, remainingLength);
                remainAsSpan.CopyTo(arrayPtr.Slice(writePosition, remainingLength));
            }

            return new string(arrayPtr);
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

        private static readonly ConcurrentDictionary<string, string> s_cachedCombinedToAbsolute = new ConcurrentDictionary<string, string>();

        public static string PathGetAbsolute(string absolutePath, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return absolutePath;

            // Handle environment variables and string that contains more than 1 path
            if (relativePath.StartsWith("$", StringComparison.Ordinal) || relativePath.Count(x => x == ';') > 1)
                return relativePath;

            string cleanRelative = SimplifyPath(relativePath);
            if (Path.IsPathFullyQualified(cleanRelative))
                return cleanRelative;

            string resultPath = s_cachedCombinedToAbsolute.GetOrAdd(string.Format("{0}|{1}", absolutePath, relativePath), combined =>
            {
                string firstPart = PathMakeStandard(absolutePath);

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
            return GetCapitalizedPath(resolvedPath);
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
                // This search could fail on case sensitive filesystem. We will revert to a slower method if not found
                bool foundFilename = false;
                foreach (var fsInfo in dirInfo.EnumerateFiles(fileInfo.Name))
                {
                    properFileName = fsInfo.Name;
                    foundFilename = true;
                    break;
                }

                if (!foundFilename) 
                {
                    // Slow search - Normally shouldn't happen
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
            }
            return Path.Combine(builder.ToString(), properFileName);
        }

        private static readonly ConcurrentDictionary<string, string> s_capitalizedPaths = new ConcurrentDictionary<string, string>();

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

        internal static void RegisterCapitalizedPath(string physicalPath)
        {
            string pathLC = physicalPath.ToLower();
            s_capitalizedPaths.TryAdd(pathLC, physicalPath);
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

        public static string ConvertToUnixSeparators(string path)
        {
            return path.Replace(WindowsSeparator, UnixSeparator);
        }

        public static string ConvertToMountedUnixPath(string path)
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

        public static string FindCommonRootPath(IEnumerable<string> paths)
        {
            paths = paths.Select(PathMakeStandard);
            var pathsChunks = paths.Select(p => p.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)).Where(p => p.Any());

            if (pathsChunks.Any())
            {
                var sb = new StringBuilder();
                var isFirst = true;
                var chunkStartIndex = 0;

                // Handle fully qualified paths
                // C#11 is currently disable until Sharpmake is fully ported to .net8
                //var fullyQualifiedPath = paths.FirstOrDefault(p => p is ([UnixSeparator or WindowsSeparator, ..]) or ([_, ':', UnixSeparator or WindowsSeparator, ..]));
                string fullyQualifiedPath = null;
                foreach (var path in paths)
                {
                    if (path[0] == UnixSeparator || path[0] == WindowsSeparator
                        || (path.Length >= 3 && path[1] == ':' && (path[2] == UnixSeparator || path[2] == WindowsSeparator)))
                    {
                        fullyQualifiedPath = path;
                        break;
                    }
                }

                // If no fully qualified path is found, it remains null

                if (fullyQualifiedPath is not null)
                {
                    if (fullyQualifiedPath[0] == Path.DirectorySeparatorChar)
                    {
                        sb.Append(Path.DirectorySeparatorChar);
                    }
                    else
                    {
                        sb.Append(fullyQualifiedPath[0]);
                        sb.Append(':');
                        sb.Append(Path.DirectorySeparatorChar);
                        chunkStartIndex++;
                    }

                    // All path should start with the same root path, else there is nothing in common
                    var rootPath = sb.ToString();
                    if (paths.Any(p => !p.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        return null;
                    }
                }

                var referenceChunks = pathsChunks.First();
                int smallestChunksCount = pathsChunks.Min(p => p.Length);
                for (var i = chunkStartIndex; i < smallestChunksCount; ++i)
                {
                    var reference = referenceChunks[i];
                    if (pathsChunks.All(p => string.Equals(p[i], reference, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!isFirst)
                            sb.Append(Path.DirectorySeparatorChar);
                        isFirst = false;

                        sb.Append(reference);
                    }
                    else
                    {
                        break;
                    }
                }

                var foundSomeCommonChunks = sb.Length != 0;
                if (foundSomeCommonChunks)
                {
                    return sb.ToString();
                }
            }

            return null;
        }

        /// <summary>
        /// Checks is pathToTest is a subfolder or file under the rootPath directory. 
        /// </summary>
        /// <param name="rootPath">An absolute path to a directory or a file considered as the root for the test and resolivng relative paths.
        /// If a file path is used, the file's direct parent directory will be used.</param>
        /// <param name="pathToTest">An absolute or relative path to a file or directory to be tested.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static bool PathIsUnderRoot(string rootPath, string pathToTest)
        {
            if (!Path.IsPathFullyQualified(rootPath))
                throw new ArgumentException("rootPath needs to be absolute.", nameof(rootPath));

            if (!Path.IsPathFullyQualified(pathToTest))
                pathToTest = Path.GetFullPath(pathToTest, rootPath);

            var intersection = GetPathIntersection(rootPath, pathToTest);

            if (string.IsNullOrEmpty(intersection))
                return false;

            if (!Util.PathIsSame(intersection, rootPath))
            {
                if (rootPath.EndsWith(Path.DirectorySeparatorChar))
                    return false;

                // only way to make sure path point to file is to check on disk
                // if file doesn't exist, treats this edge case as if path wasn't a file path
                var fileInfo = new FileInfo(rootPath);
                if (fileInfo.Exists && Util.PathIsSame(intersection, fileInfo.DirectoryName))
                    return true;

                return false;
            }

            return true;
        }

        /// <summary>
        ///     Removes every occurance of dotdot ("../") from the beginning of a relative path and returns it. 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string TrimAllLeadingDotDot(string path)
        {
            var spanStartIndex = 0;
            ReadOnlySpan<char> trimmedSpan = path.AsSpan().Slice(start: spanStartIndex);
            var platformSpecificDotDot = new List<string>();

            foreach (var platformSeparator in _pathSeparators)
            {
                platformSpecificDotDot.Add(".." + platformSeparator);
            }

            var keepTrimming = true;
            while (keepTrimming)
            {
                keepTrimming = false;

                foreach (var dotdot in platformSpecificDotDot)
                {
                    if (trimmedSpan.StartsWith(dotdot))
                    {
                        spanStartIndex += dotdot.Length;
                        trimmedSpan = path.AsSpan().Slice(start: spanStartIndex);

                        keepTrimming = true;
                        break;
                    }
                }
            }

            return trimmedSpan.ToString();
        }
    }
}
