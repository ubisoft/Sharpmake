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
using System.Linq;
using System.Text;

namespace Sharpmake.Generators.Apple
{
    public static class XCodeUtil
    {
        public static string XCodeFormatSingleItem(string item, bool forceQuotes = false)
        {
            if (forceQuotes || item.Contains(' ') || item.Contains(Util.DoubleQuotes))
                return $"{Util.DoubleQuotes}{Util.EscapedDoubleQuotes}{item.Replace(Util.DoubleQuotes, @"\\\""")}{Util.EscapedDoubleQuotes}{Util.DoubleQuotes}";
            return $"{item}";
        }

        public static string XCodeFormatList(IEnumerable<string> items, int nbIndent, bool forceQuotes = false)
        {
            int nbItems = items.Count();
            if (nbItems == 0)
                return forceQuotes ? XCodeFormatSingleItem(string.Empty, true) : FileGeneratorUtilities.RemoveLineTag;

            if (nbItems == 1)
                return XCodeFormatSingleItem(items.First(), forceQuotes);

            // Write all selected items.
            var strBuilder = new StringBuilder(1024 * 16);

            string indent = new string('\t', nbIndent);

            strBuilder.Append("(");
            strBuilder.AppendLine();

            foreach (string item in items)
            {
                strBuilder.AppendFormat("{0}\t{1},{2}", indent, XCodeFormatSingleItem(item, forceQuotes), Environment.NewLine);
            }

            strBuilder.AppendFormat("{0})", indent);

            return strBuilder.ToString();
        }

        public static string ResolveProjectPaths(Project project, string stringToResolve)
        {
            Resolver resolver = new Resolver();
            using (resolver.NewScopedParameter("project", project))
            {
                string resolvedString = resolver.Resolve(stringToResolve);
                return Util.SimplifyPath(resolvedString);
            }
        }

        public static void ResolveProjectPaths(Project project, Strings stringsToResolve)
        {
            foreach (string value in stringsToResolve.Values)
            {
                string newValue = ResolveProjectPaths(project, value);
                stringsToResolve.UpdateValue(value, newValue);
            }
        }
    }
}
