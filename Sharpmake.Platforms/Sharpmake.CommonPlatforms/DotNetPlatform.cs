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
namespace Sharpmake
{
    [PlatformImplementation(Platform.anycpu, typeof(IPlatformDescriptor))]
    public sealed class DotNetPlatform : BasePlatform
    {
        #region IPlatformDescriptor implementation
        public override string SimplePlatformString => "Any CPU";
        public override bool IsMicrosoftPlatform => true;
        public override bool IsPcPlatform => true;
        public override bool IsUsingClang => false;
        public override bool HasDotNetSupport => true;
        public override bool HasSharedLibrarySupport => true;
        #endregion

        #region IPlatformVcxproj
        public override string SharedLibraryFileExtension => "dll";
        public override string ProgramDatabaseFileExtension => "pdb";
        public override string ExecutableFileExtension => "exe";
        #endregion
    }
}
