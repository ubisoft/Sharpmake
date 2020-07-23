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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Sharpmake")]
[assembly: AssemblyDescription("Sharpmake core.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Ubisoft")]
[assembly: AssemblyProduct("Sharpmake")]
[assembly: AssemblyCopyright("Copyright \u00A9 Ubisoft 2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is  for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d5ca34b4-73e3-4adb-893a-d1adab5fc719")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion("0.14.4.0")]
#pragma warning disable CS7035
[assembly: AssemblyFileVersion("0.14.4.0 (LocalBuild)")]
#pragma warning restore

[assembly: InternalsVisibleTo("Sharpmake.Application")]
[assembly: InternalsVisibleTo("Sharpmake.Generators")]
[assembly: InternalsVisibleTo("Sharpmake.UnitTests")]
