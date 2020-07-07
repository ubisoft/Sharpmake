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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sharpmake
{
    public static partial class Options
    {
        private const string RemoveLineTag = "REMOVE_LINE_TAG";

        [Flags]
        public enum DefaultTarget
        {
            Debug = 0x01,
            Release = 0x02,
            All = Debug | Release
        }

        /// <summary>
        /// Used to hold an option that has a string value
        /// A default value can be set by adding a `public static readonly string Default` field, ex:
        ///     public static readonly string Default = "3.0";
        /// </summary>
        public abstract class StringOption
        {
            public static string Get<T>(Project.Configuration conf)
                where T : StringOption
            {
                var option = Options.GetObject<T>(conf);
                if (option == null)
                {
                    var defaultValue = typeof(T).GetField("Default", BindingFlags.Public | BindingFlags.Static);
                    return defaultValue != null ? (defaultValue.GetValue(null) as string) : RemoveLineTag;
                }
                return option.Value;
            }

            protected StringOption(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        /// <summary>
        /// Used to hold an option that's a path, either to a file or directory, that's gonna be resolved
        /// </summary>
        public abstract class PathOption
        {
            public static string Get<T>(Project.Configuration conf, string fallback = RemoveLineTag, string rootpath = null)
                where T : PathOption
            {
                var option = Options.GetObject<T>(conf);
                if (option == null)
                {
                    return fallback;
                }
                if (!string.IsNullOrEmpty(rootpath))
                {
                    return Util.PathGetRelative(rootpath, option.Path, true);
                }
                return option.Path;
            }

            protected PathOption(string path)
            {
                Path = path;
            }

            public string Path;
        }

        public abstract class IntOption
        {
            public static string Get<T>(Project.Configuration conf)
                where T : IntOption
            {
                var option = GetObject<T>(conf);
                return option != null ? option.Value.ToString() : RemoveLineTag;
            }

            protected IntOption(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }

        /// <summary>
        /// Used to hold an option that has an untyped argument, could be another option
        /// </summary>
        public abstract class WithArgOption<T>
        {
            public static bool Get<U>(Configuration conf, ref T option)
                where U : WithArgOption<T>
            {
                var optionObject = Options.GetObject<U>(conf);
                if (optionObject == null)
                {
                    var defaultValue = typeof(U).GetField("Default", BindingFlags.Static);
                    if (defaultValue != null)
                        option = (T)defaultValue.GetValue(null);
                    return false;
                }

                option = optionObject.Argument;
                return true;
            }

            protected WithArgOption(T argument)
            {
                Argument = argument;
            }

            public T Argument { get; }
        }

        internal class ScopedOption : IDisposable
        {
            private Dictionary<string, string> _options;
            private string _name = null;
            private bool _existed = false;
            private string _previousValue = null;

            public ScopedOption(Dictionary<string, string> options, string name, string value)
            {
                _name = name;
                _options = options;

                if (_options.TryGetValue(_name, out _previousValue))
                    _existed = true;
                _options[_name] = value;
            }

            public void Dispose()
            {
                if (_existed)
                    _options[_name] = _previousValue;
                else
                    _options.Remove(_name);
            }
        }

        public class ExplicitOptions : Dictionary<string, string>
        {
            public Strings ExplicitDefines = new Strings();
        }

        /// <summary>
        /// This method will retrieve a path option from all the configurations, ensuring it has the same value.
        /// If none of the configurations have the option, it will return the fallback value
        /// </summary>
        /// <typeparam name="T">The type of the option to lookup in the configurations.</typeparam>
        /// <param name="configurations">The list of configurations to look into.</param>
        /// <param name="fallback">Optional: Fallback value to return in case none of the configurations have the option.</param>
        /// <param name="rootpath">Optional: The rootpath to convert the path relative to.</param>
        /// <returns></returns>
        public static string GetConfOption<T>(IEnumerable<Project.Configuration> configurations, string fallback = RemoveLineTag, string rootpath = null)
            where T : PathOption
        {
            var values = configurations.Select(conf => PathOption.Get<T>(conf, fallback, rootpath)).Distinct().ToList();
            if (values.Count != 1)
                throw new Error(nameof(T) + " has conflicting values in the configurations, they must all have the same");

            return values.First();
        }

        /// <summary>
        /// This method will retrieve the values associated to a key in an enumerable of dictionaries,
        /// ensuring that the value is identical in all of them, or doesn't exist at all.
        /// If none of the dictionaries contain the key, it will return the fallback value
        /// </summary>
        /// <param name="key">The list of dictionaries to look into.</param>
        /// <param name="dictionaries">The list of dictionaries to look into.</param>
        /// <param name="fallback">Optional: Fallback value to return in case none of the dictionaries have the key.</param>
        /// <returns></returns>
        public static string GetOptionValue(string key, IEnumerable<IReadOnlyDictionary<string, string>> dictionaries, string fallback = RemoveLineTag)
        {
            var values = dictionaries.Select(dict =>
            {
                string value;
                return dict.TryGetValue(key, out value) ? value : fallback;
            }).Distinct().ToList();

            if (values.Count != 1)
                throw new Error($"Found conflicting values for '{key}' in the dictionaries, they must all have the same value");

            return values.First();
        }

        #region Private

        [System.AttributeUsage(AttributeTargets.Field)]
        public class Default : Attribute
        {
            public DefaultTarget DefaultTarget { get; set; }

            public Default()
            {
                DefaultTarget = DefaultTarget.All;
            }

            public Default(DefaultTarget defaultValue)
            {
                DefaultTarget = defaultValue;
            }
        }

        [System.AttributeUsage(AttributeTargets.Field)]
        public class DevEnvVersion : Attribute
        {
            public DevEnv minimum { get; set; }

            public DevEnvVersion()
            {
                minimum = DevEnv.vs2010;
            }
        }

        public class OptionAction
        {
            public object Value;
            public Action Action;
            public OptionAction(object value, Action action)
            {
                Value = value;
                Action = action;
            }
        }

        public static OptionAction Option(object value, Action action)
        {
            return new OptionAction(value, action);
        }

        public static OptionAction Option(object value)
        {
            return new OptionAction(value, () => { });
        }

        private static ConcurrentDictionary<FieldInfo, object[]> s_cachedDefaultAttributes = new ConcurrentDictionary<FieldInfo, object[]>();
        private static ConcurrentDictionary<FieldInfo, object[]> s_cachedDevEnvAttributes = new ConcurrentDictionary<FieldInfo, object[]>();

        public static void SelectOption(Configuration conf, params OptionAction[] options)
        {
            SelectOptionImpl(true, conf, options);
        }
        public static void SelectOptionWithFallback(Configuration conf, Action fallbackAction, params OptionAction[] options)
        {
            if (!SelectOptionImpl(false, conf, options))
                fallbackAction();
        }

        private static bool SelectOptionImpl(bool throwIfNotFound, Configuration conf, OptionAction[] options)
        {
            // Get the type of current options and make sure they are all off the same type
            Type optionType = null;
            foreach (OptionAction optionAction in options)
            {
                if (optionType == null)
                    optionType = optionAction.Value.GetType();
                else
                    if (optionType != optionAction.Value.GetType())
                    throw new Error("SelectOption may only be called of value of the same type");
            }

            if (optionType == null)
                throw new Error();

            FieldInfo[] optionTypeFields = optionType.GetFields();

            // find the latest added option of this type
            for (int i = conf.Options.Count - 1; i >= 0; i--)
            {
                object latestOption = conf.Options[i];
                if (latestOption.GetType() == optionType)
                {
                    foreach (FieldInfo field in optionTypeFields)
                    {
                        if (field.FieldType != optionType)
                            continue;

                        object fieldValue = field.GetValue(optionType);
                        if (fieldValue.Equals(latestOption))
                        {
                            object[] attributes = s_cachedDevEnvAttributes.GetOrAdd(field, fi => fi.GetCustomAttributes(typeof(Options.DevEnvVersion), true));

                            foreach (Options.DevEnvVersion version in attributes)
                            {
                                if (version.minimum > conf.Compiler)
                                    throw new Error(optionType + " " + latestOption + " is not compatible with your DevEnv (" + conf.Compiler + ")");
                            }
                            break;
                        }
                    }

                    foreach (OptionAction optionAction in options)
                    {
                        if (optionAction.Value.Equals(latestOption))
                        {
                            optionAction.Action();
                            return true;
                        }
                    }
                }
            }

            // find the default options
            foreach (FieldInfo field in optionTypeFields)
            {
                object[] attributes = s_cachedDefaultAttributes.GetOrAdd(field, fi => fi.GetCustomAttributes(typeof(Options.Default), true));

                foreach (Options.Default defaultOption in attributes)
                {
                    if (Util.FlagsTest(defaultOption.DefaultTarget, conf.DefaultOption))
                    {
                        object fieldValue = field.GetValue(optionType);
                        foreach (OptionAction optionAction in options)
                        {
                            if (optionAction.Value.Equals(fieldValue))
                            {
                                optionAction.Action();
                                return true;
                            }
                        }
                    }
                }
            }

            if (throwIfNotFound)
                throw new Error("Not default value found for options: " + optionType.Name + " Default Options is " + conf.DefaultOption);

            return false;
        }

        public static T GetObject<T>(Configuration conf)
        {
            return GetObject<T>(conf.Options, conf.DefaultOption);
        }

        public static T GetObject<T>(List<Object> options, DefaultTarget defaultTarget)
        {
            for (int i = options.Count - 1; i >= 0; --i)
            {
                object option = options[i];
                if (option is T)
                {
                    return (T)option;
                }
            }

            // no found, return the default
            Type optionType = typeof(T);

            // for class type , default value is NULL;
            if (optionType.IsClass)
                return default(T);

            // find the default options
            FieldInfo[] optionTypeFields = optionType.GetFields();
            foreach (FieldInfo field in optionTypeFields)
            {
                object[] attributes = s_cachedDefaultAttributes.GetOrAdd(field, fi => fi.GetCustomAttributes(typeof(Options.Default), true));

                foreach (Default defaultOption in attributes)
                {
                    if (Util.FlagsTest(defaultOption.DefaultTarget, defaultTarget))
                    {
                        object fieldValue = field.GetValue(optionType);
                        return (T)fieldValue;
                    }
                }
            }


            throw new Error("Not default value found for options: " + optionType.Name + " Default Options is " + options);
        }

        public static bool HasOption<T>(Configuration conf)
        {
            List<object> options = conf.Options;
            for (int i = options.Count - 1; i >= 0; --i)
            {
                if (options[i] is T)
                    return true;
            }

            return false;
        }

        public static IEnumerable<T> GetObjects<T>(Configuration conf)
        {
            for (int i = conf.Options.Count - 1; i >= 0; --i)
            {
                object option = conf.Options[i];
                if (option != null && option.GetType() == typeof(T))
                {
                    yield return (T)option;
                }
            }
        }

        public static Strings GetStrings<T>(Configuration conf) where T : Strings
        {
            List<object> options = conf.Options;
            Strings values = new Strings();
            for (int i = options.Count - 1; i >= 0; --i)
            {
                Strings option = options[i] as Strings;
                if (option is T)
                {
                    values.AddRange(option);
                }
            }
            return values;
        }

        public static string GetString<T>(Configuration conf) where T : StringOption
        {
            List<object> options = conf.Options;

            for (int i = options.Count - 1; i >= 0; --i)
            {
                string option = options[i] as string;
                if (option is T)
                {
                    return option;
                }
            }
            return string.Empty;
        }

        #endregion
    }
}
