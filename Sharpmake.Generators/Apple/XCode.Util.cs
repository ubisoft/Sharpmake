// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sharpmake.Generators.Apple
{
    public static class XCodeUtil
    {
        public const string CustompropertiesFilename = "customproperties.xml";

        public static string XCodeFormatSingleItem(string item, bool forceQuotes = false)
        {
            if (forceQuotes || item.Contains(Util.DoubleQuotes) || item.Contains(' '))
                return $"{Util.DoubleQuotes}{Util.EscapedDoubleQuotes}{item.Replace(Util.DoubleQuotes, @"\\\""")}{Util.EscapedDoubleQuotes}{Util.DoubleQuotes}";
            else if (item.Contains('+'))
                return $"{Util.DoubleQuotes}{item}{Util.DoubleQuotes}";
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

        public static string ResolveProjectVariable(Project project, string stringToResolve)
        {
            Resolver resolver = new Resolver();
            using (resolver.NewScopedParameter("project", project))
            {
                return resolver.Resolve(stringToResolve);
            }
        }

        public static string ResolveProjectPaths(Project project, string stringToResolve)
        {
            return Util.SimplifyPath(ResolveProjectVariable(project, stringToResolve));
        }

        public static void ResolveProjectPaths(Project project, Strings stringsToResolve)
        {
            foreach (string value in stringsToResolve.Values)
            {
                string newValue = ResolveProjectPaths(project, value);
                stringsToResolve.UpdateValue(value, newValue);
            }
        }

        public static void ResolveProjectPaths(Project project, OrderableStrings stringsToResolve)
        {
            var count = stringsToResolve.Count;
            for (var i = 0; i < count; i++)
            {
                string value = stringsToResolve[i];
                string newValue = ResolveProjectPaths(project, value);
                stringsToResolve[i] = newValue;
            }
        }
    }
}
