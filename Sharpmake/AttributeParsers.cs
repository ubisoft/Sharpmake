using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharpmake
{
    public class IncludeAttributeParser : SimpleSourceAttributeParser
    {
        public IncludeAttributeParser() : base("Include", 1, "Sharpmake")
        {
        }

        public override void ParseParameter(string[] parameters, FileInfo sourceFilePath, int lineNumber, IAssemblerContext context)
        {
            string includeFilename = parameters[0];
            string includeAbsolutePath = Path.IsPathRooted(includeFilename) ? includeFilename : null;

            if (Util.IsPathWithWildcards(includeFilename))
            {
                includeAbsolutePath = includeAbsolutePath ?? Path.Combine(sourceFilePath.DirectoryName, includeFilename);
                context.AddSourceFiles(Util.DirectoryGetFilesWithWildcards(includeAbsolutePath));
            }
            else
            {
                includeAbsolutePath = includeAbsolutePath ?? Util.PathGetAbsolute(sourceFilePath.DirectoryName, includeFilename);

                if (!Util.FileExists(includeAbsolutePath))
                    includeAbsolutePath = Util.GetCapitalizedPath(includeAbsolutePath);
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

        public override void ParseParameter(string[] parameters, FileInfo sourceFilePath, int lineNumber, IAssemblerContext context)
        {
            string referenceFilename = parameters[0];
            string referenceAbsolutePath = Path.IsPathRooted(referenceFilename) ? referenceFilename : null;

            if (Util.IsPathWithWildcards(referenceFilename))
            {
                referenceAbsolutePath = referenceAbsolutePath ?? Path.Combine(sourceFilePath.DirectoryName, referenceFilename);
                context.AddReferences(Util.DirectoryGetFilesWithWildcards(referenceAbsolutePath));
            }
            else
            {
                referenceAbsolutePath = referenceAbsolutePath ?? Util.PathGetAbsolute(sourceFilePath.DirectoryName, referenceFilename);

                // Try with the full path
                if (!Util.FileExists(referenceAbsolutePath))
                {
                    // Try next to the Sharpmake binary
                    referenceAbsolutePath = Util.PathGetAbsolute(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location), referenceFilename);

                    if (!File.Exists(referenceAbsolutePath))
                    {
                        // Try in the current working directory
                        referenceAbsolutePath = Util.PathGetAbsolute(Directory.GetCurrentDirectory(), referenceFilename);

                        if (!File.Exists(referenceAbsolutePath))
                        {
                            // Try using .net framework locations
                            referenceAbsolutePath = Assembler.GetAssemblyDllPath(referenceFilename);

                            if (referenceAbsolutePath == null)
                                throw new Error("\t" + sourceFilePath.FullName + "(" + lineNumber + "): error: Sharpmake.Reference file not found: {0}", referenceFilename);
                        }
                    }
                }

                context.AddReference(referenceAbsolutePath);
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
                    throw new Error($"Circual Sharpmake.Package dependency on {includeFilename}");
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
                    throw new Error("\t" + sourceFilePath.FullName + "(" + lineNumber + "): error: Sharpmake.Include file not found {0}", includeFilename);

                files = new string[] { includeAbsolutePath };
            }

            assemblyInfo = context.BuildLoadAndAddReferenceToSharpmakeFilesAssembly(files);
            _assemblies[includeAbsolutePath] = assemblyInfo;
        }
    }

    public class DebugProjectNameAttributeParser : SimpleSourceAttributeParser
    {
        private readonly Dictionary<string, IAssemblyInfo> _assemblies = new Dictionary<string, IAssemblyInfo>(StringComparer.OrdinalIgnoreCase);

        public DebugProjectNameAttributeParser() : base("DebugProjectName", 1, "Sharpmake")
        {
        }

        public override void ParseParameter(string[] parameters, FileInfo sourceFilePath, int lineNumber, IAssemblerContext context)
        {
            context.SetDebugProjectName(parameters[0]);
        }
    }
}
