// Copyright (c) 2022 Ubisoft Entertainment
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
using System.IO;
using System.Text;

namespace Sharpmake
{
    /// <summary>
    /// A Rust project.
    /// 
    /// Requires the cargo build tools to be installed.
    /// For additional build configuration (such as rustflags), use build.rs and
    /// https://doc.rust-lang.org/cargo/reference/config.html#build
    /// </summary>
    public abstract class RustProject : Project
    {
        /// <summary>
        /// Location where all generated artifacts are placed.
        /// </summary>
        protected readonly string CargoTargetDir;

        /// <summary>
        /// Path to the project's Cargo.toml manifest file.
        /// </summary>
        protected readonly string ManifestPath;

        private CargoBuildStep cargoBuildStep;

        /// <summary>
        /// Constructs a Rust project.
        /// </summary>
        /// <param name="targetType">The type of a Target.</param>
        /// <param name="manifestPath">Path to a Cargo.toml manifest file.</param>
        /// <param name="cargoTargetDir">Location where all generated artifacts are placed.</param>
        protected RustProject(Type targetType, string manifestPath, string cargoTargetDir = null)
            : base(targetType)
        {
            CargoTargetDir = cargoTargetDir;
            ManifestPath = manifestPath;

            SourceFilesExtensions.AddRange(new[] { ".toml", ".rs" });
            NoneExtensions.Add(".lock");
        }

        /// <summary>
        /// Adds a Rust build step to the Configuration, for the specified Target.
        /// </summary>
        /// <param name="conf">Project configuration for current target.</param>
        /// <param name="target">Current target.</param>
        protected void AddRustBuildStep(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]";

            conf.Output = Configuration.OutputType.Lib;

            // Link required Windows libraries
            if (target.Platform == Platform.win64)
            {
                conf.LibraryFiles.Add("userenv");
                conf.LibraryFiles.Add("ws2_32");
                conf.LibraryFiles.Add("Bcrypt");
            }

            string libName;
            switch (target.Platform)
            {
                case Platform.win32:
                case Platform.win64:
                    libName = Name + ".lib";
                    break;

                case Platform.ios:
                case Platform.linux:
                case Platform.mac:
                    libName = "lib" + Name + ".a";
                    break;

                default:
                    throw new ArgumentException(String.Format("Unknown library extension for platform: {0}", target.Platform));
            }

            cargoBuildStep = new CargoBuildStep(target, CargoTargetDir, ManifestPath, libName);
            conf.CustomFileBuildSteps.Add(cargoBuildStep);

            conf.TargetCopyFiles.Add(Path.Combine(CargoTargetDir, "[target.Optimization]", libName));
        }

        public override void PostResolve()
        {
            base.PostResolve();

            // Add Rust source files as dependency
            cargoBuildStep.AdditionalInputs.AddRange(ResolvedSourceFiles);
        }

        public class CargoBuildStep : Configuration.CustomFileBuildStep
        {
            public CargoBuildStep(Target target, string cargoTargetDir, string manifestPath, string libName)
            {
                Executable = GetCargoPath(target.Platform);

                ExecutableArguments += " build --manifest-path " + manifestPath;
                if (target.Optimization == Optimization.Release)
                {
                    ExecutableArguments += " --release";
                }

                if (cargoTargetDir != null)
                {
                    ExecutableArguments += " --target-dir " + cargoTargetDir;
                }
                else
                {
                    // Rust default
                    cargoTargetDir = "target";
                }

                KeyInput = manifestPath;

                Output = Path.Combine(cargoTargetDir, "[target.Optimization]", libName);
            }

            internal static string GetCargoPath(Platform platform)
            {
                EnvironmentVariableTarget[] targets = { EnvironmentVariableTarget.Process, EnvironmentVariableTarget.User, EnvironmentVariableTarget.Machine };

                foreach (var target in targets)
                {
                    var values = Environment.GetEnvironmentVariable("PATH", target);
                    foreach (var path in values.Split(Path.PathSeparator))
                    {
                        var fullPath = Path.Combine(path, platform.IsMicrosoft() ? "cargo.exe" : "cargo");
                        if (File.Exists(fullPath))
                        {
                            return fullPath;
                        }
                    }
                }

                throw new FileNotFoundException("Rust's Cargo was not installed");
            }
        }
    }
}
