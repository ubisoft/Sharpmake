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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sharpmake.NuGet.Impl
{
    /// <summary>
    /// Provides a typed array interface around a json object where each key represent an item in the list.
    /// </summary>
    public abstract class JsonDictionaryWrapper<T> : IReadOnlyCollection<T>
    {
        private JsonObject JsonObject { get; }

        internal JsonDictionaryWrapper(JsonObject jsonObject)
        {
            JsonObject = jsonObject;
        }

        protected void Add(string key, object value)
        {
            JsonObject[key] = value;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return JsonObject.Select(Wrap).GetEnumerator();
        }

        protected abstract T Wrap(KeyValuePair<string, object> arg);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => JsonObject.Count;
    }
}