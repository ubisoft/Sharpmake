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
    public static partial class Durango
    {
        [PlatformImplementation(Platform.durango, typeof(ICommandLineInterface))]
        public sealed class DurangoCommandLineInterface : ICommandLineInterface
        {
            public void Validate()
            {
                // Nothing to validate.
            }

            [CommandLine.Option("xdkedition", @"Set the Microsoft XDK Edition ex: /xdkedition(""151100"")")]
            public void CommandLineXDKEdition(string xdkEdition)
            {
                GlobalSettings.XdkEditionTarget = xdkEdition;
            }

            [CommandLine.Option("ignoremissingxdkedition", @"Ignore Missing XDKEdition Path, used on TG build machines when building on other platforms.: /ignoremissingxdkedition")]
            public void CommandLineIgnoreMissingXDKEdition(string xdkEdition)
            {
                GlobalSettings.IgnoreMissingXdkEditionTargetPath = true;
            }
        }
    }
}