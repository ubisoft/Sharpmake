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

        public override bool WriteLog { get { return _writeLog; } }
    }
}
