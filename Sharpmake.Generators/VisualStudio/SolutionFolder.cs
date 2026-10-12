// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sharpmake.Generators.VisualStudio
{
    // A node in the solution-folder tree declared via ResolvedProject.SolutionFolder paths
    // (e.g. "A/B"). Shared by the .sln and .slnx generators so folder identity/GUIDs stay
    // consistent between them.
    [DebuggerDisplay("{Path} - {Guid}")]
    internal class SolutionFolder
    {
        public string Name;
        public string Path
        {
            get
            {
                if (Parent == null)
                    return Name;
                return Parent.Path + System.IO.Path.DirectorySeparatorChar + Name;
            }
        }

        public SolutionFolder Parent;
        public List<SolutionFolder> Childs = new List<SolutionFolder>();

        // Used by both generators to identify the folder (.sln: as the folder's project GUID;
        // .slnx: only as a stable sort tiebreak, since .slnx folders are identified by path).
        public Guid Guid;
    }

    // Builds and tracks the tree of solution folders for a single solution-generation pass.
    internal class SolutionFolderTree
    {
        public readonly List<SolutionFolder> RootFolders = new List<SolutionFolder>();
        public readonly List<SolutionFolder> AllFolders = new List<SolutionFolder>();

        public void Clear()
        {
            RootFolders.Clear();
            AllFolders.Clear();
        }

        public SolutionFolder GetSolutionFolder(string names)
        {
            if (names == null)
                return null;

            string[] nameList = names.Split(Util._pathSeparators, StringSplitOptions.RemoveEmptyEntries);

            SolutionFolder result = null;
            SolutionFolder parent = null;

            foreach (string name in nameList)
            {
                result = null;
                List<SolutionFolder> childs = parent == null ? RootFolders : parent.Childs;

                foreach (SolutionFolder child in childs)
                {
                    if (child.Name == name)
                    {
                        result = child;
                        break;
                    }
                }

                if (result == null)
                {
                    result = new SolutionFolder();
                    result.Name = name;
                    result.Parent = parent;
                    result.Guid = Util.BuildGuid(result.Path);
                    childs.Add(result);
                    AllFolders.Add(result);
                }
                parent = result;
            }

            return result;
        }

        // Ensures folders are always emitted in a stable order regardless of discovery order.
        public void SortStably()
        {
            AllFolders.Sort((a, b) =>
            {
                int nameComparison = string.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase);
                if (nameComparison != 0)
                    return nameComparison;
                return a.Guid.CompareTo(b.Guid);
            });
        }
    }
}
