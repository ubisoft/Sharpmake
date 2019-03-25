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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sharpmake
{
    /// <summary>
    /// ISourceParser classes are tasked with parsing lines in a file.
    /// They can add Sharpmake references through the AssemblerContext.
    /// </summary>
    public interface ISourceParser
    {
        /// <summary>
        /// Parse the current line of the file and update the context if necessary. Parsers MUST NOT assume that the lines will be parsed sequentially.
        /// </summary>
        void ParseLine(string line, FileInfo sourceFilePath, int lineNumber, IAssemblerContext context);
    }

    /// <summary>
    /// A parser that parses C# Attributes, eg [Sharpmake.Include(...)]
    /// </summary>
    public interface ISourceAttributeParser : ISourceParser
    {
    }

    public static class SourceAttributeParserHelpers
    {
        public static Regex CreateAttributeRegex(string attributeTarget, string attributeName, uint parameterCount, params string[] namespaces)
        {
            const string dp = @"(?:/\*.*?\*/|\s)*"; // Discardable Part, we use dp to make it small in the following lines
            string regex = $@"^{dp}\[{dp}";
            if (!string.IsNullOrWhiteSpace(attributeTarget))
            {
                regex += $"{attributeTarget}{dp}:{dp}";
            }
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

        public static Regex CreateModuleAttributeRegex(string attributeName, uint parameterCount, params string[] namespaces)
        {
            return CreateAttributeRegex("module", attributeName, parameterCount, namespaces);
        }
    }

    public abstract class SimpleSourceAttributeParser : ISourceAttributeParser
    {
        private List<Regex> _attributeRegexes = new List<Regex>();

        public SimpleSourceAttributeParser(string attributeName, uint parameterCount, params string[] namespaces)
        {
            _attributeRegexes.Add(SourceAttributeParserHelpers.CreateModuleAttributeRegex(attributeName, parameterCount, namespaces));
        }

        public SimpleSourceAttributeParser(string attributeName, uint parameterMinCount, uint parameterMaxCount, params string[] namespaces)
        {
            for (uint i = parameterMinCount; i <= parameterMaxCount; i++)
            {
                _attributeRegexes.Add(SourceAttributeParserHelpers.CreateModuleAttributeRegex(attributeName, i, namespaces));
            }
        }


        public void ParseLine(string line, FileInfo sourceFilePath, int lineNumber, IAssemblerContext context)
        {
            foreach (Regex attributeRegex in _attributeRegexes)
            {
                Match match = attributeRegex.Match(line);
                if (match.Success)
                    ParseParameter(match.Groups.Cast<Group>().Skip(1).Select(group => group.Captures[0].Value).ToArray(), sourceFilePath, lineNumber, context);
            }
        }

        public abstract void ParseParameter(string[] parameters, FileInfo sourceFilePath, int lineNumber, IAssemblerContext context);
    }
}
