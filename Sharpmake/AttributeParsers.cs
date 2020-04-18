// Copyright (c) 2017 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;

namespace Sharpmake
{
    public class IncludeAttributeParser : SimpleSourceAttributeParser
    {
        public IncludeAttributeParser() : base("Include", 1, 2, "Sharpmake")
        {
        }

        private string MatchIncludeInParentPath(string filePath, string initialDirectory, IncludeType includeMatchType)
        {
            string matchPath = Path.Combine(initialDirectory, filePath);
            bool matchPathExists = Util.FileExists(matchPath);

            if (matchPathExists && includeMatchType == IncludeType.NearestMatchInParentPath)
            {
                return matchPath;
            }

            // backtrace one level in the path
            string matchResult = null;
            DirectoryInfo info = Directory.GetParent(initialDirectory);
            if (info != null)
            {
                string parentPath = info.FullName;

                if (!parentPath.Equals(initialDirectory))
                {
                    matchResult = MatchIncludeInParentPath(filePath, parentPath, includeMatchType);
                }
            }

            if (matchPathExists && matchResult == null)
                return matchPath;

            return matchResult;
        }
        public override void ParseParameter(string[] parameters, FileInfo sourceFilePath, int lineNumber, IAssemblerContext context)
        {
            string includeFilename = parameters[0];
            IncludeType matchType = IncludeType.Relative;
            if (parameters.Length > 1)
            {
                string incType = parameters[1].Replace("Sharpmake.", "");
                incType = incType.Replace("IncludeType.", "");
                if (!Enum.TryParse<IncludeType>(incType, out matchType))
                {
                    throw new Error("\t" + sourceFilePath.FullName + "(" + lineNumber + "): error: Sharpmake.Include invalid include type used ({0})", parameters[1]);
                }
            }
            string includeAbsolutePath = Path.IsPathRooted(includeFilename) ? includeFilename : null;

            if (Util.IsPathWithWildcards(includeFilename))
            {
                if (matchType != IncludeType.Relative)
                {
                    throw new Error("\t" + sourceFilePath.FullName + "(" + lineNumber + "): error: Sharpmake.Include with non-relative match types, wildcards are not supported ({0})", includeFilename);
                }
                includeAbsolutePath = includeAbsolutePath ?? Path.Combine(sourceFilePath.DirectoryName, includeFilename);
                context.AddSourceFiles(Util.DirectoryGetFilesWithWildcards(includeAbsolutePath));
            }
            else
            {
                includeAbsolutePath = includeAbsolutePath ?? Util.PathGetAbsolute(sourceFilePath.DirectoryName, includeFilename);

                if (matchType == IncludeType.Relative)
                {
                    if (!Util.FileExists(includeAbsolutePath))
                        includeAbsolutePath = Util.GetCapitalizedPath(includeAbsolutePath);
                }
                else
                {
                    string matchIncludeInParentPath = MatchIncludeInParentPath(includeFilename, sourceFilePath.DirectoryName, matchType);
                    if (matchIncludeInParentPath == null)
                        throw new Error("\t" + sourceFilePath.FullName + "(" + lineNumber + "): error: Sharpmake.Include file not found '{0}'[{1}]. Search started from '{2}'", includeFilename, matchType, sourceFilePath.DirectoryName);

                    includeAbsolutePath = Util.GetCapitalizedPath(matchIncludeInParentPath);
                }

                if (!Util.FileExists(includeAbsolutePath))
                    throw new Error("\t" + sourceFilePath.FullName + "(" + lineNumber + "): error: Sharpmake.Include file not found {0}", includeFilename);

                context.AddSourceFile(includeAbsolutePath);
            }
        }
    }

    public class ReferenceAttributeParser : SimpleSourceAttributeParser
    {
        public ReferenceAttributeParser() : base("Reference", 1, "Sharpmake")
        {
        }

        private IEnumerable<string> EnumerateReferencePathCandidates(FileInfo sourceFilePath, string reference)
        {
            // Try with the full path
            if (Path.IsPathRooted(reference))
                yield return reference;

            // Try relative from the sharpmake file
            yield return Util.PathGetAbsolute(sourceFilePath.DirectoryName, reference);

            // Try next to the Sharpmake binary
            string pathToBinary = System.Reflection.Assembly.GetEntryAssembly()?.Location;
            if (!string.IsNullOrEmpty(pathToBinary))
                yield return Util.PathGetAbsolute(Path.GetDirectoryName(pathToBinary), reference);

            // In some cases, the main module is not the current binary, so try it if so
            string mainModule = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            if (!string.IsNullOrEmpty(mainModule) && pathToBinary != mainModule)
                yield return Util.PathGetAbsolute(Path.GetDirectoryName(mainModule), reference);

            // Try in the current working directory
            yield return Util.PathGetAbsolute(Directory.GetCurrentDirectory(), reference);

            // Try using .net framework locations
            foreach (string frameworkDirectory in Assembler.EnumeratePathToDotNetFramework())
                yield return Path.Combine(Path.Combine(frameworkDirectory, reference));
        }

