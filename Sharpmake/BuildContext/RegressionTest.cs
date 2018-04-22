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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Sharpmake.BuildContext
{
    public class RegressionTest : GenerateAll
    {
        public enum FileStatus
        {
            NotGenerated,
            Similar,
            Different
        }

        public class OutputInfo
        {
            public OutputInfo(string referencePath, FileStatus fileStatus)
            {
                ReferencePath = referencePath;
                FileStatus = fileStatus;
            }
            public OutputInfo(string referencePath, string outputPath, FileStatus fileStatus)
            {
                ReferencePath = referencePath;
                OutputPath = outputPath;
                FileStatus = fileStatus;
            }
            public FileStatus FileStatus { get; set; }
            public string ReferencePath { get; private set; }
            public string OutputPath { get; set; }
        }

        public IEnumerable<OutputInfo> GetRegressions()
        {
            foreach (var entry in _referenceDifferenceMap)
            {
                switch (entry.Value.FileStatus)
                {
                    case FileStatus.Similar:
                        // intentionally do nothing... it is the one intended case
                        break;
                    case FileStatus.Different:
                        yield return entry.Value;
                        break;
                    case FileStatus.NotGenerated:
                    default:
                        var outPath = entry.Key.Replace(Reference.FullName, Output.FullName);
                        var outFileInfo = new FileInfo(outPath);

                        if (outFileInfo.Exists)
                        {
                            Util.LogWrite($"Warning: {outPath} is present in the output but wasn't properly tracked by the regression tester."
                                + "It was probably written manually from the project files."
                                + "Please use Util.FileWriteIsDifferent instead to write files.");

                            using (var reference = File.OpenRead(entry.Key))
                                entry.Value.FileStatus = Util.IsFileDifferent(outFileInfo, reference) ? FileStatus.Different : FileStatus.Similar;
                        }
                        yield return entry.Value;
                        break;
                }
            }
        }

        public readonly DirectoryInfo Output;
        public readonly DirectoryInfo Reference;
        public readonly DirectoryInfo RemapRoot;

        private readonly ConcurrentDictionary<string, OutputInfo> _referenceDifferenceMap = new ConcurrentDictionary<string, OutputInfo>(StringComparer.OrdinalIgnoreCase);

        public RegressionTest(DirectoryInfo outputDirectory, DirectoryInfo referenceDirectory, DirectoryInfo remapRoot)
            : base(false, true)
        {
            Output = outputDirectory;
            Reference = referenceDirectory;
            RemapRoot = remapRoot;
            FillReferenceMapRecursive(referenceDirectory);

            // delete the output directory subfolders and files so all files are generated files
            if (Output.Exists)
                Output.Delete(true);

            // recreate it just in case it makes something crash... we never know
            Output.Create();
        }

        private void FillReferenceMapRecursive(DirectoryInfo reference)
        {
            foreach (var file in reference.GetFiles())
            {
                _referenceDifferenceMap.TryAdd(file.FullName, new OutputInfo(file.FullName, FileStatus.NotGenerated));
            }
            foreach (var subdirectory in reference.GetDirectories())
            {
                FillReferenceMapRecursive(subdirectory);
            }
        }

        public override bool WriteGeneratedFile(Type type, FileInfo path, MemoryStream generated)
        {
            var rootPath = RemapRoot?.FullName ?? Path.GetPathRoot(path.FullName);
            var outFileInfo = new FileInfo(path.FullName.ReplaceHeadPath(rootPath, Output.FullName));
            var refFileInfo = new FileInfo(path.FullName.ReplaceHeadPath(rootPath, Reference.FullName));

            bool isDifferent = Util.IsFileDifferent(refFileInfo, generated);
            var outputInfo = new OutputInfo(refFileInfo.FullName, outFileInfo.FullName, isDifferent ? FileStatus.Different : FileStatus.Similar);
            _referenceDifferenceMap[refFileInfo.FullName] = outputInfo;

            Util.FileWriteIfDifferentInternal(outFileInfo, generated);

            return isDifferent;
        }
    }
}
