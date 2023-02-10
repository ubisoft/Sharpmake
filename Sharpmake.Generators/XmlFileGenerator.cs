// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Text;

namespace Sharpmake.Generators
{
    internal class XmlFileGenerator : FileGenerator
    {
        private static readonly Encoding s_xmlEncoding = new UTF8Encoding(true);

        public XmlFileGenerator()
            : base(s_xmlEncoding)
        { }

        public XmlFileGenerator(Resolver resolver)
            : base(s_xmlEncoding, resolver)
        { }
    }
}