        public override void ParseParameter(string[] parameters, FileInfo sourceFilePath, int lineNumber, IAssemblerContext context)
        {
            string reference = parameters[0];

            if (Util.IsPathWithWildcards(reference))
            {
                string referenceAbsolutePath = Path.IsPathRooted(reference) ? reference : null;
                referenceAbsolutePath = referenceAbsolutePath ?? Path.Combine(sourceFilePath.DirectoryName, reference);
                context.AddReferences(Util.DirectoryGetFilesWithWildcards(referenceAbsolutePath));
            }
            else
            {
                bool foundReference = false;
                foreach (string candidateReferenceLocation in EnumerateReferencePathCandidates(sourceFilePath, reference))
                {
                    if (Util.FileExists(candidateReferenceLocation))
                    {
                        context.AddReference(candidateReferenceLocation);
                        foundReference = true;
                        break;
                    }
                }

                if (!foundReference)
                {
                    throw new Error(
                        "\t{0}({1}): error: Sharpmake.Reference file not found: {2}{3}Those paths were evaluated as candidates:{3}  - {4}",
                        sourceFilePath.FullName,
                        lineNumber,
                        reference,
                        Environment.NewLine,
                        string.Join(Environment.NewLine + "  - ", EnumerateReferencePathCandidates(sourceFilePath, reference))
                    );
                }
            }
        }
    }

    public class PackageAttributeParser : SimpleSourceAttributeParser
    {
        private readonly Dictionary<string, IAssemblyInfo> _assemblies = new Dictionary<string, IAssemblyInfo>(StringComparer.OrdinalIgnoreCase);

        public PackageAttributeParser() : base("Package", 1, "Sharpmake")
        {
        }

        public override void ParseParameter(string[] parameters, FileInfo sourceFilePath, int lineNumber, IAssemblerContext context)
        {
            string includeFilename = parameters[0];
            string includeAbsolutePath;
            if (Path.IsPathRooted(includeFilename))
            {
                includeAbsolutePath = includeFilename;
            }
            else if (Util.IsPathWithWildcards(includeFilename))
            {
                includeAbsolutePath = Path.Combine(sourceFilePath.DirectoryName, includeFilename);
            }
            else
            {
                includeAbsolutePath = Util.PathGetAbsolute(sourceFilePath.DirectoryName, includeFilename);
            }

            IAssemblyInfo assemblyInfo;
            if (_assemblies.TryGetValue(includeAbsolutePath, out assemblyInfo))
            {
                if (assemblyInfo == null)
                    throw new Error($"Circular Sharpmake.Package dependency on {includeFilename}");
                context.AddReference(assemblyInfo);
                return;
            }
            _assemblies[includeAbsolutePath] = null;

            string[] files;
            if (Util.IsPathWithWildcards(includeFilename))
            {
                files = Util.DirectoryGetFilesWithWildcards(includeAbsolutePath);
            }
            else
            {
                if (!Util.FileExists(includeAbsolutePath))
                    includeAbsolutePath = Util.GetCapitalizedPath(includeAbsolutePath);
                if (!Util.FileExists(includeAbsolutePath))
                    throw new Error("\t" + sourceFilePath.FullName + "(" + lineNumber + "): error: Sharpmake.Package file not found {0}", includeFilename);

                files = new string[] { includeAbsolutePath };
            }

            assemblyInfo = context.BuildLoadAndAddReferenceToSharpmakeFilesAssembly(files);
            _assemblies[includeAbsolutePath] = assemblyInfo;
        }
    }

    public class DebugProjectNameAttributeParser : SimpleSourceAttributeParser
    {
        public DebugProjectNameAttributeParser() : base("DebugProjectName", 1, "Sharpmake")
        {
        }

        public override void ParseParameter(string[] parameters, FileInfo sourceFilePath, int lineNumber, IAssemblerContext context)
        {
            context.SetDebugProjectName(parameters[0]);
        }
    }
}
