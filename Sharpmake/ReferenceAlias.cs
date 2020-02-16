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
using System.IO;
using System.Linq;
using System.Reflection;

namespace Sharpmake
{
    /// <summary>
    /// Define a mapping between an enum value and a project reference.
    /// Project reference can be either 
    ///     * Type Reference (public or private)
    ///     * Path(s) reference (any list of path with root 'ReferenceAliasManager<T>.BaseFilePath'
    ///     * Nuget reference (Name + Version)
    ///     * CustomAction (let the client decide how to add reference)
    /// </summary>
    /// <typeparam name="T">Enum type</typeparam>
    public class ReferenceAlias<T> where T : struct, IConvertible
    {
        public Type Type { get; private set; }
        public bool PublicReference { get; private set; }
        public Tuple<string, string> Nuget { get; private set; }
        public IReadOnlyList<string> Paths { get; private set; }
        public Action<T, Project.Configuration, ITarget, Project> CustomAction { get; private set; }

        internal static ReferenceAlias<T> FromType(Type type, bool publicRef)
        {
            return new ReferenceAlias<T> { Type = type, PublicReference = publicRef };
        }

        internal static ReferenceAlias<T> FromNuget(string name, string version)
        {
            return new ReferenceAlias<T> { Nuget = Tuple.Create(name, version) };
        }

        internal static ReferenceAlias<T> FromNuget(Tuple<string, string> nugetVersion)
        {
            return new ReferenceAlias<T> { Nuget = nugetVersion };
        }

        internal static ReferenceAlias<T> FromPaths(IReadOnlyList<string> paths)
        {
            return new ReferenceAlias<T> { Paths = paths };
        }

        internal static ReferenceAlias<T> FromCustomAction(Action<T, Project.Configuration, ITarget, Project> customAction)
        {
            return new ReferenceAlias<T> { CustomAction = customAction };
        }

        internal static ReferenceAlias<T> FromAttribute(ReferenceAliasAttribute attr)
        {
            if (attr.Type != null)
            {
                return FromType(attr.Type, attr.PublicReference);
            }

            if (!string.IsNullOrEmpty(attr.Name) && !string.IsNullOrEmpty(attr.Version))
            {
                return FromNuget(attr.Name, attr.Version);
            }

            return FromPaths(attr.Paths);
        }

        public override string ToString()
        {
            if (Type != null)
                return string.Format("Type:{0}", Type.FullName);

            if (Nuget != null)
                return string.Format("Nuget:{0}[{1}]", Nuget.Item1, Nuget.Item2);

            if (Paths.Any())
                return string.Format("Path:{0}", string.Join(",", Paths));

            return "<Invalid>";
        }
    }

