// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sharpmake
{
    [DebuggerDisplay("Gen: {Generated.Count} Skip: {Skipped.Count} Ex: {Exception}")]
    public class GenerationOutput
    {
        public bool HasChanged => Exception != null || Generated.Count > 0;

        public List<string> Generated = new List<string>();
        public List<string> Skipped = new List<string>();

        private object _lock = new object();

        public Exception Exception
        {
            get
            {
                return _exception;
            }
            set
            {
                _exception = value;
                Util.LogWrite(_exception.Message);
            }
        }

        private Exception _exception = null;

        public override string ToString()
        {
            return Exception != null ? Exception.ToString() : string.Format("Generated: {0,2} Skipped: {1,2}", Generated.Count, Skipped.Count);
        }

        public void Merge(GenerationOutput other)
        {
            lock (_lock)
            {
                Generated.AddRange(other.Generated);
                Skipped.AddRange(other.Skipped);
                if (_exception == null)
                    _exception = other._exception;
            }
        }
    }
}
