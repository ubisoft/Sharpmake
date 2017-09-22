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
    public class RedirectOutput : GenerateAll
    {
        private readonly DirectoryInfo _output;
        private readonly DirectoryInfo _remapRoot;

        public RedirectOutput(DirectoryInfo outputDirectory, DirectoryInfo remapRoot)
            : base(false, true)
        {
            _output = outputDirectory;
            _remapRoot = remapRoot;
        }

        public override bool WriteGeneratedFile(Type type, FileInfo path, MemoryStream generated)
        {
            WriteToSecondaryPath(path, generated);

            string redirectOutputPath = path.FullName.ReplaceHeadPath(_remapRoot?.FullName ?? Path.GetPathRoot(path.FullName), _output.FullName);
            return Util.FileWriteIfDifferentInternal(new FileInfo(redirectOutputPath), generated);
        }
    }
}
