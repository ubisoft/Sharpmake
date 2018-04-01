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

using System.IO;

namespace Sharpmake.NuGet.Impl
{
    /// <summary>
    /// Provides a typed array interface around a json object where each key represent an item in the list.
    /// </summary>
    public abstract class JsonObjectWrapper
    {
        private JsonObject _jsonObject;

        internal JsonObjectWrapper(JsonObject jsonObject)
        {
            _jsonObject = jsonObject;
        }

        internal JsonObject GetOrCreate(string property)
        {
            object propertyValue;

            if (!_jsonObject.TryGetValue(property, out propertyValue) ||
                !(propertyValue is JsonObject))
            {
                propertyValue = new JsonObject();
                _jsonObject[property] = propertyValue;
            }

            return (JsonObject)propertyValue;
        }

        /// <summary>
        /// Reads from a file.
        /// </summary>
        public void Read(string path)
        {
            var content = File.ReadAllText(path);
            ReadFromString(content);
        }

        /// <summary>
        /// Writes to the file.
        /// </summary>
        public void Write(string path)
        {
            var content = WriteToString();
            File.WriteAllText(path, content);
        }

        /// <summary>
        /// Reads the json content into the current object.
        /// </summary>
        public void ReadFromString(string json)
        {
            _jsonObject = (JsonObject)SimpleJson.DeserializeObject(json);
        }

        /// <summary>
        /// Reads the json content into the current object.
        /// </summary>
        public string WriteToString()
        {
            return SimpleJson.SerializeObject(_jsonObject);
        }
    }
}