// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Sharpmake
{
    /// <summary>
    /// The development environments supported by Sharpmake generators.
    /// </summary>
    /// <remarks>
    /// This fragment is mandatory in every target.
    /// </remarks>
    [Fragment, Flags]
    public enum DevEnv
    {
        /// <summary>
        /// Visual Studio 2015
        /// </summary>
        vs2015 = 1 << 3,

        /// <summary>
        /// Visual Studio 2017
        /// </summary>
        vs2017 = 1 << 4,

        /// <summary>
        /// Visual Studio 2019
        /// </summary>
        vs2019 = 1 << 5,

        /// <summary>
        /// Visual Studio 2022
        /// </summary>
        vs2022 = 1 << 6,

        /// <summary>
        /// Xcode projects
        /// </summary>
        xcode = 1 << 7,

        /// <summary>
        /// iOS project with Xcode [deprecated]
        /// </summary>
        [Obsolete("xcode4ios has been deprecated, please use 'xcode'", error: true)]
        xcode4ios = 1 << 7,

        /// <summary>
        /// Eclipse.
        /// </summary>
        eclipse = 1 << 8,

        /// <summary>
        /// GNU Makefiles.
        /// </summary>
        make = 1 << 9,

        /// <summary>
        /// All supported Visual Studio versions.
        /// </summary>
        [CompositeFragment]
        VisualStudio = vs2015 | vs2017 | vs2019 | vs2022,

        [Obsolete("Sharpmake doesn't support vs2010 anymore.")]
        vs2010 = -1,
        [Obsolete("Sharpmake doesn't support vs2012 anymore.")]
        vs2012 = -1,
        [Obsolete("Sharpmake doesn't support vs2013 anymore.")]
        vs2013 = -1,
    }

    /// <summary>
    /// The platforms supported by Sharpmake generators.
    /// Always use 'Util.GetSimplePlatformString' to get the correct name of these platforms.
    /// </summary>
    /// <remarks>
    /// This fragment is mandatory in every target.
    /// </remarks>
    [Fragment, Flags]
    public enum Platform
    {
        /// <summary>
        /// Windows 32-bit
        /// </summary>
        win32 = 1 << 0,

        /// <summary>
        /// Windows 64-bit
        /// </summary>
        win64 = 1 << 1,

        /// <summary>
        /// .NET CLR
        /// </summary>
        anycpu = 1 << 2,

        /// <summary>
        /// Xbox One
        /// </summary>
        durango = 1 << 3,

        /// <summary>
        /// Playstation 4
        /// </summary>
        orbis = 1 << 4,

        /// <summary>
        /// Nintendo Switch
        /// </summary>
        nx = 1 << 5,

        _inactive1 = 1 << 6, // This used to be "ctr"

        /// <summary>
        /// Apple iPhone and iPad
        /// </summary>
        ios = 1 << 7,

        /// <summary>
        /// Android
        /// </summary>
        android = 1 << 8,

        /// <summary>
        /// Linux
        /// </summary>
        linux = 1 << 9,

        /// <summary>
        /// macOS
        /// </summary>
        mac = 1 << 10,

        /// <summary>
        /// Android Game Development Extension
        /// </summary>
        agde = 1 << 11,

        /// <summary>
        /// Apple TV
        /// </summary>
        tvos = 1 << 12,

        /// <summary>
        /// Apple Watch
        /// </summary>
        watchos = 1 << 13,

        /// <summary>
        /// Mac Catalyst (see https://developer.apple.com/mac-catalyst/)
        /// </summary>
        maccatalyst = 1 << 14,

        // This is a reverse-growing section for undisclosed platforms
        _reserved10 = 1 << 21, // ACTIVE
        _reserved9  = 1 << 22, // ACTIVE
        _reserved8  = 1 << 23, // ACTIVE
        _reserved7  = 1 << 24, // ACTIVE
        _reserved6  = 1 << 25, // Inactive
        _reserved5  = 1 << 26, // Inactive
        _reserved4  = 1 << 27, // Inactive
        _reserved3  = 1 << 28, // Inactive
        _reserved2  = 1 << 29, // Inactive
        _reserved1  = 1 << 30, // Inactive

        [Obsolete]
        x360 = -1,
        [Obsolete]
        ps3 = -1,
        [Obsolete]
        ps3spu = -1,
        [Obsolete]
        wii = -1,
        [Obsolete]
        wiiu = -1,
        [Obsolete]
        nvshield = -1,
        [Obsolete]
        ctr = -1,
    }

    [Fragment, Flags]
    public enum BuildSystem
    {
        MSBuild = 0x01,
        FastBuild = 0x02,
    }

    [Fragment, Flags]
    public enum Optimization
    {
        Debug = 0x01,
        Release = 0x02,
        Retail = 0x04
    }

    [Fragment, Flags]
    public enum OutputType
    {
        Lib = 0x01,
        Dll = 0x02,
    }

    [Fragment, Flags]
    public enum DotNetFramework
    {
        net10_0 = 1 << 0,
        // UNUSED = 1 << 1,
        // UNUSED = 1 << 2,
        // UNUSED = 1 << 3,
        // UNUSED = 1 << 4,
        // UNUSED = 1 << 5,
        v4_7 = 1 << 6,
        v4_7_1 = 1 << 7,
        v4_7_2 = 1 << 8,
        v4_8 = 1 << 9,

        netcore1_0 = 1 << 10,
        netcore1_1 = 1 << 11,
        netcore2_0 = 1 << 12,
        netcore2_1 = 1 << 13,
        netcore2_2 = 1 << 14,
        netcore3_0 = 1 << 15,
        netcore3_1 = 1 << 16,

        net5_0 = 1 << 17,
        net6_0 = 1 << 18,
        net7_0 = 1 << 19,
        net8_0 = 1 << 20,

        netstandard1_0 = 1 << 21,
        netstandard1_1 = 1 << 22,
        netstandard1_2 = 1 << 23,
        netstandard1_3 = 1 << 24,
        netstandard1_4 = 1 << 25,
        netstandard1_5 = 1 << 26,
        netstandard1_6 = 1 << 27,
        netstandard2_0 = 1 << 28,
        netstandard2_1 = 1 << 29,

        net9_0 = 1 << 30,

        [CompositeFragment]
        all_netframework = v4_7 | v4_7_1 | v4_7_2 | v4_8,
        [CompositeFragment]
        all_netcore = netcore1_0 | netcore1_1 | netcore2_0 | netcore2_1 | netcore3_0 | netcore3_1 | net5_0 | net6_0 | net7_0 | net8_0 | net9_0 | net10_0,
        [CompositeFragment]
        all_netstandard = netstandard1_0 | netstandard1_1 | netstandard1_2 | netstandard1_3 | netstandard1_4 | netstandard1_5 | netstandard1_6 | netstandard2_0 | netstandard2_1,

        [Obsolete("Please use at least .net framework 4.7", error: true)]
        v2,
        [Obsolete("Please use at least .net framework 4.7", error: true)]
        v3,
        [Obsolete("Please use at least .net framework 4.7", error: true)]
        v4_0,
        [Obsolete("Please use at least .net framework 4.7", error: true)]
        v4_5,
        [Obsolete("Please use at least .net framework 4.7", error: true)]
        v4_5clientprofile,
        [Obsolete("Please use at least .net framework 4.7", error: true)]
        v4_5_1,
        [Obsolete("Please use at least .net framework 4.7", error: true)]
        v3_5,
        [Obsolete("Please use at least .net framework 4.7", error: true)]
        v3_5clientprofile,
        [Obsolete("Please use at least .net framework 4.7", error: true)]
        v4_5_2,
        [Obsolete("Please use at least .net framework 4.7", error: true)]
        v4_6,
        [Obsolete("Please use at least .net framework 4.7", error: true)]
        v4_6_1,
        [Obsolete("Please use at least .net framework 4.7", error: true)]
        v4_6_2,

    }

    // https://docs.microsoft.com/en-us/dotnet/standard/frameworks#net-5-os-specific-tfms
    [Fragment, Flags]
    public enum DotNetOS
    {
        Default = 1 << 0,
        android = 1 << 1,
        ios = 1 << 2,
        maccatalyst = 1 << 3,
        macos = 1 << 4,
        tvos = 1 << 5,
        watchos = 1 << 6,
        windows = 1 << 7
    }

    // Optional
    [Fragment, Flags]
    public enum Blob
    {
        // Blob only project, another project reference the source files
        Blob = 0x01,

        // Normal Visual Studio project without blobbing.
        // Can be combined with Blob inside same solution.
        NoBlob = 0x02,

        FastBuildUnitys = 0x04,
    }

    public enum KitsRootEnum
    {
        KitsRoot,
        KitsRoot81,
        KitsRoot10
    }

    // Default Target, user may define its own if needed
    public class Target : ITarget
    {
        public Optimization Optimization;
        public Platform Platform;
        public string ToolchainPlatform { get { return Util.GetToolchainPlatformString(Platform, this); } }
        public BuildSystem BuildSystem;
        public DevEnv DevEnv;
        public OutputType OutputType;
        public DotNetFramework Framework;
        public string FrameworkFolder { get { return Framework.ToFolderName(); } }
        public Blob Blob;

        public override string Name
        {
            get { return Optimization.ToString(); }
        }

        public Target() { }

        public Target(
            Platform platform,
            DevEnv devEnv,
            Optimization optimization,
            OutputType outputType = OutputType.Lib,
            Blob blob = Blob.NoBlob,
            BuildSystem buildSystem = BuildSystem.MSBuild,
            DotNetFramework framework = DotNetFramework.v4_7_2
        )
        {
            Platform = platform;
            DevEnv = devEnv;
            Optimization = optimization;
            OutputType = outputType;
            Framework = framework;
            BuildSystem = buildSystem;
            Blob = blob;
        }
    }

    public abstract class ITarget : IComparable<ITarget>
    {
        public override string ToString()
        {
            return GetTargetString();
        }

        private static ConcurrentDictionary<object, string> s_cachedFieldValueToString = new ConcurrentDictionary<object, string>();

        public string GetTargetString()
        {
            if (_valueCache != null)
                return _valueCache;

            FieldInfo[] fieldInfos = GetFragmentFieldInfo();

            var fieldInfoValues = fieldInfos.Select(f => f.GetValue(this));
            var nonZeroValues = fieldInfoValues.Where(f => ((int)f) != 0);
            string result = string.Join(
                "_",
                nonZeroValues.Select(f => s_cachedFieldValueToString.GetOrAdd(f, value =>
                {
                    if (value is Platform platformValue)
                    {
                        return Util.GetSimplePlatformString(platformValue);
                    }
                    return value.ToString();
                }))
            );
            _valueCache = result;
            return result;
        }

        public virtual string Name
        {
            get { return GetTargetString(); }
        }

        public virtual string ProjectConfigurationName
        {
            get { return Name; }
        }

        public ITarget Clone(params object[] overrideValues)
        {
            Type sourceType = GetType();
            ITarget destination = Activator.CreateInstance(sourceType) as ITarget;

            FieldInfo[] fragmentFields = GetFragmentFieldInfo();

            foreach (FieldInfo fragmentField in fragmentFields)
            {
                int sourceFragmentValue = (int)fragmentField.GetValue(this);
                fragmentField.SetValue(destination, sourceFragmentValue);
            }

            if (overrideValues.Length > 0)
            {
                destination.SetFragments(overrideValues);
            }

            return destination;
        }

        public static void ValidFragmentType(Type fragmentType)
        {
            if (!fragmentType.IsEnum)
                throw new Error("fragment must be an Enum: {0}", fragmentType.FullName);

            if (!fragmentType.IsDefined(typeof(Fragment), false))
                throw new Error("fragment must have [Sharpmake.Fragment] attribute: {0}", fragmentType.FullName);

            if (!fragmentType.IsDefined(typeof(FlagsAttribute), false))
                throw new Error("fragment must have [Flags] attribute: {0}", fragmentType.FullName);
        }

        public int CompareTo(ITarget other)
        {
            var thisType = GetType();
            var otherType = other.GetType();
            if (thisType != otherType)
            {
                int cmp = string.Compare(thisType.FullName, otherType.FullName, StringComparison.Ordinal);
                if (cmp == 0)
                    throw new Exception("Two different types cannot have same name: " + thisType.FullName);
                return cmp;
            }

            if (_valueCache == null)
                _valueCache = GetTargetString();

            if (other._valueCache == null)
                other._valueCache = other.GetTargetString();

            return string.Compare(_valueCache, other._valueCache, StringComparison.Ordinal);
        }


        public bool IsEqualTo(ITarget other)
        {
            if (GetType() != other.GetType())
                return false;

            if (_valueCache == null)
                _valueCache = GetTargetString();

            if (other._valueCache == null)
                other._valueCache = other.GetTargetString();

            if (_valueCache == other._valueCache)
                return true;
            return string.Compare(_valueCache, other._valueCache, StringComparison.Ordinal) == 0;
        }

        //possible to override this to make the associations with custom platforms and Sharpmake's
        public virtual Platform GetPlatform()
        {
            return GetFragment<Platform>();
        }

        //possible to override this to make the associations with custom platforms and Sharpmake's
        public virtual Optimization GetOptimization()
        {
            return GetFragment<Optimization>();
        }

        public T GetFragment<T>()
        {
            T value;
            if (TryGetFragment(out value))
                return value;
            throw new Exception("cannot find fragment value of type " + typeof(T).FullName + " in object " + GetType().FullName);
        }

        public bool TryGetFragment<T>(out T value)
        {
            FieldInfo[] fragments = GetType().GetFields();
            var tType = typeof(T);
            foreach (FieldInfo fragment in fragments)
            {
                if (tType.IsAssignableFrom(fragment.FieldType))
                {
                    value = (T)fragment.GetValue(this);
                    return true;
                }
            }
            value = default(T);
            return false;
        }

        public void SetFragment<T>(T value)
        {
            FieldInfo[] fragments = GetType().GetFields();
            var valueType = value.GetType();
            foreach (FieldInfo fragment in fragments)
            {
                if (fragment.FieldType.IsAssignableFrom(valueType))
                {
                    fragment.SetValue(this, value);
                    return;
                }
            }
            throw new Exception("cannot find fragment value of type " + valueType.FullName + " in object " + GetType().FullName);
        }

        public bool TestFragment<T>(T value)
        {
            FieldInfo[] fragments = GetType().GetFields();
            var valueType = value.GetType();
            foreach (FieldInfo fragment in fragments)
            {
                if (valueType.IsAssignableFrom(fragment.FieldType))
                    return Util.FlagsTest<T>((T)fragment.GetValue(this), value);
            }
            return false;
        }

        public bool HaveFragment<T>()
        {
            FieldInfo[] fragments = GetType().GetFields();
            var tType = typeof(T);
            return fragments.Any(fragment => fragment.FieldType == tType);
        }

        public bool HaveFragmentOfSameType(object asFragment)
        {
            FieldInfo[] fragments = GetType().GetFields();
            var tType = asFragment.GetType();
            return fragments.Any(fragment => fragment.FieldType == tType);
        }

        public void SetFragments(params object[] values)
        {
            FieldInfo[] fragments = GetType().GetFields();
            foreach (FieldInfo fragment in fragments)
            {
                var overrideValues = values.Where(v => v.GetType() == fragment.FieldType);
                if (overrideValues.Any())
                {
                    int value = (int)(overrideValues.Aggregate((acc, cur) => (int)acc | (int)cur));
                    fragment.SetValue(this, value);
                }
            }

            var fragmentTypes = fragments.Select(f => f.FieldType);
            var invalidTypes = values.Select(v => v.GetType()).Where(t => !fragmentTypes.Contains(t));
            if (invalidTypes.Any())
            {
                var invalidNames = invalidTypes.Select(t => t.FullName).Aggregate((current, next) => current + ", " + next);
                throw new Exception("cannot find fragment value of type " + invalidNames + " in object " + GetType().FullName);
            }
        }

        public bool AndMask(object fragmentMask)
        {
            FieldInfo[] fragmentFields = GetType().GetFields();

            var fragmentMaskType = fragmentMask.GetType();
            foreach (FieldInfo fragmentField in fragmentFields)
            {
                if (fragmentMaskType.IsAssignableFrom(fragmentField.FieldType))
                {
                    int targetValue = (int)fragmentField.GetValue(this);
                    int maskValue = (int)fragmentMask;
                    if ((targetValue == 0) || (targetValue & maskValue) != targetValue)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool IsIncludeIn(ITarget other)
        {
            if (GetType() != other.GetType())
                return false;

            int[] target1Values = GetFragmentsValue();
            int[] target2Values = other.GetFragmentsValue();

            Debug.Assert(target1Values.Length == target2Values.Length);

            for (int i = 0; i < target1Values.Length; ++i)
            {
                if ((target1Values[i] & target2Values[i]) == 0)
                    return false;
            }
            return true;
        }

        public bool IsIncludeIn(Targets others)
        {
            foreach (ITarget other in others.TargetObjects)
            {
                if (IsIncludeIn(other))
                    return true;
            }
            return false;
        }

        public int[] GetFragmentsValue()
        {
            FieldInfo[] fragmentFields = GetFragmentFieldInfo();

            int[] values = new int[fragmentFields.Length];

            for (int i = 0; i < values.Length; ++i)
            {
                FieldInfo fragmentField = fragmentFields[i];
                int fragmentValue = (int)fragmentField.GetValue(this);
                values[i] = fragmentValue;
            }
            return values;
        }

        public FieldInfo[] GetFragmentFieldInfo()
        {
            if (_fragmentFieldInfoCache == null)
            {
                _fragmentFieldInfoCache = s_cachedTypeToFragmentFieldInfos.GetOrAdd(GetType(), type =>
                {
                    List<FieldInfo> results = new List<FieldInfo>();

                    FieldInfo[] fields = GetType().GetFields();
                    foreach (FieldInfo field in fields)
                    {
                        if (field.FieldType.IsDefined(typeof(Fragment), false))
                            results.Add(field);
                    }
                    results.Sort((l, r) => string.Compare(l.FieldType.FullName, r.FieldType.FullName, StringComparison.Ordinal));

                    return results.ToArray();
                });
            }
            return _fragmentFieldInfoCache;
        }

        #region Private

        private static ConcurrentDictionary<Type, FieldInfo[]> s_cachedTypeToFragmentFieldInfos = new ConcurrentDictionary<Type, FieldInfo[]>();
        private FieldInfo[] _fragmentFieldInfoCache = null;

        private string _valueCache = null;

        #endregion
    }

    public class Targets
    {
        // Type of target object, must derive from Target
        public Type TargetType { get; private set; }

        // Target possibilities contains target with fragments bitfield,
        // will be expose for every possibles combinations of unique target value
        private List<ITarget> _targetPossibilities = new List<ITarget>();

        private Dictionary<Type, List<int>> _fragmentMasks;

        // Contain all possible unique value of target
        private List<ITarget> _targetObjects = null;

        public IEnumerable<ITarget> TargetObjects => _targetObjects;

        public IEnumerable<ITarget> TargetPossibilities => _targetPossibilities;

        public Targets()
        { }

        public Targets(Type targetType, params ITarget[] targets)
        {
            TargetType = targetType;
            AddTargets("", targets);
            BuildTargets();
        }

        private static bool IsPowerOfTwo(ulong number)
        {
            if (number == 0)
                return false;
            for (ulong power = 1; power > 0; power = power << 1)
            {
                if (power == number)
                    return true;
                if (power > number)
                    return false;
            }
            return false;
        }

        private static ConcurrentDictionary<Type, bool> _verifiedTargetTypes = new ConcurrentDictionary<Type, bool>();

        internal void Initialize(Type targetType)
        {
            if (_verifiedTargetTypes.ContainsKey(targetType))
            {
                TargetType = targetType;
                return; // Type already validated.
            }

            if (targetType == null)
                throw new InternalError();

            if (!targetType.IsSubclassOf(typeof(ITarget)))
                throw new InternalError("target type {0} must be a subclass of {1}", targetType.FullName, typeof(ITarget).FullName);

            TargetType = targetType;

            FieldInfo[] fragments = TargetType.GetFields();

            List<Type> fragmentsType = new List<Type>();



            foreach (FieldInfo field in fragments)
            {
                if (!field.FieldType.IsEnum && !field.FieldType.IsDefined(typeof(FlagsAttribute), false))
                    throw new Error("fragment of Target Type must be enum with [Flags] attributes: " + field);

                if (fragmentsType.Contains(field.FieldType))
                    throw new Error("enum type in target must be used only once: " + field.FieldType);

                Type enumType = field.FieldType;
                FieldInfo[] enumFields = enumType.GetFields();

                for (int i = 0; i < enumFields.Length; ++i)
                {
                    // GetFields() does not guarantee order; filter out the enum's special name field
                    if (enumFields[i].Attributes.HasFlag(FieldAttributes.SpecialName))
                        continue;

                    if (enumFields[i].GetCustomAttribute<ObsoleteAttribute>() != null)
                        continue;

                    // combinations of fragments are not actual fragments so skip them
                    if (enumFields[i].GetCustomAttribute<CompositeFragmentAttribute>() != null)
                        continue;

                    // skip duplicate fragment values that have been explicitely marked as ignored
                    if (enumFields[i].GetCustomAttribute<IgnoreDuplicateFragmentValueAttribute>() != null)
                        continue;

                    int enumFieldValue = (int)enumFields[i].GetRawConstantValue();

                    if (enumFieldValue == 0)
                        throw new Error("0 enum field value, fragment value must 1 bit set, {0} fragment: {1}={2}", enumType.FullName, enumFields[i].Name, enumFieldValue);

                    // TODO: check if only one bit flag value
                    if (!IsPowerOfTwo((ulong)enumFieldValue))
                        throw new Error("enum field value must be power of two, ie only one bit set,{0} fragment: {1}={2}", enumType.FullName, enumFields[i].Name, enumFieldValue);

                    // make sure same value is not there twice
                    if (!field.FieldType.IsDefined(typeof(TolerateDoubleAttribute), false))
                    {
                        for (int j = 0; j < enumFields.Length; ++j)
                        {
                            // GetFields() does not guarantee order; filter out the enum's special name field
                            if (enumFields[j].Attributes.HasFlag(FieldAttributes.SpecialName))
                                continue;

                            if (enumFields[j].GetCustomAttribute<ObsoleteAttribute>() != null)
                                continue;

                            // skip duplicate fragment values that have been explicitely marked as ignored
                            if (enumFields[j].GetCustomAttribute<IgnoreDuplicateFragmentValueAttribute>() != null)
                                continue;

                            if (i != j)
                            {
                                int jEnumFieldValue = (int)enumFields[j].GetRawConstantValue();

                                if (enumFieldValue == jEnumFieldValue)
                                {
                                    throw new Error("2 enum fields with the same value found in {0} fragment: {1}={2} and {3}={4}",
                                                        enumType.FullName,
                                                        enumFields[i].Name,
                                                        enumFieldValue,
                                                        enumFields[j].Name,
                                                        jEnumFieldValue
                                    );
                                }
                            }
                        }
                    }
                }

                fragmentsType.Add(field.FieldType);
            }

            // Validate mandatory fragments
            if (!fragmentsType.Contains(typeof(DevEnv)))
                throw new Error("Mandatory fragment type \"{0}\" not found in target type \"{1}\" (fields also must be public)", typeof(DevEnv).ToString(), targetType);
            if (!fragmentsType.Contains(typeof(Platform)))
                throw new Error("Mandatory fragment type \"{0}\" not found in target type \"{1}\" (fields also must be public)", typeof(Platform).ToString(), targetType);

            // Mark this type as validated successfully.
            _verifiedTargetTypes.TryAdd(targetType, true);
        }

        internal void AddTargets(string callerInfo, params ITarget[] targetsMask)
        {
            foreach (ITarget targetMask in targetsMask)
                if (TargetType != targetMask.GetType())
                    throw new Error(callerInfo + "error: Target must be all of the same type " + TargetType + " != " + targetMask.GetType() +
                        "; Are you missing base(typeof(" + targetMask.GetType() + ") in your Project class?");

            _targetPossibilities.AddRange(targetsMask);
        }

        internal void AddTargets(string callerInfo, Targets targets)
        {
            foreach (ITarget targetMask in targets._targetPossibilities)
                if (TargetType != targetMask.GetType())
                    throw new Error(callerInfo + "error: Target must be all of the same type " + TargetType + " != " + targetMask.GetType());

            _targetPossibilities.AddRange(targets._targetPossibilities);
        }

        internal bool IsFragmentValueValid(Type fragmentType, int fragmentValue)
        {
            List<int> maskValues;
            if (_fragmentMasks != null && _fragmentMasks.TryGetValue(fragmentType, out maskValues))
            {
                foreach (var maskValue in maskValues)
                {
                    if ((fragmentValue & maskValue) == fragmentValue)
                    {
                        return true;
                    }
                }
                return false;
            }

            // this type is not masked, accept it
            return true;
        }

        public void AddFragmentMask(params object[] masks)
        {
            var fragmentTypes = TargetType.GetFields();

            foreach (var mask in masks)
            {
                Type maskType = mask.GetType();
                ITarget.ValidFragmentType(maskType);
                if (!fragmentTypes.Any(fragmentType => fragmentType.FieldType == maskType))
                {
                    throw new Error(
                        "Fragment mask type '{0}' is not present in this target, here is the list of valid types:\n- {1}",
                        maskType,
                        string.Join(
                            "\n- ",
                            fragmentTypes
                                .Select(fragmentType => Util.ToNiceTypeName(fragmentType.FieldType))
                                .OrderBy(type => type, StringComparer.InvariantCultureIgnoreCase)
                        )
                    );
                }

                List<int> maskValues;
                if (_fragmentMasks == null || !_fragmentMasks.TryGetValue(maskType, out maskValues))
                {
                    if (_fragmentMasks == null)
                    {
                        _fragmentMasks = new Dictionary<Type, List<int>>();
                    }

                    maskValues = new List<int>();
                    _fragmentMasks.Add(maskType, maskValues);
                }

                maskValues.Add((int)mask);
            }
        }

        /// <summary>
        /// This method will remove all the masks that were added, if any
        /// </summary>
        public void ClearFragmentMasks()
        {
            _fragmentMasks = null;
        }

        /// <summary>
        /// The global fragment mask will add the mask or and it with previously existing masks
        /// </summary>
        /// <param name="masks"></param>
        internal void SetGlobalFragmentMask(params object[] masks)
        {
            foreach (var mask in masks)
            {
                Type maskType = mask.GetType();
                ITarget.ValidFragmentType(maskType);

                List<int> maskValues;
                if (_fragmentMasks == null || !_fragmentMasks.TryGetValue(maskType, out maskValues))
                {
                    if (_fragmentMasks == null)
                        _fragmentMasks = new Dictionary<Type, List<int>>();

                    maskValues = new List<int> { (int)mask };
                    _fragmentMasks.Add(maskType, maskValues);
                }
                else
                {
                    for (int i = 0; i < maskValues.Count; ++i)
                        maskValues[i] &= (int)mask;
                }
            }
        }

        internal void ClearTargets()
        {
            _targetPossibilities.Clear();
        }

        internal void BuildTargets()
        {
            _targetObjects = new List<ITarget>();

            var fragments = TargetType.GetFields();

            var selectCP = _targetPossibilities.Select(tp =>
            {
                var tuples = fragments.Select(f =>
                {
                    int value = (int)f.GetValue(tp);
                    return Tuple.Create(value, BuildFilteredFragmentMask(f, value));
                });
                var filtered = tuples.Where(f =>
                {
                    return f.Item1 == 0 || f.Item2 != 0;
                });
                var cachedPossibility = filtered.Select(f => f.Item2).ToArray();
                return cachedPossibility;
            });
            var cachedPossibilities = selectCP.Where(f => f.Length == fragments.Length) // Filtered out by the _fragmentMasks
            .ToArray();

            //int[] masks;
            //BuildFragmentsMasks(fragments, out masks);

            foreach (var cachedPossibility in cachedPossibilities)
            {
                var current = new int?[fragments.Length];
                bool configValid = IncrementCurrent(fragments, cachedPossibility, current);
                while (configValid)
                {
                    GenerateConfiguration(fragments, current);
                    configValid = IncrementCurrent(fragments, cachedPossibility, current);
                }
            }
        }

        private readonly Dictionary<string, ITarget> _addedTargets = new Dictionary<string, ITarget>();

        private void GenerateConfiguration(FieldInfo[] fragments, int?[] current)
        {
            ITarget target = Activator.CreateInstance(TargetType) as ITarget;

            for (int i = 0; i < fragments.Length; ++i)
                fragments[i].SetValue(target, current[i] ?? 0);

            string targetKey = target.GetType().FullName + "__" + target.GetTargetString();
            if (!_addedTargets.ContainsKey(targetKey))
            {
                _addedTargets.Add(targetKey, target);
                _targetObjects.Add(target);
            }
        }

        private int BuildFilteredFragmentMask(FieldInfo fragment, int optionalMask)
        {
            int mask = 0;

            Type enumType = fragment.FieldType;
            FieldInfo[] enumFields = enumType.GetFields();

            foreach (FieldInfo enumField in enumFields)
            {
                // GetFields() does not guarantee order; filter out the enum's special name field
                if (enumField.Attributes.HasFlag(FieldAttributes.SpecialName))
                    continue;

                int value = (int)enumField.GetRawConstantValue();

                if (IsFragmentValueValid(enumField.DeclaringType, value) && (optionalMask & value) == value)
                {
                    mask |= value;
                }
            }

            return mask;
        }

        private enum NextBitState
        {
            Initialized,
            Incremented,
            Exhausted
        }

        private static NextBitState GetNextBit(int? currentBit, int mask, out int nextBit)
        {
            Func<int, int> getNextPow2 = v =>
            {
                int p = 0;

                while (v > 0)
                {
                    p++;

                    v >>= 1;
                }

                return p;
            };

            Func<int, int, int> getNextBit = (from, current) =>
            {
                for (var bit = getNextPow2(current); bit < 32; ++bit)
                {
                    var shiftedValue = 1 << bit;

                    if ((from & shiftedValue) == shiftedValue)
                    {
                        return shiftedValue;
                    }
                }
                return 0;
            };


            if (currentBit == null)
            {
                if (mask == 0)
                {
                    nextBit = 0;
                    return NextBitState.Exhausted;
                }

                nextBit = getNextBit(mask, 0);
                return NextBitState.Initialized;
            }

            nextBit = getNextBit(mask, currentBit.Value);

            return nextBit > 0 ? NextBitState.Incremented : NextBitState.Exhausted;
        }

        private static bool IncrementCurrent(FieldInfo[] fragments, int[] masks, int?[] current)
        {
            var previousExhausted = false;

            for (var j = fragments.Length - 1; j >= 0; --j)
            {
                int next;
                NextBitState nextState = GetNextBit(current[j], masks[j], out next);

                if (nextState == NextBitState.Initialized)
                {
                    Trace.Assert(current[j] == null);                    // IsFragmentValueValid() is probably masking all enum values of fragments[j].FieldType?
                    current[j] = next;
                }
                else if (nextState == NextBitState.Incremented)
                {
                    current[j] = next;

                    if (previousExhausted)
                    {
                        for (int k = j + 1; k < fragments.Length; ++k)
                        {
                            nextState = GetNextBit(null, masks[k], out next);
                            if (nextState == NextBitState.Initialized)
                                current[k] = next;
                        }
                    }

                    break;
                }
                else if (nextState == NextBitState.Exhausted)
                {
                    current[j] = null;

                    if (j == 0)
                    {
                        return false;
                    }

                    previousExhausted = true;
                }
            }

            return true;
        }
    }

    public struct DevEnvRange
    {
        public DevEnvRange(IEnumerable<Project.Configuration> configurations)
        {
            DevEnv minDevEnv = 0;
            DevEnv maxDevEnv = 0;
            foreach (var conf in configurations)
            {
                DevEnv devEnv = conf.Target.GetFragment<DevEnv>();
                if (devEnv < minDevEnv || minDevEnv == 0)
                    minDevEnv = devEnv;
                if (devEnv > maxDevEnv)
                    maxDevEnv = devEnv;
            }
            MinDevEnv = minDevEnv;
            MaxDevEnv = maxDevEnv;
        }

        public bool Contains(DevEnv devEnv)
        {
            return (MinDevEnv <= devEnv) && (devEnv <= MaxDevEnv);
        }

        public DevEnv MinDevEnv;
        public DevEnv MaxDevEnv;
    }
}
