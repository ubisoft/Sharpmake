// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.IO;

namespace Sharpmake.BuildContext
{
    public class GenerateAll : BaseBuildContext
    {
        private readonly bool _writeGeneratedFiles;
        private readonly bool _writeLog;

        internal static string s_fileWritesSecondaryPath = string.Empty;

        public GenerateAll(bool writeLog, bool writeGeneratedFiles)
        {
            _writeLog = writeLog;
            _writeGeneratedFiles = writeGeneratedFiles;
        }

        public override bool WriteGeneratedFile(Type type, FileInfo path, MemoryStream generated)
        {
            if (!_writeGeneratedFiles)
                return Util.IsFileDifferent(path, generated);

            WriteToSecondaryPath(path, generated);
            return Util.FileWriteIfDifferentInternal(path, generated);
        }

        internal static void WriteToSecondaryPath(FileInfo file, MemoryStream stream)
        {
            // If a secondary path was specified to the commandline, also write a file under that folder with
            // full path hierarchy.
            if (s_fileWritesSecondaryPath == string.Empty)
                return;

            string alternateFilePath = file.FullName;
            if (Path.IsPathRooted(file.FullName))
                alternateFilePath = file.FullName.Substring(Path.GetPathRoot(file.FullName).Length);
            FileInfo alternateFileInfo = new FileInfo(Path.Combine(s_fileWritesSecondaryPath, alternateFilePath));
            Util.FileWriteIfDifferentInternal(alternateFileInfo, stream, bypassAutoCleanupDatabase: true);
        }

        public override bool WriteGeneratedFile(Type type, FileInfo path, IFileGenerator generator)
        {
            if (!_writeGeneratedFiles)
                return generator.IsFileDifferent(path);

            WriteToSecondaryPath(path, generator);
            return generator.FileWriteIfDifferent(path);
        }

        internal static void WriteToSecondaryPath(FileInfo file, IFileGenerator generator)
        {
            // If a secondary path was specified to the commandline, also write a file under that folder with
            // full path hierarchy.
            if (s_fileWritesSecondaryPath == string.Empty)
                return;

            string alternateFilePath = file.FullName;
            if (Path.IsPathRooted(file.FullName))
                alternateFilePath = file.FullName.Substring(Path.GetPathRoot(file.FullName).Length);
            FileInfo alternateFileInfo = new FileInfo(Path.Combine(s_fileWritesSecondaryPath, alternateFilePath));
            generator.FileWriteIfDifferent(alternateFileInfo, bypassAutoCleanupDatabase: true);
        }

        public override bool WriteLog { get { return _writeLog; } }
    }
}
