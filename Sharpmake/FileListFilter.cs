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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Sharpmake
{
    public class FileListFilter
    {
        public Strings FilterFiles = new Strings();
        public Strings FilterFilesExtensions = new Strings();
        public Strings ExcludeFiles = new Strings();
        public Strings ExcludeFilesExtensions = new Strings();

        public bool IsValid(string file)
        {
            string extension = Path.GetExtension(file);
            if (FilterFiles.Count != 0
                || FilterFilesExtensions.Count != 0)
            {
                if (!FilterFiles.Contains(file) &&
                    !FilterFilesExtensions.Contains(extension))
                {
                    return false;
                }
            }

            if (ExcludeFiles.Contains(file))
                return false;

            if (ExcludeFilesExtensions.Contains(extension))
                return false;

            return true;
        }
    }

    public class ForcedIncludesFilter : FileListFilter
    {
        public Strings ForcedIncludes = new Strings();
    }
}
