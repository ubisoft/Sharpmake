// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Buffers;
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
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Setup.Configuration;
using Microsoft.Win32;

namespace Sharpmake
{
    public static partial class Util
    {
        public static DateTime ProgramStartTime { get; } = DateTime.Now;

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

        /// <summary>
        /// Returns a OR regular expression for all the entries in the IEnumerable
        /// e.g. {"foo", "bar"}.ToOrRegex() yields "(foo|bar)"
        /// </summary>
        public static string ToOrRegex(this IEnumerable<string> array)
        {
            return $"({string.Join("|", array)})";
        }

        /// <summary>
        /// Returns a OR regular expression for all the entries in the IEnumerable, except those passed as `except`
        /// e.g. {"foo", "bar", "hoge"}.ToOrRegexExcept("bar") yields "(foo|hoge)"
        /// </summary>
        public static string ToOrRegexExcept(this IEnumerable<string> array, params string[] except)
        {
            if (except.Length == 0)
                return $"({string.Join("|", array)})";

            return $"({string.Join("|", array.Except(except))})";
        }


        public static bool FlagsTest<T>(T value, T flags)
        {
            int intValue = (int)(object)value;
            int intflag = (int)(object)flags;
            return ((intValue & intflag) == intflag);
        }

        /// <summary>
        /// This method will return a deterministic hash for a string.
        /// </summary>
        /// <remarks>
        /// With net core the regular GetHashCode() is now
        /// seeded for security reasons.
        /// </remarks>
        /// <see href="https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/"/>
        /// <param name="str">The input string</param>
        /// <returns>A deterministic hash</returns>
        public static int GetDeterministicHashCode(this string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
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

        /// <summary>
        /// Size of buffer used for stream comparisons.
        /// </summary>
        private const int _FileStreamBufferSize = 4096;

        /// <summary>
        /// Efficiently compare is two streams have the same content
        /// </summary>
        /// <param name="stream1">1st stream</param>
        /// <param name="stream2">2nd stream</param>
        /// <returns>true=equal, false=not equal</returns>
        private static bool AreStreamsEqual(Stream stream1, Stream stream2)
        {
            byte[] buffer1=null;
            byte[] buffer2=null;
            try
            {
                // Request buffers from shared pool to reduce pressure on GC.
                buffer1 = ArrayPool<byte>.Shared.Rent(_FileStreamBufferSize);
                buffer2 = ArrayPool<byte>.Shared.Rent(_FileStreamBufferSize);

                stream1.Position = 0;
                stream2.Position = 0;

                while (true)
                {
                    // Read from both streams
                    int count1 = stream1.Read(buffer1, 0, buffer1.Length);
                    int count2 = stream2.Read(buffer2, 0, buffer2.Length);

                    if (count1 != count2)
                        return false;

                    if (count1 == 0)
                        return true;

                    Span<byte> span1 = new Span<byte>(buffer1, 0, count1);
                    Span<byte> span2 = new Span<byte>(buffer2, 0, count2);

                    // Compare the streams efficiently without any copy.
                    if (!span1.SequenceEqual(span2))
                        return false;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer1);
                ArrayPool<byte>.Shared.Return(buffer2);
            }
        }

        [Obsolete("Call IsFileDifferent() with two parameters. Last parameter has been removed.")]
        public static bool IsFileDifferent(FileInfo file, Stream stream, bool compareEndLineCharacters = false)
        {
            return IsFileDifferent(file, stream);
        }

        /// <summary>
        /// Check if a file is different than the stream content. Content is compared bit per bit.
        /// </summary>
        /// <param name="file">file to compare</param>
        /// <param name="stream">stream to compare</param>
        /// <returns>true=different, false=same</returns>
        public static bool IsFileDifferent(FileInfo file, Stream stream)
        {
            if (!file.Exists)
                return true;

            // Note: Using same buffer size than AreStreamsEqual for better efficiency
            using (var fstream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, _FileStreamBufferSize))
            {
                return !AreStreamsEqual(stream, fstream);
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

        public static GenerationOutput FileWriteIfDifferent(string outputFilePath, IFileGenerator generator, GenerationOutput generationOutput = null)
        {
            if (generationOutput == null)
                generationOutput = new GenerationOutput();

            if (FileWriteIfDifferent(new FileInfo(outputFilePath), generator))
                generationOutput.Generated.Add(outputFilePath);
            else
                generationOutput.Skipped.Add(outputFilePath);

            return generationOutput;
        }

        public static bool FileWriteIfDifferent(FileInfo file, MemoryStream stream)
        {
            return Builder.Instance.Context.WriteGeneratedFile(null, file, stream);
        }

        public static bool FileWriteIfDifferent(FileInfo file, IFileGenerator generator)
        {
            return Builder.Instance.Context.WriteGeneratedFile(null, file, generator);
        }

        internal static bool RecordInAutoCleanupDatabase(string fullPath)
        {
            return s_writtenFiles.TryAdd(fullPath, DateTime.Now);
        }

        internal static bool FileWriteIfDifferentInternal(FileInfo file, MemoryStream stream, bool bypassAutoCleanupDatabase = false)
        {
            if (!bypassAutoCleanupDatabase)
                RecordInAutoCleanupDatabase(file.FullName);

            if (file.Exists)
            {
                if (!IsFileDifferent(file, stream))
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
                stream.WriteTo(outStream);
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
        private static List<string> _FilesAlternatesAutoCleanupDBFullPaths = new List<string>();
        public static string FilesAutoCleanupDBPath { get; set; } = string.Empty;
        public static string FilesAutoCleanupDBSuffix { get; set; } = string.Empty;   // Current auto-cleanup suffix for the database.
        internal static bool s_forceFilesCleanup = false;
        internal static string s_overrideFilesAutoCleanupDBPath;

        public static bool FilesAutoCleanupActive { get; set; }
        public static TimeSpan FilesAutoCleanupDelay { get; set; } = TimeSpan.Zero;
        public static HashSet<string> FilesToBeExplicitlyRemovedFromDB = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        public static HashSet<string> FilesAutoCleanupIgnoredEndings = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        private const string s_filesAutoCleanupDBPrefix = "sharpmakeautocleanupdb";

        private static JsonSerializerOptions GetCleanupDatabaseJsonSerializerOptions()
        {
            return new JsonSerializerOptions()
            {
                AllowTrailingCommas = true,
                PropertyNamingPolicy = null,
                WriteIndented = true,
            };
        }

        private class CleanupDatabaseContent
        {
            public enum DBVersions 
            {
                BinaryFormatterVersion = 3, // Json in a binary formatter - Deprecated - Support will be removed in the first version released after dec 31th 2024
                JsonVersion = 4, // New format - simple json
                CurrentVersion = JsonVersion
            };

            public DBVersions DBVersion { get; set; }
            public object Data { get; set; }
        }

        private static Dictionary<string, DateTime> ReadCleanupDatabase(string databaseFilename)
        {
            // DEPRECATED CODE - TO BE REMOVED AFTER DEC 31TH 2024
            string oldDatabaseFormatFilename = Path.ChangeExtension(databaseFilename, ".bin");
            if (File.Exists(oldDatabaseFormatFilename))
            {
                try
                {
                    // Read database - This is simply a simple binary file containing the list of file and a version number.
                    using (Stream readStream = new FileStream(oldDatabaseFormatFilename, FileMode.Open, FileAccess.Read, FileShare.None))
                    using (BinaryReader binReader = new BinaryReader(readStream))
                    {
                        // Validate version number
                        int version = binReader.ReadInt32();
                        if (version == (int)CleanupDatabaseContent.DBVersions.BinaryFormatterVersion)
                        {
                            // Read the list of files.
                            IFormatter formatter = new BinaryFormatter();
                            string dbAsJson = binReader.ReadString();

                            var tmpDbFiles = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, DateTime>>(dbAsJson, GetCleanupDatabaseJsonSerializerOptions());
                            var dbFiles = tmpDbFiles.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.InvariantCultureIgnoreCase);

                            // Converting to new format.
                            WriteCleanupDatabase(databaseFilename, dbFiles);
                            return dbFiles;
                        }
                        else
                        {
                            LogWrite("Warning: found cleanup database in incompatible format v{0}, skipped.", version);
                        }
                    }
                }
                catch
                {
                    // nothing to do.
                }
                finally
                {
                    TryDeleteFile(oldDatabaseFormatFilename);
                }
            }
            // END DEPRECATED CODE

            if (File.Exists(databaseFilename))
            {
                try
                {
                    string jsonString = File.ReadAllText(databaseFilename);
                    var versionedDB = System.Text.Json.JsonSerializer.Deserialize<CleanupDatabaseContent>(jsonString);
                    if (versionedDB.DBVersion == CleanupDatabaseContent.DBVersions.JsonVersion)
                    {
                        // Deserialize the Database to a dictionary
                        var tmpDbFiles = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, DateTime>>(versionedDB.Data.ToString(), GetCleanupDatabaseJsonSerializerOptions());

                        // Convert the dictionary to a case insensitive dictionary
                        var dbFiles = tmpDbFiles.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.InvariantCultureIgnoreCase);

                        return dbFiles;
                    }
                    else
                    {
                        LogWrite($"Cleanup database version {versionedDB.DBVersion} is not supported. Ignoring database"); 
                    }
                }
                catch
                {
                    // DB File is likely corrupted.
                    // This is no big deal except that cleanup won't occur and this could result in files not written by the current Sharpmake run to not being deleted.
                }
            }
            return null;
        }

        private static string GetDatabaseFilename(string dbSuffix)
        {
            if (!string.IsNullOrWhiteSpace(s_overrideFilesAutoCleanupDBPath))
                return s_overrideFilesAutoCleanupDBPath;

            string databaseFilename = Path.Combine(FilesAutoCleanupDBPath, $"{s_filesAutoCleanupDBPrefix}{dbSuffix}.json");
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
        /// This is useful when you execute sharpmake with more than one setup configuration. For example on one project, we have two setups:
        /// - Engine and Tools and both are running different scripts but have the same .sharpmake file entry point. In that case we would
        /// set the suffix with different value depending on the context we are running sharpmake with.
        /// - Generally you should also disable the cleanup when running with changelist filters(used typically by Submit Assistant).
        /// </remarks>
        ///
        /// <example>
        /// This is the way the auto-cleanup is configured on one of our projects, this code is in the main.
        /// Util.AutoCleanupDBPath = sharpmakeFileDirectory;
        /// Util.FilesAutoCleanupActive = Arguments.Filter != Filter.Changelist && arguments.Builder.BlobOnly == false;
        /// if (Arguments.GenerateTools)
        ///    Util.AutoCleanupDBSuffix = "_tools";
        /// </example>
        public static void ExecuteFilesAutoCleanup()
        {
            ExecuteFilesAutoCleanup(false);
        }

        /// <summary>
        /// This method is the same as the other ExecuteFilesAutoCleanup but this one gives control if we need to add the current context db
        /// to alternateDB files list for proper cleanup execution if execution context changes in a subsequent execution
        /// For example, _debugsolution context when generating debug solution followed
        /// by a default execution context when executing normal generation.
        /// </summary>
        /// <param name="addDBToAlternateDB"></param>
        /// <exception cref="Exception"></exception>
        internal static void ExecuteFilesAutoCleanup(bool addDBToAlternateDB)
        {
            if (!FilesAutoCleanupActive && !s_forceFilesCleanup)
                return; // Auto cleanup not active. Nothing to do.

            if (string.IsNullOrWhiteSpace(s_overrideFilesAutoCleanupDBPath) && !Directory.Exists(FilesAutoCleanupDBPath))
                throw new Exception($"Unable to find directory {FilesAutoCleanupDBPath} used to store auto-cleanup database. Is proper path set?");

            string databaseFilename = GetDatabaseFilename(FilesAutoCleanupDBSuffix);
            Dictionary<string, DateTime> dbFiles = ReadCleanupDatabase(databaseFilename);

            // Note: We must take into account all databases when doing the cleanup otherwise we might end up deleting files still used in other contexts.
            List<Dictionary<string, DateTime>> alternateDatabases = new List<Dictionary<string, DateTime>>();

            // Try to load all alternate db contexts
            var alternateDBFullPaths = FilesAlternatesAutoCleanupDBSuffixes.Select(alternateDBSuffix => GetDatabaseFilename(alternateDBSuffix))
                .Concat(_FilesAlternatesAutoCleanupDBFullPaths.AsEnumerable());

            foreach (string alternateDatabaseFilename in alternateDBFullPaths)
            {
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
                                // Exclude files that were modified since the beginning of the current Sharpmake run.
                                // This should avoid regressions when a generated file is not added to cleanup database anymore.
                                // Example: replacing a call to Util.FileWriteIfDifferent() with File.WriteAll()
                                // From the previous run, Util.FileWriteIfDifferent() added the file in the cleanup database.
                                // In the new run, File.WriteAll() wrote the file, but the cleanup system would want to delete it.
                                if (File.GetLastWriteTime(filenameDate.Key) >= ProgramStartTime)
                                {
                                    LogWrite(@"Skip deleting old file (updated during this run): {0}", filenameDate.Key);
                                    newDbFiles.Add(filenameDate.Key, filenameDate.Value);
                                    continue;
                                }

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

            WriteCleanupDatabase(databaseFilename, newDbFiles);
            if (addDBToAlternateDB)
                _FilesAlternatesAutoCleanupDBFullPaths.Add(databaseFilename);

            // We are done! Clear the list of files to avoid problems as this context is now considered as complete.
            // For example if generating debug solution and then executing normal generation
            s_writtenFiles.Clear();
        }

        private static void WriteCleanupDatabase(string databaseFilename, Dictionary<string, DateTime> generatedFiles)
        {
            CleanupDatabaseContent dbContent = new CleanupDatabaseContent
            {
                DBVersion = CleanupDatabaseContent.DBVersions.CurrentVersion,
                Data = generatedFiles
            };
            string jsonString = System.Text.Json.JsonSerializer.Serialize(dbContent, GetCleanupDatabaseJsonSerializerOptions());
            File.WriteAllText(databaseFilename, jsonString);
        }

        public static string WinFormSubTypesDbPath = string.Empty;
        private static readonly string s_winFormSubTypesDbPrefix = "winformssubtypesdb";

        public static string GetWinFormSubTypeDbPath()
        {
            return Path.Combine(WinFormSubTypesDbPath, $@"{s_winFormSubTypesDbPrefix}.bin");
        }

        private static JsonSerializerOptions GetCsprojSubTypesJsonSerializerOptions()
        {
            return new JsonSerializerOptions()
            {
                AllowTrailingCommas = true,
                PropertyNamingPolicy = null,
                WriteIndented = false,
            };
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
            using (BinaryWriter binWriter = new BinaryWriter(writeStream))
            {
                string csprojSubTypesAsJson = System.Text.Json.JsonSerializer.Serialize(allCsProjSubTypes, GetCsprojSubTypesJsonSerializerOptions());
                binWriter.Write(csprojSubTypesAsJson);
                binWriter.Flush();
            }
        }

        [Obsolete("Use DeserializeAllCsprojSubTypesJson<T> with the known type: the original C# class that was serialized isn't known in the json serialization.")]
        public static object DeserializeAllCsprojSubTypes()
        {
            return DeserializeAllCsprojSubTypesJson<object>();
        }

        public static T DeserializeAllCsprojSubTypesJson<T>()
        {
            string winFormSubTypesDbFullPath = GetWinFormSubTypeDbPath();

            if (!File.Exists(winFormSubTypesDbFullPath))
                return default(T);

            try
            {
                using (Stream readStream = new FileStream(winFormSubTypesDbFullPath, FileMode.Open, FileAccess.Read, FileShare.None))
                using (BinaryReader binReader = new BinaryReader(readStream))
                {
                    string csprojSubTypesAsJson = binReader.ReadString();
                    return System.Text.Json.JsonSerializer.Deserialize<T>(csprojSubTypesAsJson, GetCsprojSubTypesJsonSerializerOptions());
                }
            }
            catch
            {
                TryDeleteFile(winFormSubTypesDbFullPath);
            }
            return default(T);
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
            var provider = System.Security.Cryptography.MD5.Create();
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
                int comparison = string.CompareOrdinal(iterator1.Current.ToString(), iterator2.Current.ToString());
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

        [Obsolete("Use CreateOrUpdateSymbolicLink instead", error: true)]
        public static bool CreateSymbolicLink(string source, string target, bool isDirectory)
        {
            return false;
        }

        private static Lazy<bool> _UseElevatedShellForSymlinks = new (() => UseElevatedShellForSymlinks());
        private static bool UseElevatedShellForSymlinks()
        {
            if (!OperatingSystem.IsWindows())
                return false;

            // Detect if we can create symlinks without elevation. We know a couple of cases where we can:
            // 1) Running as administrator
            // 2) Developer mode is enabled on Windows 10 and later
            // 3) User is granted the SeCreateSymbolicLinkPrivilege privilege (typically via a group policy)
            // We can test this by trying to create a temporary symlink and see if it works which.
            // It is a lot simpler than trying to detect all those cases individually.
            bool requiresElevation = true;
            string tempSource = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string tempTarget = null;
            try
            {
                tempTarget = Path.GetTempFileName();
                File.CreateSymbolicLink(tempSource, tempTarget);
                requiresElevation = false;
            }
            catch
            {
            }
            finally
            {
                try { File.Delete(tempSource); } catch { }
                try { File.Delete(tempTarget); } catch { }
            }

            return requiresElevation;
        }

        public enum CreateOrUpdateSymbolicLinkResult
        {
            Created,
            Updated,
            AlreadyUpToDate
        }

        /// <summary>
        /// Creates or updates a symbolic link from source to target.
        /// </summary>
        /// <remarks>
        /// On Windows when not running as administrator, this method will attempt to use an elevated shell
        /// to create the symbolic link (requires UAC elevation prompt). On other platforms or when running
        /// as administrator, the managed APIs are used directly.
        /// </remarks>
        /// <param name="source">The path where the symbolic link will be created. Does not need to exist initially.</param>
        /// <param name="target">The path the symbolic link will point to. Must exist.</param>
        /// <param name="isDirectory">If true, creates a directory symbolic link; if false, creates a file symbolic link.</param>
        /// <returns>
        /// <see cref="CreateOrUpdateSymbolicLinkResult.Created"/> if a new symbolic link was created;
        /// <see cref="CreateOrUpdateSymbolicLinkResult.Updated"/> if an existing symbolic link was updated to point to a different target;
        /// <see cref="CreateOrUpdateSymbolicLinkResult.AlreadyUpToDate"/> if the symbolic link already points to the target.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown if source or target paths are null or empty.</exception>
        /// <exception cref="IOException">Thrown if source and target are the same path, or if link creation/update fails.</exception>
        /// <exception cref="Exception">Thrown if the elevated shell fails to start (Windows only).</exception>
        public static CreateOrUpdateSymbolicLinkResult CreateOrUpdateSymbolicLink(string source, string target, bool isDirectory)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Source path cannot be null or empty", nameof(source));
            if (string.IsNullOrWhiteSpace(target))
                throw new ArgumentException("Target path cannot be null or empty", nameof(target));

            // Note: Can't have a slash at end of path and replacing alternate slashes so that resolved link target comparison works correctly
            target = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar);
            source = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar);
            if (source == target)
            {
                throw new IOException("Source and target paths are the same for symbolic link: " + source);
            }

            CreateOrUpdateSymbolicLinkResult result;
            if (isDirectory)
            {
                if (Directory.Exists(source))
                {
                    var linkTarget = Directory.ResolveLinkTarget(source, false);
                    if (linkTarget == null || linkTarget.FullName != target)
                    {
                        result = CreateOrUpdateSymbolicLinkResult.Updated;
                        if (linkTarget == null)
                        {
                            Directory.Delete(source, true); // Not a symlink, delete recursively
                        }
                        else
                        {
                            Directory.Delete(source);
                        }
                    }
                    else
                    {
                        result = CreateOrUpdateSymbolicLinkResult.AlreadyUpToDate;
                    }
                }
                else
                {
                    result = CreateOrUpdateSymbolicLinkResult.Created;
                }
            }
            else
            {
                if (File.Exists(source))
                {
                    var linkTarget = File.ResolveLinkTarget(source, false);
                    if (linkTarget == null || linkTarget.FullName != target)
                    {
                        result = CreateOrUpdateSymbolicLinkResult.Updated;
                        File.SetAttributes(source, FileAttributes.Normal);
                        File.Delete(source);
                    }
                    else
                    {
                        result = CreateOrUpdateSymbolicLinkResult.AlreadyUpToDate;
                    }
                }
                else
                {
                    result = CreateOrUpdateSymbolicLinkResult.Created;
                }
            }

            switch (result)
            {
                case CreateOrUpdateSymbolicLinkResult.Updated:
                    LogWrite($"Updating symbolic link: {source} => {target}");
                    break;

                case CreateOrUpdateSymbolicLinkResult.AlreadyUpToDate:
                    LogWrite($"Symbolic link already up to date: {source} => {target}");
                    return result; // nothing to do bail out

                case CreateOrUpdateSymbolicLinkResult.Created:
                    LogWrite($"Creating symbolic link: {source} => {target}");
                    break;
            }

            // Create intermediate directories
            Directory.CreateDirectory(Path.GetDirectoryName(source));

            if (_UseElevatedShellForSymlinks.Value)
            {
                // Used to create symlink without requiring the whole process to be elevated
                string command = $"mklink {(isDirectory ? "/D" : string.Empty)} \"{source}\" \"{target}\"";
                string arguments = $"/C \"cd /D \"{Environment.CurrentDirectory}\" && {command}\"";
                var processStartInfo = new ProcessStartInfo("cmd", arguments)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        throw new IOException($"Failed creating or updating symbolic link with elevate shell, exited with code {process.ExitCode} for command: {command}");
                    }
                }
                else
                {
                    throw new Exception($"Failed starting elevate shell to create or update symbolic link, command: {command}.");
                }
            }
            else if (isDirectory)
            {
                Directory.CreateSymbolicLink(source, target);
            }
            else
            {
                File.CreateSymbolicLink(source, target);
            }

            return result;
        }

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

