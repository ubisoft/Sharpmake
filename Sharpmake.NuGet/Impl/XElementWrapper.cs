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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Sharpmake.NuGet.Impl
{
    /// <summary>
    /// Provides a typed interface around a XElement.
    /// </summary>
    public class XElementWrapper
    {
        public XContainer Xml { get; protected set; }

        public XElementWrapper(XContainer xml)
        {
            Xml = xml;
        }

        /// <summary>
        /// Returns the first element with the given name, or add a new one if non defined.
        /// </summary>
        protected XContainer GetOrCreateElement(string elementName)
        {
            XElement result;
            if (!TryGetElement(elementName, out result))
            {
                result = new XElement(XName.Get(FormatPropertyName(elementName), DefaultNamespace));
                Xml.Add(result);
            }

            return result;
        }

        protected XContainer AddElement(string elementName)
        {
            var result = new XElement(XName.Get(FormatPropertyName(elementName), DefaultNamespace));
            Xml.Add(result);

            return result;
        }

        private bool TryGetElement(string name, out XElement result)
        {
            result = Xml.Descendants().FirstOrDefault(x => string.Equals(x.Name.LocalName, name, StringComparison.OrdinalIgnoreCase));
            return result != null;
        }

        protected IEnumerable<XElement> Elements(string name)
        {
            return Xml.Elements().Where(x => string.Equals(x.Name.LocalName, name, StringComparison.OrdinalIgnoreCase));
        }

        protected XDocument LoadXmlDocument(Stream stream)
        {
            var xmlSettings = new XmlReaderSettings
            {
                XmlResolver = null,
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreWhitespace = true
            };

            using (var reader = XmlReader.Create(stream, xmlSettings))
            {
                return XDocument.Load(reader);
            }
        }

        protected void SaveXmlDocument(Stream stream)
        {
            var asString = ToXmlString();

            using (var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
            {
                writer.Write(asString);
            }
        }

        protected string ToXmlString()
        {
            if (Xml.Document == null)
            {
                throw new InvalidOperationException("Make sure to have a least a document defined before saving.");
            }

            var xmlSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
            };

            string asString;
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(memoryStream, xmlSettings))
                {
                    Xml.Document.WriteTo(writer);
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                asString = new StreamReader(memoryStream).ReadToEnd();
            }

            return new StringParser().Parse(asString);
        }

        protected T GetProperty<T>(T defaultValue, [CallerMemberName] string propertyName = null)
        {
            XElement property;
            if (!TryGetElement(propertyName, out property))
            {
                return defaultValue;
            }

            object converted;
            TypeConverterCache.Instance.TryConvertFromString(typeof(T), property.Value, out converted);
            return (T)converted;
        }

        protected void SetProperty<T>(T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(value, null) || Equals(value, ""))
            {
                var toRemove = (XElement)GetOrCreateElement(propertyName);
                toRemove.Remove();
                return;
            }

            var property = (XElement)GetOrCreateElement(propertyName);

            string asString;
            TypeConverterCache.Instance.TryConvertToString(typeof(T), value, out asString);

            if (typeof(T) == typeof(bool))
            {
                asString = asString.ToLower();
            }

            property.Value = asString;
        }

        private string FormatPropertyName(string propertyName)
        {
            if (propertyName.Length >= 1 && char.IsUpper(propertyName[0]))
            {
                var result = new StringBuilder(propertyName);
                result[0] = char.ToLower(propertyName[0]);
                return result.ToString();
            }
            return propertyName;
        }

        protected string Attribute(string attributeName)
        {
            return ((XElement)Xml).GetOptionalAttributeValue(attributeName);
        }

        protected void SetAttribute(string attributeName, string value)
        {
            ((XElement)Xml).SetAttributeValue(attributeName, value);
        }

        protected virtual string DefaultNamespace => Xml.Document?.Root?.Name.NamespaceName ?? "";
    }
}