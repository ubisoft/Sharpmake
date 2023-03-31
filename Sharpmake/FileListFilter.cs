// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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
