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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Sharpmake
{
    /// <summary>
    /// Resolver
    /// 
    /// Resolver is a search and replace engine of property and member, it's actually may
    /// resolve Object, List<string> and string. To be resolved, an object must have [Resolvable] attribute.
    /// Supported type of parameters is: Object and Map
    /// 
    /// ex:
    ///     class Foo
    ///     {
    ///         public int Value = 1;
    ///     }
    ///      
    ///     void Test()
    ///     {
    ///         Foo foo = new Foo();
    ///         string str = "foo.Value = [foo.Value]";
    ///
    ///         Resolver resolver = new Resolver();
    ///         resolver.SetParameter("foo", foo);
    ///
    ///         Console.WriteLine(resolver.Resolve(str));
    ///         
    ///         // Output: "foo.Value = 1"
    ///     }
    ///     
    /// Object must have [Resolvable] attribute to be resolved, ex:
    /// 
    ///     [Resolvable]
    ///     class C1
    ///     {
    ///         public string Str = "c2.Int = [c2.Int]";
    ///     }
    ///
    ///     class C2
    ///     {
    ///         public int Int = 2;
    ///     }
    ///
    ///     static void Test()
    ///     {
    ///         C1 c1 = new C1();
    ///         C2 c2 = new C2();
    ///
    ///         Resolver resolver = new Resolver();
    ///         resolver.SetParameter("c2", c2);
    ///
    ///         resolver.Resolve(c1);
    ///
    ///         Console.WriteLine(c1.Str);
    ///         // Output: "c2.Int = 2"
    ///     }
    /// 
    /// Example of Dictionary<string, T>
    /// 
    ///     [Resolvable]
    ///     class C1
    ///     {
    ///         public string Str = "map.Value1 = [map.Value1], map.Value2 = [map.Value2]";
    ///     }
    ///
    ///     static void Test()
    ///     {
    ///         C1 c1 = new C1();
    ///
    ///         Dictionary<string, string> map = new Dictionary<string, string>();
    ///         map["Value1"] = "aaa";
    ///         map["Value2"] = "bbb";
    ///
    ///         Resolver resolver = new Resolver();
    ///         resolver.SetParameter("map", map);
    ///
    ///         resolver.Resolve(c1);
    ///
    ///         Console.WriteLine(c1.Str);
    ///         // Output: "map.Value1 = aaa, map.Value2 = bbb"
    ///     }
    ///
    /// </summary>
    public class Resolver
    {
        [System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
        public class Resolvable : Attribute
        {
            public Resolvable()
            { }
        }

        [System.AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public class SkipResolveOnMember : Attribute
        {
            public SkipResolveOnMember()
            { }
        }

        public Resolver()
            : this(true)
        {
        }

        public Resolver(bool isCaseSensitive)
        {
            IsCaseSensitive = isCaseSensitive;
        }

        public bool IsCaseSensitive { get; set; }

        public void SetParameter(string name, object obj)
        {
            name = SetParameterImpl(name, obj, false);
        }

        private string SetParameterImpl(string name, object obj, bool scoped)
        {
            if (!IsCaseSensitive)
                name = name.ToLowerInvariant();

            RefCountedSymbol refCountedObject;
            if (_parameters.TryGetValue(name, out refCountedObject))
            {
                if (scoped)
                    refCountedObject.PushValue(obj);
                else
                    refCountedObject.Value = obj;
            }
            else
            {
                _parameters.Add(name, new RefCountedSymbol(obj));
            }

            return name;
        }

        public void RemoveParameter(string name)
        {
            if (!IsCaseSensitive)
                name = name.ToLowerInvariant();

            RefCountedSymbol refCountedReference = _parameters[name];
            refCountedReference.PopValue();
            if (!refCountedReference.HasValue)
                _parameters.Remove(name);
        }

        public class ScopedParameter : IDisposable
        {
            private readonly Resolver _resolver;
            private readonly string _name;
            public ScopedParameter(Resolver resolver, string name, object value)
            {
                _resolver = resolver;
                _name = name;
                _resolver.SetParameterImpl(_name, value, true);
            }
            public void Dispose()
            {
                _resolver.RemoveParameter(_name);
            }
        }

        public class ScopedParameterGroup : IDisposable
        {
            private readonly Resolver _resolver;
            private readonly string[] _names;

            public ScopedParameterGroup(Resolver resolver, params VariableAssignment[] assignments)
            {
                _resolver = resolver;

                var names = new List<string>();
                foreach (var assignment in assignments)
                {
                    _resolver.SetParameterImpl(assignment.Identifier, assignment.Value, true);
                    names.Add(assignment.Identifier);
                }

                _names = names.ToArray();
            }

            public void Dispose()
            {
                foreach (var name in _names)
                    _resolver.RemoveParameter(name);
            }
        }

        public ScopedParameter NewScopedParameter(string name, object value)
        {
            return new ScopedParameter(this, name, value);
        }

        public void CleanParameters()
        {
            _parameters.Clear();
        }

        public void Resolve(object obj, object fallbackValue = null)
        {
            Reset();
            ResolveObject(null, obj, fallbackValue);
        }

        public virtual string Resolve(string str)
        {
            return Resolve(str, null);
        }

        // Note: The method doesn't use regex as this was slower with regexes(mainly due to MT contention)
        public string Resolve(string str, object fallbackValue = null)
        {
            // Early out
            if (str == null)
                return str;

            StringBuilder builder = null;

            // Support escape char for MemberPath
            // [[MyString]] will convert to [MyString]
            bool containsEscaped = false;

            while (true)
            {
                int startMatch = 0;
                int nbrReplacements = 0;
                int currentSearchIndex = 0;
                int endMatch = 0;
                int strLength = str.Length;
                while (currentSearchIndex < strLength)
                {
                    // Find match range.
                    startMatch = -1;
                    int matchTypeIndex;
                    for (matchTypeIndex = 0; matchTypeIndex < _pathBeginStrings.Length; ++matchTypeIndex)
                    {
                        // Note that specifying StringComparison.Ordinal saves ~30% of the time passed in IndexOf.
                        startMatch = str.IndexOf(_pathBeginStrings[matchTypeIndex], currentSearchIndex, StringComparison.Ordinal);
                        if (startMatch != -1)
                            break;
                    }

                    if (startMatch == -1)
                        break;

                    endMatch = str.IndexOfAny(_pathEndCharacters, startMatch + 1);
                    if (endMatch == -1)
                        break;

                    if (builder == null)
                        builder = new StringBuilder(str.Length + 128);

                    // Append what's before the match
                    if (startMatch - currentSearchIndex > 0)
                        builder.Append(str, currentSearchIndex, startMatch - currentSearchIndex);

                    bool isValidMember = true;
                    int startMatchLength = _pathBeginStrings[matchTypeIndex].Length;
                    int memberStartIndex = startMatch + startMatchLength;
                    for (int i = memberStartIndex; i < endMatch; ++i)
                    {
                        char currentChar = str[i];
                        if (!(currentChar >= 'a' && currentChar <= 'z' ||
                            currentChar >= 'A' && currentChar <= 'Z' ||
                            currentChar >= '0' && currentChar <= '9' ||
                            currentChar == '.' || currentChar == '_'
                            ))
                        {
                            isValidMember = false;
                        }
                    }

                    // A string is escaped if the _PathBeginStrings/_PathEndStrings char is doubled (ie [[ ]])
                    // Also make sure that matchTypeIndex is a char, not a string
                    bool isEscaped = _pathBeginStrings[matchTypeIndex].Length == 1 &&
                                     memberStartIndex > 1 && endMatch < str.Length - 1 &&
                                     str[memberStartIndex - 2] == str[memberStartIndex - 1] &&
                                     str[endMatch] == str[endMatch + 1];

                    containsEscaped |= isEscaped;

                    if (isValidMember && !isEscaped)
                    {
                        bool throwIfNotFound = fallbackValue == null;

                        string resolveResult;
                        try
                        {
                            resolveResult = GetMemberStringValue(str.Substring(memberStartIndex, endMatch - memberStartIndex), throwIfNotFound) ?? fallbackValue?.ToString();
                        }
                        catch (NotFoundException e)
                        {
                            throw new Error(
                                "Error: {0} in '{1}'\n{2}",
                                e.Message,
                                str,
                                e.Arguments
                            );
                        }

                        if (resolveResult == null)
                        {
                            // Resolve failed.
                            builder.Append(str, startMatch, endMatch - startMatch + 1);
                        }
                        else
                        {
                            ++nbrReplacements;
                            builder.Append(resolveResult);
                        }
                        currentSearchIndex = endMatch + 1;
                    }
                    else
                    {
                        builder.Append(str, startMatch, startMatchLength);
                        currentSearchIndex = startMatch + startMatchLength;
                    }
                }

                if (nbrReplacements == 0 && currentSearchIndex == 0)
                    break;

                builder.Append(str, currentSearchIndex, strLength - currentSearchIndex);
                str = builder.ToString();
                builder.Clear();

                if (nbrReplacements == 0)
                    break;
            }

            if (!containsEscaped)
                return str;

            // Now that we have done all replace, convert all escaped char.
            foreach (string beginStr in _pathBeginStrings)
            {
                if (beginStr.Length != 1)
                    continue;
                string escapedStr = beginStr + beginStr;
                str = str.Replace(escapedStr, beginStr);
            }

            foreach (char endChar in _pathEndCharacters)
            {
                string endStr = string.Empty + endChar;
                string escapedStr = endStr + endStr;
                str = str.Replace(escapedStr, endStr);
            }

            return str;
        }
        #region private

        private enum ResolveStatus
        {
            UnResolved,
            Resolving,
            Resolved
        };

        private class RefCountedSymbol
        {
            private readonly Stack<object> _scopedReferences = new Stack<object>();

            public object Value
            {
                get
                {
                    return _scopedReferences.Peek();
                }
                set
                {
                    _scopedReferences.Pop();
                    _scopedReferences.Push(value);
                }
            }
            public bool HasValue => _scopedReferences.Count > 0;

            public RefCountedSymbol(object symbolValue)
            {
                _scopedReferences.Push(symbolValue);
            }

            public void PushValue(object value)
            {
                _scopedReferences.Push(value);
            }

            public void PopValue()
            {
                _scopedReferences.Pop();
            }
        }

        private Dictionary<string, ResolveStatus> _resolveStatusFields = new Dictionary<string, ResolveStatus>();
        private List<string> _resolvingObjectPath = new List<string>();
        private Dictionary<string, RefCountedSymbol> _parameters = new Dictionary<string, RefCountedSymbol>();
        private readonly HashSet<object> _resolvedObject = new HashSet<object>();

        public char[] _pathEndCharacters = { ']' };

        public string[] _pathBeginStrings = { "[" };

        public char _pathSeparator = '.';

        private void Reset()
        {
            _resolveStatusFields.Clear();
            _resolvingObjectPath.Clear();
            _resolvedObject.Clear();
        }

        private void SetResolving(string pathName)
        {
            ResolveStatus status;
            if (_resolveStatusFields.TryGetValue(pathName, out status))
            {
                string stack = "";
                foreach (string path in _resolvingObjectPath)
                    stack += Environment.NewLine + path;
                stack += Environment.NewLine + pathName;
                throw new Exception("Cannot resolve path: " + pathName + " current status is " + status + Environment.NewLine + "Resolving stack: " + stack);
            }
            _resolveStatusFields.Add(pathName, ResolveStatus.Resolving);
            _resolvingObjectPath.Add(pathName);
        }

        private void SetResolved(string pathName)
        {
            ResolveStatus status;
            if (_resolveStatusFields.TryGetValue(pathName, out status))
            {
                if (status != ResolveStatus.Resolving)
                    throw new Exception("Cannot end resolve path: " + pathName + " current status is " + status);

                _resolveStatusFields[pathName] = ResolveStatus.Resolved;

                Debug.Assert(_resolvingObjectPath[_resolvingObjectPath.Count - 1] == pathName);
                _resolvingObjectPath.RemoveAt(_resolvingObjectPath.Count - 1);
            }
            else
            {
                throw new Exception("Cannot end resolve path: " + pathName + " current status is " + status);
            }
        }

        private ResolveStatus GetResolveStatus(string pathName)
        {
            ResolveStatus status;
            return _resolveStatusFields.TryGetValue(pathName, out status) ? status : ResolveStatus.UnResolved;
        }

        private static ConcurrentDictionary<string, KeyValuePair<FieldInfo, PropertyInfo>> s_typeFieldPropertyCache = new ConcurrentDictionary<string, KeyValuePair<FieldInfo, PropertyInfo>>();

        private static void GetFieldInfoOrPropertyInfo(Type type, string name, out FieldInfo fieldInfo, out PropertyInfo propertyInfo)
        {
            // build a unique name
            string uniqueName = type.FullName + name;

            var value = s_typeFieldPropertyCache.GetOrAdd(uniqueName, keyname =>
            {
                FieldInfo field = type.GetField(name);
                PropertyInfo property = (field == null) ? type.GetProperty(name) : null;
                return new KeyValuePair<FieldInfo, PropertyInfo>(field, property);
            });

            fieldInfo = value.Key;
            propertyInfo = value.Value;
        }

        [Serializable]
        private class NotFoundException : Exception
        {
            private IEnumerable<string> _arguments;
            public string Arguments
            {
                get
                {
                    if (_arguments == null)
                        return string.Empty;

                    if (!_arguments.Any())
                        return "The list of arguments that can be used is *Empty*";

                    return "The list of arguments that can be used is:\n- " + string.Join("\n- ", _arguments.OrderBy(x => x, StringComparer.InvariantCultureIgnoreCase));
                }
            }

            public NotFoundException(string message, IEnumerable<string> arguments = null)
                : base(message)
            {
                _arguments = arguments;
            }
        }

        private string GetMemberStringValue(string memberPath, bool throwIfNotFound)
        {
            string[] names = memberPath.Split(new[] { _pathSeparator }, StringSplitOptions.RemoveEmptyEntries);

            if (names.Length == 0)
            {
                if (throwIfNotFound)
                    throw new NotFoundException("Cannot find unnamed parameter");
                return null;
            }

            string parameterName = names[0];
            // get the parameters...
            if (!IsCaseSensitive)
                parameterName = parameterName.ToLowerInvariant();
            RefCountedSymbol refCountedReference;
            if (!_parameters.TryGetValue(parameterName, out refCountedReference))
            {
                if (throwIfNotFound)
                    throw new NotFoundException($"Cannot resolve parameter '{parameterName}'.", _parameters.Keys);

                return null;
            }
            object parameter = refCountedReference.Value;

            string name = "";
            for (int i = 1; i < names.Length && parameter != null; ++i)
            {
                bool found = false;

                string nameChunk = names[i];

                FieldInfo fieldInfo;
                PropertyInfo propertyInfo;
                Type parameterType = parameter.GetType();
                GetFieldInfoOrPropertyInfo(parameterType, nameChunk, out fieldInfo, out propertyInfo);

                if (fieldInfo != null)
                {
                    parameter = fieldInfo.GetValue(parameter);
                    found = true;
                }
                else if (propertyInfo != null)
                {
                    parameter = propertyInfo.GetValue(parameter, null);
                    found = true;
                }

                // IDictionary support
                if (!found && i == names.Length - 1 && parameter is IDictionary)
                {
                    var dictionary = parameter as IDictionary;
                    if (dictionary.Contains(nameChunk))
                        return dictionary[nameChunk].ToString();
                }

                if (!found)
                {
                    if (throwIfNotFound)
                    {
                        string currentPath = parameterName + name + _pathSeparator;

                        // get all public fields
                        var possibleArguments = parameterType.GetFields().Select(f => currentPath + f.Name);

                        // all public properties
                        possibleArguments = possibleArguments.Concat(parameterType.GetProperties().Select(p => currentPath + p.Name));

                        // and dictionary keys, if they are strings
                        var dictionary = parameter as IDictionary;
                        if (dictionary != null)
                        {
                            var keysAsStrings = ((IDictionary)parameter).Keys as IEnumerable<string>;
                            if (keysAsStrings != null)
                                possibleArguments = possibleArguments.Concat(keysAsStrings.Select(k => currentPath + k));
                        }

                        throw new NotFoundException(
                            $"Cannot find path '{nameChunk}' in parameter path '{memberPath}'.",
                            possibleArguments
                        );
                    }
                    return null;
                }

                name += _pathSeparator + nameChunk;
            }

            return parameter == null ? "null" : parameter.ToString();
        }

        private static bool CanWriteFieldValue(FieldInfo fieldInfo)
        {
            return ((fieldInfo.Attributes & FieldAttributes.InitOnly) != FieldAttributes.InitOnly &&
                    (fieldInfo.Attributes & FieldAttributes.Literal) != FieldAttributes.Literal);
        }


        private void ResolveMember(string objectPath, object obj, MemberInfo memberInfo, object fallbackValue)
        {
            if (memberInfo.MemberType != MemberTypes.Field && memberInfo.MemberType != MemberTypes.Property)
                return;

            string memberPath;
            if (objectPath != null)
                memberPath = objectPath + _pathSeparator + memberInfo.Name;
            else
                memberPath = memberInfo.Name;

            if (GetResolveStatus(memberPath) == ResolveStatus.Resolved)
                return;

            if (memberInfo.IsDefined(typeof(SkipResolveOnMember), false))
                return;

            if (memberInfo.MemberType == MemberTypes.Field)
            {
                FieldInfo fieldInfo = memberInfo as FieldInfo;
                object fieldValue = fieldInfo.GetValue(obj);

                if (fieldValue != null)
                {
                    if (fieldInfo.FieldType == typeof(string))
                    {
                        if (CanWriteFieldValue(fieldInfo))
                        {
                            SetResolving(memberPath);
                            string value = fieldValue as string;
                            fieldInfo.SetValue(obj, Resolve(value, fallbackValue));
                            SetResolved(memberPath);
                        }
                    }
                    else if (fieldInfo.FieldType == typeof(Strings))
                    {
                        if (CanWriteFieldValue(fieldInfo))
                        {
                            SetResolving(memberPath);
                            Strings values = fieldValue as Strings;

                            if (values != null)
                            {
                                foreach (string value in values.Values)
                                {
                                    string newValue = Resolve(value, fallbackValue);
                                    values.UpdateValue(value, newValue);
                                }
                            }

                            SetResolved(memberPath);
                        }
                    }
                    else if (fieldValue is IList<string>)
                    {
                        if (CanWriteFieldValue(fieldInfo))
                        {
                            SetResolving(memberPath);
                            IList<string> values = fieldValue as IList<string>;

                            for (int i = 0; i < values.Count; ++i)
                                values[i] = Resolve(values[i], fallbackValue);

                            SetResolved(memberPath);
                        }
                    }
                    else if (fieldInfo.FieldType.IsClass)
                    {
                        object memberObject = fieldValue;
                        ResolveObject(memberPath, memberObject, fallbackValue);
                    }
                }
            }
            else if (memberInfo.MemberType == MemberTypes.Property)
            {
                PropertyInfo propertyInfo = memberInfo as PropertyInfo;

                if (propertyInfo.CanRead)
                {
                    object propertyValue = propertyInfo.GetValue(obj, null);
                    if (propertyValue != null)
                    {
                        if (propertyInfo.PropertyType == typeof(string))
                        {
                            if (propertyInfo.CanWrite && propertyInfo.CanRead)
                            {
                                SetResolving(memberPath);
                                string value = propertyValue as string;
                                propertyInfo.SetValue(obj, Resolve(value, fallbackValue), null);
                                SetResolved(memberPath);
                            }
                        }
                        else if (propertyInfo.PropertyType == typeof(Strings))
                        {
                            SetResolving(memberPath);
                            Strings values = propertyValue as Strings;

                            foreach (string value in values.Values)
                            {
                                string newValue = Resolve(value, fallbackValue);
                                values.UpdateValue(value, newValue);
                            }

                            SetResolved(memberPath);
                        }
                        else if (propertyValue is IList<string>)
                        {
                            SetResolving(memberPath);
                            IList<string> values = propertyValue as IList<string>;

                            for (int i = 0; i < values.Count; ++i)
                                values[i] = Resolve(values[i], fallbackValue);

                            SetResolved(memberPath);
                        }
                        else if (propertyInfo.PropertyType.IsClass)
                        {
                            ResolveObject(memberPath, propertyValue, fallbackValue);
                        }
                    }
                }
            }
        }

        private void ResolveObject(string objectPath, object obj, object fallbackValue)
        {
            // prevent object resolve recursion
            if (_resolvedObject.Add(obj) == false)
                return;

            if (!obj.GetType().IsDefined(typeof(Resolvable), true))
                return;

            if (objectPath != null)
            {
                if (GetResolveStatus(objectPath) == ResolveStatus.Resolved)
                    return;

                SetResolving(objectPath);
            }

            MemberInfo[] memberInfos = obj.GetType().GetMembers();

            foreach (MemberInfo memberInfo in memberInfos)
            {
                ResolveMember(objectPath, obj, memberInfo, fallbackValue);
            }

            if (objectPath != null)
                SetResolved(objectPath);
        }

        #endregion
    }

    public class EnvironmentVariableResolver : Resolver
    {
        public static string OutDir = @"[outputDirectory]";
        public static string RootNamespace = @"[projectName]";

        public EnvironmentVariableResolver(params VariableAssignment[] parameters)
            : base(false)
        {
            _pathBeginStrings = new string[] { "[", "%(", "$(" };
            _pathEndCharacters = new char[] { ']', ')' };

            var fields = GetType().GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            foreach (var fieldInfo in fields)
            {
                if (fieldInfo.FieldType == typeof(string))
                    SetParameter(fieldInfo.Name, fieldInfo.GetValue(this));
            }
            var properties = GetType().GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            foreach (var property in properties)
            {
                if (!property.IsDefined(typeof(Resolvable), true))
                    continue;

                if (property.PropertyType == typeof(string))
                {
                    SetParameter(property.Name, property.GetValue(this));
                }
            }

            foreach (var param in parameters)
                SetParameter(param.Identifier, param.Value);
        }

        public override string Resolve(string str)
        {
            return Resolve(str, null);
        }
    }
}
