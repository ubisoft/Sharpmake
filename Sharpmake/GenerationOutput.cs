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
using System.Diagnostics;

namespace Sharpmake
{
    [DebuggerDisplay("Gen: {Generated.Count} Skip: {Skipped.Count} Ex: {Exception}")]
    public class GenerationOutput
    {
        public bool HasChanged => Exception != null || Generated.Count > 0;

        public List<string> Generated = new List<string>();
        public List<string> Skipped = new List<string>();

        public Exception Exception
        {
            get
            {
                return _exception;
            }
            set
            {
                _exception = value;
                Console.WriteLine(_exception.Message);
            }
        }

        private Exception _exception = null;

        public override string ToString()
        {
            return Exception != null ? Exception.ToString() : string.Format("Generated: {0,2} Skipped: {1,2}", Generated.Count, Skipped.Count);
        }

        public void Merge(GenerationOutput other)
        {
            Generated.AddRange(other.Generated);
            Skipped.AddRange(other.Skipped);
            if (_exception == null)
                _exception = other._exception;
        }
    }
}
