// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.Apple
{
    public partial class XCWorkspace
    {
        private static class Template
        {
            internal const string Header =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Workspace
   version = ""1.0"">
";
            internal const string Footer = "</Workspace>";

            internal const string ProjectReference = "\t<FileRef\n\t\tlocation = \"group:[projectName].xcodeproj\">\n\t</FileRef>\n";

            internal const string ProjectReferenceAbsolute =
@"[indent]    <FileRef
[indent]        location = ""absolute:[projectPath]"">
[indent]    </FileRef>
";

            internal const string ProjectReferenceRelative =
@"[indent]    <FileRef
[indent]        location = ""group:[projectName].xcodeproj"">
[indent]    </FileRef>
";
            internal const string GroupBegin =
@"[indent]    <Group
[indent]        location = ""container:""
[indent]        name = ""[folderName]"">
";
            internal const string GroupEnd =
@"[indent]    </Group>
";
        }
    }
}
