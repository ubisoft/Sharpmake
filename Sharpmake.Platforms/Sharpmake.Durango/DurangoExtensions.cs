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
using System.IO;
using System.Linq;
using static Sharpmake.Durango;

namespace Sharpmake
{
    public static class DurangoExtensions
    {
        public static string GetDurangoBinPath(this DevEnv visualVersion)
        {
            switch (visualVersion)
            {
                case DevEnv.vs2012:
                    {
                        if (Durango.Util.IsDurangoSideBySideXDK())
                            return Path.Combine(GlobalSettings.DurangoXDK, GlobalSettings.XdkEditionTarget, @"Compilers\dev11.1\vc\bin\amd64");

                        return Path.Combine(GlobalSettings.DurangoXDK, "xdk", "VC", "bin", "amd64");
                    }
                case DevEnv.vs2015:
                case DevEnv.vs2017:
                    return visualVersion.GetVisualStudioBinPath(Platform.win64);
            }

            throw new NotImplementedException("This devEnv (" + visualVersion + ") is not supported on durango!");
        }

        public static string GetDurangoIncludePath(this DevEnv visualVersion)
        {
            string durangoXDKPath = Util.EnsureTrailingSeparator(
                Path.Combine(
                    GlobalSettings.DurangoXDK,
                    Durango.Util.IsDurangoSideBySideXDK() ? GlobalSettings.XdkEditionTarget : string.Empty
                )
            );

            List<string> includePath = new List<string>();
            includePath.Add(@"{0}xdk\Include\um;");
            includePath.Add(@"{0}xdk\Include\shared;");
            includePath.Add(@"{0}xdk\Include\winrt;");
            includePath.Add(@"{0}xdk\Include\cppwinrt;");

            switch (visualVersion)
            {
                case DevEnv.vs2012:
                    {
                        if (Durango.Util.IsDurangoSideBySideXDK())
                        {
                            includePath.Add(@"{0}Compilers\dev11.1\crt\include;");
                            includePath.Add(@"{0}Compilers\dev11.1\crt\platform\amd64;");
                        }
                        else
                        {
                            includePath.Add(@"{0}xdk\crt\include;");
                            includePath.Add(@"{0}xdk\crt\platform\amd64;");
                        }
                    }
                    break;
                case DevEnv.vs2015:
                    {
                        includePath.Add(@"{0}xdk\ucrt\inc;");
                        includePath.Add(@"{0}xdk\VS2015\vc\include;");
                        includePath.Add(@"{0}xdk\VS2015\vc\platform\amd64;");
                    }
                    break;
                case DevEnv.vs2017:
                    {
                        includePath.Add(@"{0}xdk\ucrt\inc;");

                        if (GlobalSettings.EnableLegacyXdkHeaders)
                        {
                            includePath.Add(@"{0}xdk\VS2015\vc\include;");
                            includePath.Add(@"{0}xdk\VS2015\vc\platform\amd64;");
                        }
                        else
                        {
                            includePath.Add(@"{0}xdk\VS2017\vc\include;");
                            includePath.Add(@"{0}xdk\VS2017\vc\platform\amd64;");
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException("No DurangoIncludePath associated with " + visualVersion);
            }

            includePath.Add(@"{0}xdk;"); // needed?

            return string.Format(string.Join("", includePath), durangoXDKPath);
        }

        public static string[] GetDurangoUsingDirectories(this DevEnv visualVersion)
        {
            Strings usingDirectories = new Strings();
            usingDirectories.Add(Path.Combine(Durango.Util.GetDurangoExtensionXDK(), "References", "CommonConfiguration", "Neutral"));

            string durangoXDKPath = GlobalSettings.DurangoXDK;

            switch (visualVersion)
            {
                case DevEnv.vs2012:
                    {
                        if (Durango.Util.IsDurangoSideBySideXDK())
                            usingDirectories.Add(Path.Combine(durangoXDKPath, GlobalSettings.XdkEditionTarget, @"Compilers\dev11.1\crt\platform\amd64"));
                        else
                            usingDirectories.Add(Path.Combine(durangoXDKPath, @"xdk\crt\platform\amd64"));
                    }
                    break;
                case DevEnv.vs2015:
                    {
                        string trailingPath = @"xdk\VS2015\vc\platform\amd64";
                        if (Durango.Util.IsDurangoSideBySideXDK())
                            usingDirectories.Add(Path.Combine(durangoXDKPath, GlobalSettings.XdkEditionTarget, trailingPath));
                        else
                            usingDirectories.Add(Path.Combine(durangoXDKPath, trailingPath));
                    }
                    break;
                case DevEnv.vs2017:
                    {
                        string trailingPath = (GlobalSettings.EnableLegacyXdkHeaders) ? @"xdk\VS2015\vc\platform\amd64" : @"xdk\VS2017\vc\platform\amd64";

                        if (Durango.Util.IsDurangoSideBySideXDK())
                            usingDirectories.Add(Path.Combine(durangoXDKPath, GlobalSettings.XdkEditionTarget, trailingPath));
                        else
                            usingDirectories.Add(Path.Combine(durangoXDKPath, trailingPath));
                    }
                    break;
                default:
                    throw new NotImplementedException("No DurangoUsingDirectories associated with " + visualVersion);
            }
            return usingDirectories.ToArray();
        }

        public static string GetDurangoLibraryPath(this DevEnv visualVersion)
        {
            string durangoXDKPath = Util.EnsureTrailingSeparator(
                Path.Combine(
                    GlobalSettings.DurangoXDK,
                    Durango.Util.IsDurangoSideBySideXDK() ? GlobalSettings.XdkEditionTarget : string.Empty
                )
            );

            switch (visualVersion)
            {
                case DevEnv.vs2012:
                    {
                        if (Durango.Util.IsDurangoSideBySideXDK())
                        {
                            return string.Format(
                                string.Concat(
                                    @"{0}xdk\lib\amd64;",
                                    @"{0}Compilers\dev11.1\crt\lib\amd64;",
                                    @"{0}Compilers\dev11.1\crt\platform\amd64;"
                                ),
                                durangoXDKPath
                            );
                        }
                        else
                        {
                            return string.Format(
                                string.Concat(
                                    @"{0}xdk\lib\amd64;",
                                    @"{0}xdk\crt\lib\amd64;",
                                    @"{0}xdk\crt\platform\amd64;"
                                ),
                                durangoXDKPath
                            );
                        }
                    }
                case DevEnv.vs2015:
                    {
                        return string.Format(
                            string.Concat(
                                @"{0}xdk\Lib\amd64;",
                                @"{0}xdk\ucrt\lib\amd64;",
                                @"{0}xdk\VS2015\vc\lib\amd64;",
                                @"{0}xdk\VS2015\vc\platform\amd64;"
                            ),
                            durangoXDKPath
                        );
                    }
                case DevEnv.vs2017:
                    {
                        if (GlobalSettings.EnableLegacyXdkHeaders)
                        {
                            return string.Format(
                                string.Concat(
                                    @"{0}xdk\Lib\amd64;",
                                    @"{0}xdk\ucrt\lib\amd64;",
                                    @"{0}xdk\VS2015\vc\lib\amd64;",
                                    @"{0}xdk\VS2015\vc\platform\amd64;"
                                ),
                                durangoXDKPath
                            );
                        }
                        else
                        {
                            return string.Format(
                                string.Concat(
                                    @"{0}xdk\Lib\amd64;",
                                    @"{0}xdk\ucrt\lib\amd64;",
                                    @"{0}xdk\VS2017\vc\lib\amd64;",
                                    @"{0}xdk\VS2017\vc\platform\amd64;"
                                ),
                                durangoXDKPath
                            );
                        }
                    }
                default:
                    throw new NotImplementedException("No DurangoLibraryPath associated with " + visualVersion);
            }
        }
    }
}
