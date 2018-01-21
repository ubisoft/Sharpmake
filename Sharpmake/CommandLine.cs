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
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;

namespace Sharpmake
{
    public static class CommandLine
    {
        public static readonly string[] DefaultNamespaces = { "System", "System.IO", "System.Linq", "System.Collections.Generic", "Sharpmake" };

        [AttributeUsage(AttributeTargets.Method, Inherited = true)]
        public class Option : Attribute
        {
            public string Name;
            public string Description;

            public Option(string name)
            {
                Name = name;
                Description = string.Empty;
            }

            public Option(string name, string description)
            {
                Name = name;
                Description = description;
            }
        }

        public static void ExecuteOnObject(object obj)
        {
            ExecuteOnObject(obj, GetProgramCommandLine());
        }

        public static void ExecuteOnObject(object obj, string commandLine)
        {
            ExecuteOnObject(obj, commandLine.Trim(), DefaultNamespaces.ToArray());
        }

        public static void ExecuteOnObject(object obj, string commandLine, string[] namespaces)
        {
            Execute(obj.GetType(), obj, commandLine, namespaces);
        }

        public static void ExecuteOnType(Type type)
        {
            ExecuteOnType(type, GetProgramCommandLine());
        }

        public static void ExecuteOnType(Type type, string commandLine)
        {
            ExecuteOnType(type, commandLine, DefaultNamespaces.ToArray());
        }

        public static void ExecuteOnType(Type type, string commandLine, string[] namespaces)
        {
            Execute(type, null, commandLine, namespaces);
        }

