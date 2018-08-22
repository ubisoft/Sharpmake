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
            const string dp = @"(?:/\*.*?\*/|\s)*"; // Discardable Part, we use _dp to make it small in the following line
            string regex = $@"^{dp}\[{dp}module{dp}:{dp}";
            foreach (var ns in namespaces)
            {
                regex += $@"{Regex.Escape(ns)}{dp}\.{dp}";
            }
            regex += $@"{Regex.Escape(attributeName)}{dp}";
            if (parameterCount == 0)
            {
                regex += $@"(?:\({dp}\))";
            }
            else
            {
                regex += $@"\({dp}@?\""([^""]*)""{dp}";
                for (int i = 1; i < parameterCount; ++i)
                {
                    regex += $@",{dp}@?\""([^""]*)""{dp}";
                }
                regex += $@"\)";
            }
            regex += $@"{dp}\]";
            return new Regex(regex, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        }
    }

    public abstract class SimpleSourceAttributeParser : ISourceAttributeParser
    {
        private Regex AttributeRegex { get; }

        public SimpleSourceAttributeParser(string attributeName, uint parameterCount, params string[] namespaces)
        {
            AttributeRegex = SourceAttributeParserHelpers.CreateAttributeRegex(attributeName, parameterCount, namespaces);
        }

        public void ParseLine(string line, FileInfo sourceFilePath, int lineNumber, IAssemblerContext context)
        {
            Match match = AttributeRegex.Match(line);
            if (match.Success)
                ParseParameter(match.Groups.Cast<Group>().Skip(1).Select(group => group.Captures[0].Value).ToArray(), sourceFilePath, lineNumber, context);
        }

        public abstract void ParseParameter(string[] parameters, FileInfo sourceFilePath, int lineNumber, IAssemblerContext context);
    }
}