    /// <summary>
    /// Enum value on attribute used to define reference alias
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Field)]
    public sealed class ReferenceAliasAttribute : Attribute
    {
        public Type Type { get; set; }
        public bool PublicReference { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string[] Paths { get; set; }

        public ReferenceAliasAttribute(Type type, bool publicRef = true)
        {
            Type = type;
            PublicReference = publicRef;
        }

        public ReferenceAliasAttribute(params string[] paths)
        {
            Paths = paths;
        }

        public static ReferenceAlias<T> Get<T>(object enumValue) where T : struct, IConvertible
        {
            if (enumValue == null)
                return null;

            MemberInfo[] mi = typeof(T).GetMember(enumValue.ToString());
            if (mi.Length <= 0)
                return null;

            ReferenceAliasAttribute attr = Attribute.GetCustomAttribute(mi[0], typeof(ReferenceAliasAttribute)) as ReferenceAliasAttribute;

            if (attr == null)
                return null;

            return ReferenceAlias<T>.FromAttribute(attr);
        }
    }

    /// <summary>
    /// Enum type attribute used to automatically initialize ReferenceAliasManager<T>
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Enum)]
    public sealed class ReferenceAliasInitAttribute : Attribute
    {
        private readonly Type _initContainerType;

        public ReferenceAliasInitAttribute(Type containerType)
        {
            _initContainerType = containerType;
        }

        public void Invoke(Type enumType, object parameter)
        {
            MethodInfo method = _initContainerType.GetMethods()
                .FirstOrDefault(m => m.GetCustomAttributes().Any(a => a is ReferenceAliasInitMethodAttribute &&
                                                                ((ReferenceAliasInitMethodAttribute)a).EnumType == enumType));

            if (method == null)
                throw new InvalidOperationException(string.Format("Unable to find ReferenceAliasInitMethod on type {0} for enum {1}", _initContainerType.FullName, enumType.FullName));

            method.Invoke(this, new[] { parameter });
        }
    }

    /// <summary>
    /// Method attribute used to with ReferenceAliasInit attribute to initialize ReferenceAliasManager<T>
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Method)]
    public sealed class ReferenceAliasInitMethodAttribute : Attribute
    {
        public Type EnumType { get; private set; }

        public ReferenceAliasInitMethodAttribute(Type enumType)
        {
            EnumType = enumType;
        }
    }

    /// <summary>
    /// Manager keeping a dictionary<T, ReferenceAlias> with all defined mapping from an enum to a reference alias
    /// It automatically initialized itself by getting attribute values on the given enum
    /// </summary>
    /// <typeparam name="T">Enum type</typeparam>
    public class ReferenceAliasManager<T> where T : struct, IConvertible
    {
        public static ReferenceAliasManager<T> Instance { get; protected set; }

        public static void CreateInstance()
        {
            Trace.Assert(Instance == null);
            Instance = new ReferenceAliasManager<T>();
        }

        private readonly Dictionary<T, ReferenceAlias<T>> _referenceAliases = new Dictionary<T, ReferenceAlias<T>>();

        public string BaseFilePath { get; set; }
        public Action<T, Project.Configuration, ITarget, Project> FallbackAction { get; set; }

        public ReferenceAliasManager()
        {
            if (!typeof(T).IsEnum)
                throw new InvalidOperationException("T must be an enumerated type");

            // Load all default ReferenceAlias
            foreach (T alias in Enum.GetValues(typeof(T)))
            {
                ReferenceAlias<T> referenceAlias = ReferenceAliasAttribute.Get<T>(alias);
                if (referenceAlias == null)
                    continue;

                _referenceAliases.Add(alias, referenceAlias);
            }

            // Execute enum custom initialization if defined
            ReferenceAliasInitAttribute attr = typeof(T).GetCustomAttributes(typeof(ReferenceAliasInitAttribute), true).FirstOrDefault() as ReferenceAliasInitAttribute;

            if (attr != null)
            {
                attr.Invoke(typeof(T), this);
            }
        }

        public void Set(T aliasValue, Type type, bool publicReference = true)
        {
            _referenceAliases.Add(aliasValue, ReferenceAlias<T>.FromType(type, publicReference));
        }

        public void Set(T aliasValue, Tuple<string, string> nuget)
        {
            _referenceAliases.Add(aliasValue, ReferenceAlias<T>.FromNuget(nuget));
        }

        public void Set(T aliasValue, params string[] paths)
        {
            _referenceAliases.Add(aliasValue, ReferenceAlias<T>.FromPaths(paths));
        }

        public void Set(T aliasValue, Action<T, Project.Configuration, ITarget, Project> customAction)
        {
            _referenceAliases.Add(aliasValue, ReferenceAlias<T>.FromCustomAction(customAction));
        }

        /// <summary>
        /// Main AddReference method called by extensions. 
        /// This one is using aliasValue and call the proper add reference for sharpmake projet
        /// </summary>
        /// <param name="aliasValue"></param>
        /// <param name="conf"></param>
        /// <param name="target"></param>
        /// <param name="project"></param>
        public virtual void AddReference(T aliasValue, Project.Configuration conf, ITarget target, Project project)
        {
            if (!_referenceAliases.ContainsKey(aliasValue))
            {
                // If aliasValue not defined and we have a FallbackAction, call it !
                if (FallbackAction != null)
                {
                    FallbackAction(aliasValue, conf, target, project);
                    return;
                }

                throw new InvalidOperationException(string.Format("{0} reference is not defined in enum {1}", aliasValue, typeof(T).FullName));
            }

            ReferenceAlias<T> reference = _referenceAliases[aliasValue];

            if (reference.Type != null)
            {
                if (target == null)
                    throw new ArgumentNullException(nameof(target), string.Format("You need to specify target when adding {0} reference", aliasValue));

                if (reference.PublicReference)
                {
                    conf.AddPublicDependency(target, reference.Type);
                }
                else
                {
                    conf.AddPrivateDependency(target, reference.Type);
                }
            }
            else if (reference.Nuget != null && !reference.Nuget.Equals(default(Tuple<string, string>)) && !string.IsNullOrEmpty(reference.Nuget.Item1) && !string.IsNullOrEmpty(reference.Nuget.Item2))
                conf.ReferencesByNuGetPackage.Add(reference.Nuget.Item1, reference.Nuget.Item2);
            else if (reference.Paths != null && reference.Paths.Any())
            {
                foreach (string path in reference.Paths.Where(p => !string.IsNullOrEmpty(p)))
                {
                    string filePath = Path.IsPathRooted(path) ? path : $"{BaseFilePath}\\{path}";
                    conf.ReferencesByPath.Add(filePath);
                }
            }
            else if (reference.CustomAction != null)
                reference.CustomAction(aliasValue, conf, target, project);
            else
                throw new InvalidOperationException(string.Format("{0} reference found but not properly defined in enum {1}", aliasValue, typeof(T).FullName));
        }
    }

    /// <summary>
    /// Helper ReferenceAliasManager that allow to forward AddReference from one enum type to another, based on enum value name matching
    /// This is used to match a publicly exposed enum to a private enum definition for example.
    /// </summary>
    /// <typeparam name="T">Source enum type</typeparam>
    /// <typeparam name="TDest">Destination enum type</typeparam>
    public class ReferenceAliasForwardManager<T, TDest> : ReferenceAliasManager<T> where T : struct, IConvertible
                                                                                   where TDest : struct, IConvertible
    {
        public new static void CreateInstance()
        {
            Instance = new ReferenceAliasForwardManager<T, TDest>();
        }

        public override void AddReference(T aliasValue, Project.Configuration conf, ITarget target, Project project)
        {
            if (ReferenceAliasManager<TDest>.Instance == null)
                throw new InvalidOperationException(string.Format("{0} does not have any instance created", typeof(ReferenceAliasManager<TDest>)));

            TDest forwardAliasValue = (TDest)Enum.Parse(typeof(TDest), aliasValue.ToString(), true);

            ReferenceAliasManager<TDest>.Instance.AddReference(forwardAliasValue, conf, target, project);
        }
    }

    /// <summary>
    /// Those extensions will let you use AddReference on conf object in your Configure
    /// ex : 
    ///     conf.AddReferences(MyEnum.Lib1, MyEnum.Lib2);
    ///     conf.AddReference(MyEnum.Lib3, target, project);
    /// </summary>
    public static class ReferenceAliasExtensions
    {
        public static void AddReference<T>(this Project.Configuration conf, T name, ITarget target = null, Project project = null) where T : struct, IConvertible
        {
            if (ReferenceAliasManager<T>.Instance == null)
                throw new InvalidOperationException(string.Format("{0} does not have any instance created", typeof(ReferenceAliasManager<T>)));

            ReferenceAliasManager<T>.Instance.AddReference(name, conf, target, project);
        }

        public static void AddReferences<T>(this Project.Configuration conf, params T[] names) where T : struct, IConvertible
        {
            foreach (T name in names)
            {
                AddReference(conf, name);
            }
        }
    }
}
