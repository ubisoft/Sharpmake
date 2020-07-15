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
using System.Linq;

namespace Sharpmake.Generators.Apple
{
    public partial class XCWorkspace : ISolutionGenerator
    {
        // Solution _Solution;
        private Builder _builder;
        private const string SolutionExtension = ".xcworkspace";
        private const string SolutionContentsFileName = "contents.xcworkspacedata";

        public void Generate(Builder builder, Solution solution, List<Solution.Configuration> configurations, string solutionFile, List<string> generatedFiles, List<string> skipFiles)
        {
            _builder = builder;

            FileInfo fileInfo = new FileInfo(solutionFile);
            string solutionPath = fileInfo.Directory.FullName;
            string solutionFileName = fileInfo.Name;

            bool updated;
            string solutionFileResult = Generate(solution, configurations, solutionPath, solutionFileName, out updated);
            if (updated)
                generatedFiles.Add(solutionFileResult);
            else
                skipFiles.Add(solutionFileResult);

            _builder = null;
        }

        private string Generate(Solution solution, List<Solution.Configuration> configurations, string solutionPath, string solutionFile, out bool updated)
        {
            // Create the target folder (solutions and projects are folders in XCode).
            string solutionFolder = Util.GetCapitalizedPath(solutionPath + Path.DirectorySeparatorChar + solutionFile + SolutionExtension);
            Directory.CreateDirectory(solutionFolder);

            // Main solution file.
            string solutionFileContentsPath = solutionFolder + Path.DirectorySeparatorChar + SolutionContentsFileName;
            FileInfo solutionFileContentsInfo = new FileInfo(solutionFileContentsPath);

            bool projectsWereFiltered;
            List<Solution.ResolvedProject> solutionProjects = solution.GetResolvedProjects(configurations, out projectsWereFiltered).ToList();
            solutionProjects.Sort((a, b) => string.Compare(a.ProjectName, b.ProjectName)); // Ensure all projects are always in the same order to avoid random shuffles

            // Move the first executable project on top.
            foreach (Solution.ResolvedProject resolvedProject in solutionProjects)
            {
                if (resolvedProject.Configurations[0].Output == Project.Configuration.OutputType.Exe)
                {
                    solutionProjects.Remove(resolvedProject);
                    solutionProjects.Insert(0, resolvedProject);
                    break;
                }
            }

            if (solutionProjects.Count == 0)
            {
                updated = solutionFileContentsInfo.Exists;
                if (updated)
                    File.Delete(solutionFileContentsPath);
                return solutionFolder;
            }

            var fileGenerator = new FileGenerator();

            fileGenerator.Write(Template.Header);

            foreach (Solution.ResolvedProject resolvedProject in solutionProjects)
            {
                using (fileGenerator.Declare("projectPath", resolvedProject.ProjectFile))
                {
                    fileGenerator.Write(Template.ProjectReferenceAbsolute);
                }
            }

            fileGenerator.Write(Template.Footer);

            // Write the solution file
            updated = _builder.Context.WriteGeneratedFile(solution.GetType(), solutionFileContentsInfo, fileGenerator.ToMemoryStream());

            return solutionFileContentsInfo.FullName;
        }

        private void Write(string value, TextWriter writer, Resolver resolver)
        {
            string resolvedValue = resolver.Resolve(value);
            StringReader reader = new StringReader(resolvedValue);
            string str = reader.ReadToEnd();
            writer.Write(str);
            writer.Flush();
        }
    }
}
