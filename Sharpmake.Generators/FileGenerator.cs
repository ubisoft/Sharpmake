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
using System.IO;
using System.Text;

namespace Sharpmake.Generators
{
    public class FileGenerator : IFileGenerator
    {
        // TODO: Remove usage of MemoryStream in APIs and refactor this to use StreamBuilder
        //       instead.
        private MemoryStream _stream;
        private StreamWriter _writer;

        public Resolver Resolver { get; }

        public FileGenerator()
            : this(new Resolver())
        { }

        public FileGenerator(Resolver resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            _stream = new MemoryStream();
            _writer = new StreamWriter(_stream);
            Resolver = resolver;
        }

        public FileGenerator(Encoding encoding)
            : this(encoding, new Resolver())
        { }

        public FileGenerator(Encoding encoding, Resolver resolver)
        {
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            _stream = new MemoryStream();
            _writer = new StreamWriter(_stream, encoding);
            Resolver = resolver;
        }

        public FileGenerator(MemoryStream stream, Resolver resolver)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            _stream = stream;
            _writer = new StreamWriter(stream);
            Resolver = resolver;
        }

        public void Write(string text)
        {
            string resolvedValue = Resolver.Resolve(text);

            // I assume they go to the trouble of doing all that to read a line for a reason. May
            // help to know what it is though.
            StringReader reader = new StringReader(resolvedValue);
            string str = reader.ReadToEnd();
            _writer.Write(str);
        }

        public void Write(string text, string fallbackValue)
        {
            string resolvedValue = Resolver.Resolve(text, fallbackValue);

            // I assume they go to the trouble of doing all that to read a line for a reason. May
            // help to know what it is though.
            StringReader reader = new StringReader(resolvedValue);
            string str = reader.ReadToEnd();
            _writer.Write(str);
        }

        public void WriteLine(string text)
        {
            string resolvedValue = Resolver.Resolve(text);

            // I assume they go to the trouble of doing all that to read a line for a reason. May
            // help to know what it is though.
            StringReader reader = new StringReader(resolvedValue);
            string str = reader.ReadToEnd();
            _writer.WriteLine(str);
        }

        public void WriteLine(string text, string fallbackValue)
        {
            string resolvedValue = Resolver.Resolve(text, fallbackValue);

            // I assume they go to the trouble of doing all that to read a line for a reason. May
            // help to know what it is though.
            StringReader reader = new StringReader(resolvedValue);
            string str = reader.ReadToEnd();
            _writer.WriteLine(str);
        }

        public void WriteVerbatim(string text)
        {
            _writer.Write(text);
        }

        public void WriteLineVerbatim(string text)
        {
            _writer.WriteLine(text);
        }

        public IDisposable Declare(params VariableAssignment[] variables)
        {
            if (variables == null)
                throw new ArgumentNullException(nameof(variables));

            return new Resolver.ScopedParameterGroup(Resolver, variables);
        }

        public void ResolveEnvironmentVariables(Platform platform, params VariableAssignment[] variables)
        {
            if (variables == null)
                throw new ArgumentNullException(nameof(variables));

            var resolver = PlatformRegistry.Query<IPlatformDescriptor>(platform)?.GetPlatformEnvironmentResolver(variables);

            if (resolver != null)
            {
                Flush();

                string content = ToString();
                string cleanContent = resolver.Resolve(content);

                _stream.SetLength(0);

                // Logically the writer should be reusable on a modified stream after flushing it's
                // buffer but the API does not guarantee that buffered writers after modifying a stream.
                // So create a new string.
                _writer = new StreamWriter(_stream);
                _writer.Write(cleanContent);
            }
        }

        public void Flush()
        {
            _writer.Flush();
        }

        public override string ToString()
        {
            Flush();
            return ToString(Encoding.Default);
        }

        public string ToString(Encoding encoding)
        {
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            Flush();
            return encoding.GetString(_stream.ToArray());
        }

        // TODO: Remove this since callers can call Seek and break the writer. Calling code should
        //       just use ToString.
        public MemoryStream ToMemoryStream()
        {
            Flush();
            return _stream;
        }

        public void RemoveTaggedLines()
        {
            Flush();

            try
            {
                _stream.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(_stream))
                {
                    var cleanStream = new MemoryStream();
                    var writer = new StreamWriter(cleanStream);

                    do
                    {
                        string readline = reader.ReadLine();
                        if (readline == null)
                            break;
                        if (!readline.Contains(FileGeneratorUtilities.RemoveLineTag))
                            writer.WriteLine(readline);
                    } while (true);

                    _stream = cleanStream;
                    _writer = writer;
                }
            }
            catch (Exception)
            {
                _stream.Seek(0, SeekOrigin.End);
                _writer = new StreamWriter(_stream);
            }
        }
    }
}