        public static string GetProgramCommandLine()
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            if (commandLineArgs.Length > 1)
            {
                string commandLine = Environment.CommandLine.Remove(0, commandLineArgs[0].Length + 1);
                commandLine = commandLine.Trim(' ', '\"');
                commandLine = commandLine.Replace(@"'", @"""");
                return commandLine;
            }

            // No arguments except for executable
            return "";
        }

        public static Parameter[] GetParameters()
        {
            return GetParameters(GetProgramCommandLine());
        }

        public static Parameter[] GetParameters(string commandLine)
        {
            int commandLineHash = commandLine.GetHashCode();
            Parameter[] parameters;
            lock (s_lastCacheLock)
            {
                if (commandLineHash != s_lastCacheHash)
                {
                    s_lastCacheParameters = Parse(commandLine);
                    s_lastCacheHash = commandLineHash;
                }
                parameters = s_lastCacheParameters;
            }
            return parameters;
        }

        public static bool ContainParameter(string name)
        {
            return ContainParameter(name, GetProgramCommandLine());
        }

        private static bool ContainParameter(string name, string commandLine)
        {
            if (commandLine.Length > 0)
            {
                return GetParameters(commandLine).Any((CommandLine.Parameter p) => (p.Name == name));
            }

            return false;
        }

        public static string GetCommandLineHelp(Type type, bool staticMethod)
        {
            StringWriter help = new StringWriter();

            Dictionary<string, List<MethodInfo>> methodMappings = GetMethodsMapping(type, staticMethod);

            List<string> sortedMethodNames = new List<string>(methodMappings.Keys);
            sortedMethodNames.Sort();

            help.WriteLine("Usage: sharpmake [options]");
            help.WriteLine("Options:");

            foreach (string commandLine in sortedMethodNames)
            {
                help.Write("  /" + commandLine);

                foreach (MethodInfo methodInfo in methodMappings[commandLine])
                {
                    Option[] options = methodInfo.GetCustomAttributes(typeof(Option), false) as Option[];
                    foreach (Option option in options)
                    {
                        ParameterInfo[] parameters = methodInfo.GetParameters();
                        string[] parametersName = new string[parameters.Length];
                        for (int i = 0; i < parameters.Length; ++i)
                        {
                            ParameterInfo parameter = parameters[i];

                            if (parameter.ParameterType.IsArray)
                                parametersName[i] = "params " + parameter.ParameterType.Name + " " + parameter.Name;
                            else
                                parametersName[i] = parameter.Name;
                        }
                        help.WriteLine("({0})", String.Join(", ", parametersName));
                        if (option.Description.Length != 0)
                            help.WriteLine("\t{0}", option.Description.Replace(Environment.NewLine, Environment.NewLine + "\t"));
                    }
                }

                help.WriteLine("");
            }

            return help.ToString();
        }

        #region Private

        private static int s_lastCacheHash = 0;
        private static object s_lastCacheLock = new object();
        private static Parameter[] s_lastCacheParameters;

        private static void Execute(Type type, object instance, string commandLine, string[] namespaces)
        {
            bool isStatic = instance == null;

            Parameter[] parameters = GetParameters(commandLine);
            if (parameters.Length == 0)
                return;

            HashSet<string> namespacesSet = new HashSet<string>();
            namespacesSet.UnionWith(namespaces);
            namespacesSet.Add(type.Namespace);

            StringBuilder errors = new StringBuilder();

            Dictionary<string, List<MethodInfo>> optionsNameMethodMapping = GetMethodsMapping(type, isStatic);

            // use to not call more than one for method with parameter overloading...
            HashSet<string> executedMethods = new HashSet<string>();

            // tell to use all loaded assembly
            List<Assembly> assemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());

            // in case of scripted c#, associated assembly may not be loaded in the current application domain.
            if (!assemblies.Contains(type.Assembly))
                assemblies.Add(type.Assembly);

            foreach (Parameter parameter in parameters)
            {
                executedMethods.Clear();

                List<MethodInfo> methodInfos;
                if (optionsNameMethodMapping.TryGetValue(parameter.Name, out methodInfos))
                {
                    foreach (MethodInfo methodInfo in methodInfos)
                    {
                        try
                        {
                            string uniqueExecutedMethodName = methodInfo.Name + parameter;
                            if (executedMethods.Contains(uniqueExecutedMethodName))
                                continue;

                            if (isStatic)
                            {
                                string executeCode = String.Format("{0}.{1}({2});", type.FullName.Replace("+", "."), methodInfo.Name, parameter.Args);
                                Action execute = Assembler.BuildDelegate<Action>(executeCode, type.Namespace, DefaultNamespaces.ToArray(), assemblies.ToArray());

                                try
                                {
                                    execute();
                                }
                                catch (TargetInvocationException e)
                                {
                                    if (e.InnerException != null)
                                        throw (e.InnerException);
                                }
                            }
                            else
                            {
                                string executeCode = String.Format("((global::{0})obj).{1}({2});", type.FullName.Replace("+", "."), methodInfo.Name, parameter.Args);
                                Action<object> execute = Assembler.BuildDelegate<Action<object>>(executeCode, type.Namespace, DefaultNamespaces.ToArray(), assemblies.ToArray());

                                try
                                {
                                    execute(instance);
                                }
                                catch (TargetInvocationException e)
                                {
                                    if (e.InnerException != null)
                                        throw (e.InnerException);
                                }
                            }
                            executedMethods.Add(uniqueExecutedMethodName);
                        }
                        catch (Error e)
                        {
                            string[] parametersName = methodInfo.GetParameters().Select((ParameterInfo p) => p.ToString()).ToArray();
                            errors.Append(String.Format("Command line option '/{0}' have invalid parameters '({1})', maybe not compatible with '({2})'" + Environment.NewLine + "\t",
                                parameter.Name,
                                parameter.Args,
                                String.Join(", ", parametersName)));

                            errors.Append(e.Message + Environment.NewLine);
                        }
                    }
                }
            }
            if (errors.Length != 0)
                throw new Error(errors.ToString());
        }

        private static Dictionary<string, List<MethodInfo>> GetMethodsMapping(Type type, bool isStatic)
        {
            Dictionary<string, List<MethodInfo>> results = new Dictionary<string, List<MethodInfo>>(StringComparer.OrdinalIgnoreCase);

            MethodInfo[] methodInfos = type.GetMethods();

            foreach (MethodInfo methodInfo in methodInfos)
            {
                if (methodInfo.IsStatic != isStatic)
                    continue;

                Option[] options = methodInfo.GetCustomAttributes(typeof(Option), false) as Option[];
                foreach (Option option in options)
                {
                    List<MethodInfo> optionsMethodInfo;
                    if (!results.TryGetValue(option.Name, out optionsMethodInfo))
                    {
                        optionsMethodInfo = new List<MethodInfo>();
                        results.Add(option.Name, optionsMethodInfo);
                    }
                    optionsMethodInfo.Add(methodInfo);
                }
            }
            return results;
        }

        public class Parameter
        {
            public string Name = string.Empty;
            public string Args = string.Empty;
            public int ArgsCount = 0;

            public override string ToString()
            {
                if (ArgsCount == 0)
                    return String.Format("/{0}", Name);

                return String.Format("/{0}({1})", Name, Args);
            }
        }

        // LC TODO : simplify this
        public static Parameter[] Parse(string commandLine)
        {
            List<Parameter> parameters = new List<Parameter>();
            int commandLineLength = commandLine.Length;

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < commandLineLength;)
            {
                // skip white space
                for (; i < commandLineLength && char.IsWhiteSpace(commandLine[i]); ++i)
                {
                }

                // read parameter name
                Error.Valid(i < commandLineLength && commandLine[i] == '/',
                            "Command line parameters must begin with '/'" + Environment.NewLine + "\t{0}" + Environment.NewLine + "\tAt offset {1}",
                            commandLine, i);
                ++i;

                // skip white space
                for (; i < commandLineLength && char.IsWhiteSpace(commandLine[i]); ++i)
                {
                }

                builder.Remove(0, builder.Length);
                for (; i < commandLineLength && char.IsLetterOrDigit(commandLine[i]); ++i)
                    builder.Append(commandLine[i]);

                Error.Valid(builder.Length != 0,
                            "Command line invalid parameter name" + Environment.NewLine + "\t{0}" + Environment.NewLine + "\tAt offset {1}",
                            commandLine, i);

                Parameter parameter = new Parameter();
                parameters.Add(parameter);
                parameter.Name = builder.ToString();

                // read parameter value

                // skip white space
                for (; i < commandLineLength && char.IsWhiteSpace(commandLine[i]); ++i)
                {
                }

                // check is parameters have value
                if (i >= commandLineLength || commandLine[i] != '(')
                    continue;
                // skip '('
                ++i;

                bool validValue = false;
                builder.Remove(0, builder.Length);
                int innerBraceCount = 0;
                for (; i < commandLineLength; ++i)
                {
                    switch (commandLine[i])
                    {
                        case '(':
                            {
                                ++innerBraceCount;
                            }
                            break;
                        case ')':
                            {
                                if (innerBraceCount == 0)
                                    validValue = true;
                                else
                                    --innerBraceCount;
                            }
                            break;
                        case ',':
                            {
                                if (innerBraceCount == 0)
                                    ++parameter.ArgsCount;
                            }
                            break;
                    }

                    if (validValue)
                        break;

                    builder.Append(commandLine[i]);

                    Error.Valid(innerBraceCount >= 0,
                                "Command line invalid brace" + Environment.NewLine + "\t{0}" + Environment.NewLine + "\tAt offset {1}",
                                commandLine, i);
                }

                Error.Valid(validValue, "Command line invalid parameter value:" + Environment.NewLine + "\t{0}" + Environment.NewLine + "\tAt offset {1}", commandLine, i);

                parameter.Args = builder.ToString().Trim();

                if (!String.IsNullOrEmpty(parameter.Args))
                    ++parameter.ArgsCount;

                // skip ')'
                ++i;

                // skip white space
                for (; i < commandLineLength && char.IsWhiteSpace(commandLine[i]); ++i)
                {
                }
            }

            return parameters.ToArray();
        }
        #endregion
    }
}
