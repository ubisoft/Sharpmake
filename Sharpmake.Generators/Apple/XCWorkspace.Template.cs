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
@"   <FileRef
      location = ""absolute:[projectPath]"">
   </FileRef>
";

            public static string ProjectReferenceRelative =
@"   <FileRef
      location = ""group:[projectName].xcodeproj"">
   </FileRef>
";
        }
    }
}
