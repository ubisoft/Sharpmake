// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Sharpmake
{
    public static class Tools
    {
        private static readonly Regex s_vcprojLogFileRegex = new Regex(@"^\s*RelativePath=\""((?<FILE>([^\""]*)+))", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        public static bool ProjectLogFiles(string projectFile)
        {
            List<string> sourceFiles = new List<string>();
            FileInfo projectFileInfo = new FileInfo(projectFile);

            if (!projectFileInfo.Exists)
                return false;

            using (StreamReader projectStream = projectFileInfo.OpenText())
            {
                string line = projectStream.ReadLine();
                while (line != null)
                {
                    Match match = s_vcprojLogFileRegex.Match(line);

                    if (match.Success)
                    {
                        string relativeFileName = match.Groups["FILE"].ToString();
                        string fileName = Util.PathGetAbsolute(projectFileInfo.Directory.FullName, relativeFileName);
                        sourceFiles.Add(fileName);
                    }
                    line = projectStream.ReadLine();
                }
            }

            if (sourceFiles.Count == 0)
                return false;

            sourceFiles.Sort();


            MemoryStream memoryStream = new MemoryStream();
            StreamWriter streamWriter = new StreamWriter(memoryStream);

            foreach (string file in sourceFiles)
                streamWriter.WriteLine(file);

            streamWriter.Flush();
            FileInfo outputFileInfo = new FileInfo(projectFileInfo.FullName + ".files.log");
            Builder.Instance.Context.WriteGeneratedFile(null, outputFileInfo, memoryStream);
            streamWriter.Close();

            return true;
        }
    }
}
