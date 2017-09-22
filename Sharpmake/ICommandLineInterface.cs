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
    /// <summary>
    /// Interface for objects that expose additional command line interfaces for a given platform.
    /// This allows platforms to extend the command line interface of Sharpmake.
    /// </summary>
    public interface ICommandLineInterface
    {
        /// <summary>
        /// Validates that the command line arguments are valid.
        /// </summary>
        void Validate();
    }
}
