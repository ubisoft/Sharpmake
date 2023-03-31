// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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

        public override bool WriteGeneratedFile(Type type, FileInfo path, IFileGenerator generator)
        {
            WriteToSecondaryPath(path, generator);

            string redirectOutputPath = path.FullName.ReplaceHeadPath(_remapRoot?.FullName ?? Path.GetPathRoot(path.FullName), _output.FullName);
            return generator.FileWriteIfDifferent(new FileInfo(redirectOutputPath));
        }
    }
}
