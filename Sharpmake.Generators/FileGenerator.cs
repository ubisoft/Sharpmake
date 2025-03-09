// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Sharpmake.Generators
{
    public class FileGenerator : IFileGenerator
    {
        private MemoryStream _stream;
        private StreamWriter _writer;
        internal Encoding Encoding => _writer.Encoding;
        public Resolver Resolver { get; }

        public FileGenerator()
            : this(new Resolver())
        { }

        public FileGenerator(Resolver resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            _stream = new MemoryStream();
            _writer = new StreamWriter(_stream, new UTF8Encoding(false));
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

        public void WriteTo(IFileGenerator generator)
        {
            var fileGenerator = generator as FileGenerator;
            Debug.Assert(fileGenerator != null);

            Flush();
            fileGenerator.Flush();
            _stream.WriteTo(fileGenerator._stream);
        }

        public void Write(string text)
        {
            string resolvedValue = Resolver.Resolve(text);
            _writer.Write(resolvedValue);
        }

        public void Write(string text, string fallbackValue)
        {
            string resolvedValue = Resolver.Resolve(text, fallbackValue);
            _writer.Write(resolvedValue);
        }

        public void WriteLine(string text)
        {
            string resolvedValue = Resolver.Resolve(text);
            _writer.WriteLine(resolvedValue);
        }

        public void WriteLine(string text, string fallbackValue)
        {
            string resolvedValue = Resolver.Resolve(text, fallbackValue);
            _writer.WriteLine(resolvedValue);
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

        public void Clear()
        {
            Flush(); // Needed otherwise remaining data would be written after reset
            _stream.SetLength(0);
        }

        public void ResolveEnvironmentVariables(Platform platform, params VariableAssignment[] variables)
        {
            if (variables == null)
                throw new ArgumentNullException(nameof(variables));

            var resolver = PlatformRegistry.Query<IPlatformDescriptor>(platform)?.GetPlatformEnvironmentResolver(variables);

            if (resolver != null)
            {
                Flush();

                string content = GetBufferAsString();
                string newContent = resolver.Resolve(content, null, out var wasChanged);
                if (!wasChanged)
                    return;

                _stream.SetLength(0);
                _writer.Write(newContent);
            }
        }

        public bool IsEmpty()
        {
            _writer.Flush();
            return _stream.Length == 0;
        }

        public void Flush()
        {
            _writer.Flush();
        }

        private string GetBufferAsString()
        {
            Flush();

            return _writer.Encoding.GetString(_stream.GetBuffer(), 0, (int)_stream.Length);
        }

        [Obsolete("Can't use this anymore. Please use IsFileDifferent or FileWriteIfDifferent")]
        public MemoryStream ToMemoryStream()
        {
            Flush();
            return _stream;
        }
        public bool IsFileDifferent(FileInfo file)
        {
            Flush();
            return Util.IsFileDifferent(file, _stream);
        }

        public bool FileWriteIfDifferent(FileInfo file, bool bypassAutoCleanupDatabase = false)
        {
            Flush();
            return Util.FileWriteIfDifferentInternal(file, _stream, bypassAutoCleanupDatabase);
        }

        public void RemoveTaggedLines()
        {
            Flush();

            // Read and process the stream using spans to avoid any extra buffer copy.
            var removeLineBytes = _writer.Encoding.GetBytes(FileGeneratorUtilities.RemoveLineTag.ToCharArray()).AsSpan();
            var wholeStreamSpan = _stream.GetBuffer().AsSpan().Slice(0, (int)_stream.Length);
            if (wholeStreamSpan.IndexOf(removeLineBytes) == -1)
                return; // Early exit when there is no remove line tag in the file.

            var newLineBytes = _writer.Encoding.GetBytes(Environment.NewLine.ToCharArray()).AsSpan();
            var newStream = new MemoryStream((int)_stream.Length);
            int nextSlice = 0;
            while (true)
            {
                // Looking for end of line
                var restOfFileSlice = wholeStreamSpan.Slice(nextSlice);
                int endLineIndex = -1;
                int endLineMarkerSize = 0;
                for (int i = 0; i < restOfFileSlice.Length; i++)
                {
                    byte ch = restOfFileSlice[i];
                    if (ch == '\r' || ch == '\n')
                    {
                        endLineIndex = nextSlice + i;
                        endLineMarkerSize = 1;

                        if (ch == '\r' && i + 1 < restOfFileSlice.Length && restOfFileSlice[i + 1] == '\n')
                        {
                            endLineMarkerSize = 2;
                        }
                        break;
                    }
                }

                if (endLineIndex != -1)
                {
                    // Check if the line contains the remove line tag. Skip the line if found
                    var lineSlice = wholeStreamSpan.Slice(nextSlice, endLineIndex - nextSlice);
                    if (lineSlice.IndexOf(removeLineBytes) == -1)
                    {
                        newStream.Write(lineSlice);
                        newStream.Write(newLineBytes);
                    }

                    // Advance to next line
                    nextSlice += (lineSlice.Length + endLineMarkerSize);
                    if (nextSlice >= wholeStreamSpan.Length)
                    {
                        Debug.Assert(nextSlice == wholeStreamSpan.Length);
                        break;
                    }
                }
                else
                {
                    // Rest of file.
                    var lineSlice = wholeStreamSpan.Slice(nextSlice);

                    // Check if the line contains the remove line tag. Skip the line if found
                    if (lineSlice.IndexOf(removeLineBytes) == -1)
                    {
                        newStream.Write(lineSlice);
                        // Note: Adding a new line to generate the exact same thing than with original implementation
                        newStream.Write(newLineBytes);
                    }

                    // End of file
                    break;
                }
            }

            Debug.Assert(newStream.Length > 0);
            var newWriter = new StreamWriter(newStream, _writer.Encoding);
            _writer.Dispose(); // This will dispose _stream as well.

            _stream = newStream;
            _writer = newWriter;
        }
    }
}
