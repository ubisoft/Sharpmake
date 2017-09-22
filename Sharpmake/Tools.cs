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
