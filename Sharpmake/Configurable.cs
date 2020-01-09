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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sharpmake
{
    public class Configuration
    {
        /// <summary>
        /// Default options is used to select an options if not set, default may be debug or release setting
        /// see Sharpmake.Options class to know associated default setting to debug or release.
        /// </summary>

        public object Owner { get; private set; }
        public ITarget Target { get; private set; }
        public DevEnv Compiler { get; private set; }
        public Platform Platform { get; private set; }
        public List<object> Options { get; } = new List<object>();
        public Options.DefaultTarget DefaultOption { get; set; } = Sharpmake.Options.DefaultTarget.Debug;

        internal virtual void Construct(object owner, ITarget target)
        {
            Owner = owner;
            Target = target;
            Compiler = target.GetFragment<DevEnv>();
            Platform = target.GetPlatform();
        }

        public override string ToString()
        {
            return Owner.GetType().ToNiceTypeName() + ":" + Target;
        }
    }

    public class Configurable<TConfiguration>
        where TConfiguration : Configuration, new()
    {
        private bool _readOnly = false;
        public Targets Targets { get; } = new Targets();                 // Solution Targets
        public IReadOnlyList<TConfiguration> Configurations => _configurations;

        private readonly List<TConfiguration> _configurations = new List<TConfiguration>();

        // Type of Configuration object, must derive from TConfiguration
        public Type ConfigurationType { get; internal protected set; }

        public void AddTargets(params ITarget[] targetsMask)
        {
            Targets.AddTargets("", targetsMask);
        }

        public void AddTargets(
            ITarget targetMask1,
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Targets.AddTargets(Util.FormatCallerInfo(sourceFilePath, sourceLineNumber), targetMask1);
        }

        public void AddTargets(
            ITarget targetMask1,
            ITarget targetMask2,
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Targets.AddTargets(Util.FormatCallerInfo(sourceFilePath, sourceLineNumber), targetMask1, targetMask2);
        }

        public void AddTargets(
            ITarget targetMask1,
            ITarget targetMask2,
            ITarget targetMask3,
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Targets.AddTargets(Util.FormatCallerInfo(sourceFilePath, sourceLineNumber), targetMask1, targetMask2, targetMask3);
        }

        public void AddTargets(
            ITarget targetMask1,
            ITarget targetMask2,
            ITarget targetMask3,
            ITarget targetMask4,
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Targets.AddTargets(Util.FormatCallerInfo(sourceFilePath, sourceLineNumber), targetMask1, targetMask2, targetMask3, targetMask4);
        }

        public void AddTargets(
            ITarget targetMask1,
            ITarget targetMask2,
            ITarget targetMask3,
            ITarget targetMask4,
            ITarget targetMask5,
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Targets.AddTargets(Util.FormatCallerInfo(sourceFilePath, sourceLineNumber), targetMask1, targetMask2, targetMask3, targetMask4, targetMask5);
        }

        public void AddTargets(Targets targets, [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            Targets.AddTargets(Util.FormatCallerInfo(sourceFilePath, sourceLineNumber), targets);
        }

        public void AddFragmentMask(params object[] masks)
        {
            Targets.AddFragmentMask(masks);
        }

        public void ClearTargets()
        {
            Targets.ClearTargets();
        }

        private static bool FilterMethodForTarget(MethodInfo configure, ITarget target)
        {
            Configure configureAttribute = ConfigureCollection.GetConfigureAttribute(configure, inherit: true);
            if (configureAttribute?.Flags != null)
            {
                foreach (object fragmentValue in configureAttribute.Flags)
                {
                    if (!target.AndMask(fragmentValue))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private class ReadOnlyScope : IDisposable
        {
            private readonly Configurable<TConfiguration> _configurable;
            private bool _disposed = false;

            public ReadOnlyScope(Configurable<TConfiguration> configurable)
            {
                _configurable = configurable;
                _configurable.PreInvokeConfiguration();
            }

            ~ReadOnlyScope()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _configurable.PostInvokeConfiguration();
                    _disposed = true;
                }
            }
        }

        internal void InvokeConfiguration(BuildContext.BaseBuildContext context)
        {
            using (var scope = new ReadOnlyScope(this))
            {
                InvokeConfigurationInternal(context);
            }
        }

        protected virtual void PreInvokeConfiguration()
        {
        }

        protected virtual void PostInvokeConfiguration()
        {
        }

        private void InvokeConfigurationInternal(BuildContext.BaseBuildContext context)
        {
            _readOnly = true;
            var configureMethods = context.CreateConfigureCollection(GetType()).ToList();

            // Clear current configurations
            _configurations.Clear();

            var usedTargetNames = new Dictionary<string, ITarget>();

            foreach (ITarget target in Targets.TargetObjects)
            {
                string targetString = target.GetTargetString();
                if (usedTargetNames.ContainsKey(targetString))
                {
                    ITarget otherTarget = usedTargetNames[targetString];
                    string diffString = Util.MakeDifferenceString(target, otherTarget);
                    throw new Error("Target string \"" + target + "\" is present twice; difference is: " + diffString);
                }
                usedTargetNames.Add(targetString, target);

                TConfiguration conf = Activator.CreateInstance(ConfigurationType) as TConfiguration;
                conf.Construct(this, target);
                _configurations.Add(conf);
                var param = new object[] { conf, target };
                foreach (MethodInfo method in configureMethods)
                {
                    if (!FilterMethodForTarget(method, target))
                        continue;

                    try
                    {
                        method.Invoke(this, param);
                    }
                    catch (Exception e)
                    {
                        // SMARTLINE TODO: Should be method line
                        throw new Error(e, "Error executing [Configure(...)] method: {0} {1}; are the method arguments of good types?", GetType().Name, method.ToString());
                    }
                }
            }
            _readOnly = false;
        }

        protected void SetProperty<T>(ref T Property, T value, [CallerFilePath] string sourceFilePath = "", [CallerMemberName]string propertyName = "")
        {
            if (_readOnly)
                throw new Error(Util.GetCurrentSharpmakeCallerInfo() + "Cannot change {0} property \"{1}\" during configuration", Path.GetFileNameWithoutExtension(sourceFilePath), propertyName);

            Property = value;
        }
    }
}
