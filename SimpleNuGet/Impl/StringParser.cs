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
using System;
using System.Text.RegularExpressions;

namespace SimpleNuGet.Impl
{
    /// <summary>
    /// A string parser is an object that handles the parsing of ${xyz} tags.
    /// All environment variables are available under the name ${env:[NAME]}
    /// where [NAME] represents the string under which it is available in
    /// the environment.
    /// </summary>
    internal class StringParser
    {
        private static readonly Regex s_regex = new Regex(@"\${env:(.*)}");

        /// <summary>
        /// Replaces the environment variables in the string.
        /// </summary>
        public string Parse(string input)
        {
            if (input == null)
            {
                return null;
            }

            return s_regex.Replace(input, ExpandEnvironmentVariable);
        }

        private string ExpandEnvironmentVariable(Match match)
        {
            var variableName = match.Groups[1].Value.Trim();
            var variableContent = Environment.GetEnvironmentVariable(variableName);

            if (variableContent == null)
            {
                throw new ArgumentException($"{variableName} is not a valid variable.");
            }

            return variableContent;
        }
    }
}
