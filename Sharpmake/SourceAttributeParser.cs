// Copyright (c) 2017 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sharpmake
{
    public interface ISourceAttributeParser
    {
        void ParseLine(string line, FileInfo sourceFilePath, int lineNumber, IAssemblerContext context);
    }

    public static class SourceAttributeParserHelpers
    {
        public static Regex CreateAttributeRegex(string attributeName, uint parameterCount, params string[] namespaces)
        {
            const string dp = @"(?:/\*.*?\*/|\s)*"; // Discardable Part, we use dp to make it small in the following lines
            string regex = $@"^{dp}\[{dp}module{dp}:{dp}";
            // Handle namespaces being optional
            // First we open groups for every namespace
            foreach (var ns in namespaces)
            {
                regex += "(?:";
            }
            // Then we write a namespace, close the group, mark it as optional
            // For {"Sharpmake", "MyNs"}, the result, ignoring spaces, will be (?:(?:Sharpmake\.)?MyNs\.)?
            // This means we accept either no prefix, MyNs. prefix, or Sharpmake.MyNs. prefix, but not the Sharpmake. prefix.
            foreach (var ns in namespaces)
            {
                regex += $@"{Regex.Escape(ns)}{dp}\.{dp})?";
            }
            regex += $@"{Regex.Escape(attributeName)}{dp}";
            if (parameterCount == 0)
            {
                regex += $@"(?:\({dp}\))";
            }
            else
            {
                regex += $@"\({dp}@?\""?([^""]*)""?{dp}";
                for (int i = 1; i < parameterCount; ++i)
                {
                    regex += $@",{dp}@?\""?([^""]*)""?{dp}";
                }
                regex += $@"\)";
            }
            regex += $@"{dp}\]";
            return new Regex(regex, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        }
    }

    public abstract class SimpleSourceAttributeParser : ISourceAttributeParser
    {
        private List<Regex> AttributeRegexes = new List<Regex>();

        public SimpleSourceAttributeParser(string attributeName, uint parameterCount, params string[] namespaces)
        {
            AttributeRegexes.Add(SourceAttributeParserHelpers.CreateAttributeRegex(attributeName, parameterCount, namespaces));
        }

        public SimpleSourceAttributeParser(string attributeName, uint parameterMinCount, uint parameterMaxCount, params string[] namespaces)
        {
            for (uint i = parameterMinCount; i <= parameterMaxCount; i++)
            {
                AttributeRegexes.Add(SourceAttributeParserHelpers.CreateAttributeRegex(attributeName, i, namespaces));
            }
        }


        public void ParseLine(string line, FileInfo sourceFilePath, int lineNumber, IAssemblerContext context)
        {
            foreach (Regex attributeRegex in AttributeRegexes)
            {
                Match match = attributeRegex.Match(line);
                if (match.Success)
                    ParseParameter(match.Groups.Cast<Group>().Skip(1).Select(group => group.Captures[0].Value).ToArray(), sourceFilePath, lineNumber, context);
            }
        }

        public abstract void ParseParameter(string[] parameters, FileInfo sourceFilePath, int lineNumber, IAssemblerContext context);
    }
}
