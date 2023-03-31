// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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

        private Dictionary<string, SolutionFolder> _solutionFolderCache = new Dictionary<string, SolutionFolder>();

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

            _solutionFolderCache.Clear();
            List<SolutionFile> solutionsFiles = solutionProjects.Select(project => { var solutionFileItem = new SolutionFile() { Name = project.ProjectFile, Parent = ParseSolutionFolder(project.SolutionFolder) }; solutionFileItem.RegisterToParent(); return solutionFileItem; }).ToList();
            List<SolutionItem> solutionsItems = solutionsFiles.GroupBy(solutionsItem => solutionsItem.GetRoot()).Select(group => group.Key).ToList();
            solutionsItems.Sort((a, b) => string.Compare(a.Name, b.Name));

            foreach (var solutionItem in solutionsItems)
            {
                //Sort of folders content
                (solutionItem as SolutionFolder)?.Sort();
                WriteSolutionItem(fileGenerator, solutionItem);
            }

            fileGenerator.Write(Template.Footer);

            // Write the solution file
            updated = _builder.Context.WriteGeneratedFile(solution.GetType(), solutionFileContentsInfo, fileGenerator);

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

        private static void WriteSolutionItem(FileGenerator fileGenerator, SolutionItem solutionItem, int nbIndent = 0)
        {
            bool isFolder = !solutionItem.IsFile;
            string indent = new string('\t', nbIndent);
            if (isFolder)
            {
                SolutionFolder solutionFolder = solutionItem as SolutionFolder;

                using (fileGenerator.Declare("folderName", solutionFolder.Name))
                using (fileGenerator.Declare("indent", indent))
                {
                    fileGenerator.Write(Template.GroupBegin);
                }

                foreach (var child in solutionFolder.Childs)
                {
                    WriteSolutionItem(fileGenerator, child, nbIndent + 1);
                }

                using (fileGenerator.Declare("indent", indent))
                {
                    fileGenerator.Write(Template.GroupEnd);
                }
            }
            else
            {
                using (fileGenerator.Declare("projectPath", solutionItem.Name))
                using (fileGenerator.Declare("indent", indent))
                {
                    fileGenerator.Write(Template.ProjectReferenceAbsolute);
                }
            }
        }

        [System.Diagnostics.DebuggerDisplay("{Path}")]
        private class SolutionItem
        {
            public string Name;
            public string Path
            {
                get
                {
                    if (Parent == null)
                        return Name;

                    return System.IO.Path.Combine(Parent.Path, Name);
                }
            }

            public SolutionItem Parent;

            public virtual bool IsFile => true;

            public SolutionItem GetRoot()
            {
                return Parent == null ? this : Parent.GetRoot();
            }
        }

        private class SolutionFile : SolutionItem
        {
            public override bool IsFile => true;
            public void RegisterToParent()
            {
                var parent = Parent as SolutionFolder;
                if (parent != null)
                    parent.Childs.Add(this);
            }
        }

        private class SolutionFolder : SolutionItem
        {
            public List<SolutionItem> Childs = new List<SolutionItem>();
            public void Sort()
            {
                Childs.Sort((a, b) => string.Compare(a.Name, b.Name));
                foreach (var child in Childs)
                {
                    (child as SolutionFolder)?.Sort();
                }
            }
            public override bool IsFile => false;
        }

        private SolutionFolder ParseSolutionFolder(string solutionFolders)
        {
            if (string.IsNullOrEmpty(solutionFolders))
                return null;

            SolutionFolder result = null;

            string path = MakeStandartPath(solutionFolders);
            if (!_solutionFolderCache.TryGetValue(path, out result))
            {
                List<string> solutionFolderStack = path.Split(Util._pathSeparators, System.StringSplitOptions.RemoveEmptyEntries).ToList();
                string folderName = solutionFolderStack.Last();
                solutionFolderStack.RemoveAt(solutionFolderStack.Count - 1);
                SolutionFolder parent = ParseSolutionFolder(string.Join(Path.DirectorySeparatorChar.ToString(), solutionFolderStack));

                result = new SolutionFolder()
                {
                    Name = folderName,
                    Parent = parent
                };

                if (parent != null)
                {
                    parent.Childs.Add(result);
                }

                _solutionFolderCache.Add(path, result);
            }
            return result;
        }

        private string MakeStandartPath(string path)
        {
            return string.Join(Path.DirectorySeparatorChar.ToString(), path.Split(Util._pathSeparators, System.StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
