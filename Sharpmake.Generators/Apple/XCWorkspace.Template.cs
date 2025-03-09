// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.Apple
{
    public partial class XCWorkspace
    {
        private static class Template
        {
            public static string Header =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Workspace
   version = ""1.0"">
";
            public static string Footer = "</Workspace>";

            public static string ProjectReference = "\t<FileRef\n\t\tlocation = \"group:[projectName].xcodeproj\">\n\t</FileRef>\n";

            public static string ProjectReferenceAbsolute =
@"[indent]    <FileRef
[indent]        location = ""absolute:[projectPath]"">
[indent]    </FileRef>
";

            public static string ProjectReferenceRelative =
@"[indent]    <FileRef
[indent]        location = ""group:[projectName].xcodeproj"">
[indent]    </FileRef>
";
            public static string GroupBegin =
@"[indent]    <Group
[indent]        location = ""container:""
[indent]        name = ""[folderName]"">
";
            public static string GroupEnd =
@"[indent]    </Group>
";
        }
    }
}