        [Obsolete("Sharpmake doesn't support vs2013 anymore.")]
        public static bool IsVisualStudio2013Installed() => false;
        [Obsolete("Sharpmake doesn't support vs2012 anymore.")]
        public static bool IsVisualStudio2012Installed() => false;
        [Obsolete("Sharpmake doesn't support vs2010 anymore.")]
        public static bool IsVisualStudio2010Installed() => false;

        private static bool IsVisualStudioInstalled(DevEnv devEnv)
        {
            if (!OperatingSystem.IsWindows())
                return false;

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

            // We expect folder lib/clang to contain only one subfolder which name is the clang version
            // However in some cases like MacOS, there can be both "16" and "16.0.0", the latter being a symlink.
            // In that case return the shorter one, which matches what we find on other platforms.
            var versionFolders = DirectoryGetDirectories(libDir)
                                .Select(s => Path.GetFileName(s))
                                .Where(s => Regex.IsMatch(s, @"^\d+?(\.\d+?\.\d+?)?$", RegexOptions.Singleline | RegexOptions.CultureInvariant))
                                .OrderBy(s => s.Length)
                                .ToList();

            if (!versionFolders.Any())
                throw new Error($"Couldn't find a version number folder for clang in {llvmInstallDir}");

            // Consider only short version folders if any, else use longer version folders.
            // VS2019 uses long version like "12.0.0" while VS2022 uses short version like "19".
            // Also since VS2022 version 17.13, its possible to have more than one version in the lib/clang folder.
            // Depending on installed VS components. Return the highest version number found.
            var shortVersionFolders = versionFolders.Where(s => !s.Contains('.')).ToList();
            string version;
            if (shortVersionFolders.Any())
            {
                // Can't use System.Version for major version only.
                version = shortVersionFolders.Max(v => int.Parse(v)).ToString();
            }
            else
            {
                // Use System.Version comparer to get the highest version.
                version = versionFolders.Select(v => Version.Parse(v)).Max().ToString();
            }

            return version;
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
            if (IsRunningOnUnix())
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

        [Obsolete("Use " + nameof(GetToolVersionString) + " without the second argument.")]
        public static string GetToolVersionString(DevEnv env, DotNetFramework desiredFramework)
        {
            return GetToolVersionString(env);
        }

        public static string GetToolVersionString(DevEnv env)
        {
            return env.GetVisualProjectToolsVersionString();
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
            return (string.Compare(extension, ".vcxproj", StringComparison.OrdinalIgnoreCase) == 0);
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
                DevEnv devEnv = conf.Target.GetFragment<DevEnv>();
                switch (devEnv)
                {
                    case DevEnv.vs2015:
                    case DevEnv.vs2017:
                    case DevEnv.vs2019:
                    case DevEnv.vs2022:
                        {
                            extension = ".vcxproj";
                        }
                        break;

                    case DevEnv.xcode:
                        return ".xcodeproj";

                    case DevEnv.eclipse:
                        return ".mk";

                    case DevEnv.make:
                        return ".make";

                    default:
                        throw new NotImplementedException("GetProjectFileExtension called with unknown DevEnv: " + devEnv);
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

        [Obsolete("Use GetToolchainPlatformString instead")]
        public static string GetPlatformString(Platform platform, Project project, ITarget target, bool isForSolution = false)
        {
            return GetToolchainPlatformString(platform, project, target, isForSolution);
        }

        public static string GetToolchainPlatformString(Platform platform, Project project, ITarget target, bool isForSolution = false)
        {
            if (project is CSharpProject)
            {
                switch (platform)
                {
                    case Platform.win32:
                        return "x86";
                    case Platform.win64:
                        return "x64";
                    case Platform.anycpu:
                        return isForSolution ? "Any CPU" : "AnyCPU";
                    default:
                        throw new Exception(string.Format("This platform: {0} is not supported", platform));
                }
            }
            else if (project is PythonProject)
            {
                return isForSolution ? "Any CPU" : "AnyCPU";
            }

            return GetToolchainPlatformString(platform, target);
        }

        public static string GetToolchainPlatformString(Platform platform, ITarget target)
        {
            return PlatformRegistry.Query<IPlatformDescriptor>(platform)?.GetToolchainPlatformString(target) ?? platform.ToString();
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

        public static uint Rotl32(uint x, int r)
        {
            return (x << r) | (x >> (32 - r));
        }

        [SupportedOSPlatform("windows")]
        public static object ReadRegistryValue(string key, string value, object defaultValue = null)
        {
            return Registry.GetValue(key, value, defaultValue);
        }

        [SupportedOSPlatform("windows")]
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

            if (OperatingSystem.IsWindows())
            {
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
            }

            if (string.IsNullOrEmpty(key))
                key = fallbackValue;

            s_registryCache.TryAdd(subKeyValueTuple, key);

            return key;
        }

        public class StopwatchProfiler : IDisposable
        {
            private readonly Stopwatch _stopWatch;
            private readonly Action<long> _disposeActionDuration;
            private readonly Action<long, long> _disposeActionStartEnd;
            private readonly long _minThresholdMs;

            public StopwatchProfiler(Action<long> disposeActionDuration)
                : this(disposeActionDuration, 0)
            {
            }

            public StopwatchProfiler(Action<long, long> disposeActionStartEnd)
            {
                _disposeActionStartEnd = disposeActionStartEnd;
                _stopWatch = Stopwatch.StartNew();
                _minThresholdMs = 0;
            }

            public StopwatchProfiler(Action<long> disposeActionDuration, long minThresholdMs)
            {
                _disposeActionDuration = disposeActionDuration;
                _stopWatch = Stopwatch.StartNew();
                _minThresholdMs = minThresholdMs;
            }

            public void Dispose()
            {
                _stopWatch.Stop();
                if (_disposeActionStartEnd != null)
                {
                    long timestamp = Stopwatch.GetTimestamp(); // sadly the stopwatch can't tell us the real start time
                    _disposeActionStartEnd.Invoke(timestamp - _stopWatch.ElapsedTicks, timestamp);
                }

                long elapsed = _stopWatch.ElapsedMilliseconds;
                if (elapsed > _minThresholdMs)
                    _disposeActionDuration?.Invoke(elapsed);
            }
        }

        public class VersionStringComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                if (x == y)
                    return 0;
                var version = new { First = GetVersion(x), Second = GetVersion(y) };
                int limit = Math.Max(version.First.Length, version.Second.Length);
                for (int i = 0; i < limit; i++)
                {
                    int first = version.First.ElementAtOrDefault(i);
                    int second = version.Second.ElementAtOrDefault(i);
                    if (first > second)
                        return 1;
                    if (second > first)
                        return -1;
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

        private static readonly bool s_isUnix = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsRunningOnUnix() => s_isUnix;

        public static Platform GetExecutingPlatform() => s_executingPlatform;

        private static readonly Platform s_executingPlatform = DetectExecutingPlatform();
        private static Platform DetectExecutingPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                switch (RuntimeInformation.OSArchitecture)
                {
                    case Architecture.X86:
                        return Platform.win32;
                    case Architecture.X64:
                        return Platform.win64;
                    default:
                        throw new NotSupportedException($"{RuntimeInformation.OSArchitecture} Architecture is not supported on Windows");
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Platform.mac;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Platform.linux;
            }

            LogWrite("Warning: Couldn't determine running platform");
            return Platform.win64; // arbitrary
        }

        private static readonly string s_framework = Assembly.GetEntryAssembly()?.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkName;
        private static readonly string s_frameworkDisplayName = Assembly.GetEntryAssembly()?.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkDisplayName;
        private static readonly bool s_isDotNetCore = s_framework != null && !s_framework.StartsWith(".NETFramework");
        public static string FrameworkDisplayName() => !string.IsNullOrEmpty(s_frameworkDisplayName) ? s_frameworkDisplayName : !string.IsNullOrEmpty(s_framework) ? s_framework : "Unknown";
        public static bool IsRunningDotNetCore() => s_isDotNetCore;
    }
}
