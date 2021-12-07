# Sharpmake

*This documentation is far from complete and is a work in progress. Feel free
to improve it.*



## Table of Contents
-------------------------------------------------------------------------------
* [Sharpmake](#sharpmake)  
  * [Table of Contents](#table-of-contents)  
  * [About this repository](#about-this-repository)  
  * [Introduction](#introduction)  
    * [Quick Feature Overview](#quick-feature-overview)  
      * [Powerful Scripting](#powerful-scripting)  
      * [Accessible for Many Programmers](#accessible-for-many-programmers)  
      * [Exceptional Scalability and Modularity](#exceptional-scalability-and-modularity)  
      * [Generation from Sources](#generation-from-sources)  
      * [Generate Everything Quickly](#generate-everything-quickly)  
      * [Smart Granularity and Fragments](#smart-granularity-and-fragments)  
      * [Target and Configuration Independence](#target-and-configuration-independence)  
      * [Native Blobbing Support](#native-blobbing-support)  
      * [Dependency Management and Flexible Target System](#dependency-management-and-flexible-target-system)
  * [Hello World](#hello-world)  
    * [Create a C++ Source File](#create-a-c-source-file)  
    * [Create a Sharpmake File](#create-a-sharpmake-file)  
    * [Launch Sharpmake](#launch-sharpmake)  
    * [Create a Debugging Environment](#create-a-debugging-environment)  
  * [More Detailed Documentation](#more-detailed-documentation)  
    * [Main Attribute](#main-attribute)  
    * [Custom Argument Type](#custom-argument-type)  
    * [Solution](#solution)  
    * [Project](#project)  
      * [Generate Attribute](#generate-attribute)  
      * [Compile Attribute](#compile-attribute)  
      * [Export Attribute](#export-attribute)  
      * [Preprocessor Define](#preprocessor-define)  
    * [Projects and File Mapping](#projects-and-file-mapping)  
    * [Fragments](#fragments)  
    * [Configure Attribute](#configure-attribute)  
      * [Configure Call Order](#configure-call-order)  
      * [Old Configure Order](#old-configure-order)  
      * [ConfigurePriority Attribute](#configurepriority-attribute)  
      * [Enforce no Dependencies on Configure Order](#enforce-no-dependencies-on-configure-order)  
    * [Targets and Configurations](#targets-and-configurations)  
    * [Sharpmake Strings](#sharpmake-strings)  
    * [Dependencies](#dependencies)  
    * [Compiler, Linker and Other Options](#compiler-linker-and-other-options)  
      * [Per-file Options](#per-file-options)  
    * [File Including](#file-including)  
    * [Blobbing Support](#blobbing-support)  
    * [File Inclusion and Exclusion](#file-inclusion-and-exclusion)  
    * [Ordering String Values](#ordering-string-values)  
    * [Filtering Targets Added in Base Class](#filtering-targets-added-in-base-class)  
    * [Limited Dependencies](#limited-dependencies)  
    * [Preferences](#preferences)  
  * [.NET Support](#net-support)  
    * [C++/CLI Support](#ccli-support)  
    * [C# Support](#c-support)  
      * [C# *Hello, World!*](#c-hello-world)  
      * [References](#references)  
        * [Project References](#project-references)  
        * [.NET References](#net-references)  
        * [External References](#external-references)  
    * [Copy Local](#copy-local)  
    * [Build Actions](#build-actions)  
      * [Resource](#resource)  
      * [Content](#content)  
      * [None](#none)  
    * [Namespace](#namespace)  
    * [Output File Name](#output-file-name)  
    * [WebReferenceUrls](#webreferenceurls)  



## About this repository
-------------------------------------------------------------------------------
This repository has branches mirroring the Sharpmake code used by many
projects.



## Introduction
-------------------------------------------------------------------------------
Sharpmake is an Ubisoft-developed solution to generate *.vcxproj*, *.vcproj*,
*.sln*, and *.csproj* files, as well as potentially more formats. Sharpmake
was developed to generate files very quickly and allow users to easily
generate multiple solutions and projects according to project and user
preferences. It was conceived to be easy to maintain and debug. It has native
support for blobbing unity builds) and has been developed to fulfill production
needs and provide flexible control over what is generated. As far as we know,
it is better than all available open source solutions.

Sharpmake was originally developed by Eric Thiffeault on Assassin's Creed 3 in
2011 and is used on multiple projects at Ubisoft.


### Quick Feature Overview
Here's an overview of some of the features that make Sharpmake stand out.

#### Powerful Scripting
As its name hints, Sharpmake is implemented in C#. But it's not only
implemented in C#, the scripts (they can be either *.sharpmake* or
*.sharpmake.cs* files, but the official extension is the latter) are also C#
files used in a scripting way with dynamic compilation. This makes Sharpmake
very easy to debug, in addition to being very quick. It also delivers a very
powerful language to use within the scripts. The scripts use the Sharpmake API
directly, facilitating the override and expansion of the API, which is
important for sharing Sharpmake across projects. The nature of C# makes it
very easy to write everything once. Sharpmake's "include" system is also very
scalable.

#### Accessible for Many Programmers
The use of C#, including well-known concepts like C# attributes and C#
inheritance, make Sharpmake files less intimidating. The result might be
slightly more verbose than the likes of Jamfiles, makefiles, and so on, but
the results are definitely easier to follow and use paradigms programmers know
well. The usage of .NET directly in *.sharpmake.cs* files to manipulate
strings also makes these files more accessible.

#### Exceptional Scalability and Modularity
One of the biggest strength of Sharpmake might not be obvious at first
glance: Sharpmake clearly allows encapsulation in a single and independent
file of an external library. (External dependencies are a great example of
where it may be useful.) This definition can include everything for the include
path, the library paths, the source code for different platforms, and so on.

#### Generation from Sources
Projects and solutions don't need to be in source control to be used with
Sharpmake. Sharpmake will scan specified folders for sources and provides easy
control over this behavior. For huge productions it avoids the need to merge
these files and provides an easy way to transition between different Visual
Studio versions. It also facilitates the creation of solutions optimized for
specific programmers as well as allowing programmer preferences inside
configurations.

#### Generate Everything Quickly
Sharpmake is designed to rapidly generate all the files a programmer could
potentially need in a single pass.

#### Smart Granularity and Fragments
Simply changing a solution name can automatically affect the number of .sln
files Sharpmake will generate, because a name can contain target-dependent
variables. The number of generated project and solution files is handled
intelligently by Sharpmake, making the end results very easy to maintain.

Sharpmake also presents the concept of fragments, which is what targets are
made of. Fragments will automatically affect the possible granularity that
Sharpmake will evaluate.

#### Target and Configuration Independence
If you want to use custom configuration names, the Sharpmake target system
makes this very easy. This comes in very handy when using external projects.

#### Native Blobbing Support
Sharpmake has native support for blobbing (also known as unity builds),
allowing multiple strategies to reduce compilation and iteration time as much
as possible.

#### Dependency Management and Flexible Target System
Sharpmake has dependency management to allow easy propagation of include and
library paths. Private dependencies are provided to prevent propagation.
Combined with the target system which is also extremely scalable, it lets you
mix different target types together, providing great flexibility. For example,
a software provider could define their own target types, which a program could
use with its own target type. Dependencies are made with a single target type,
but it's trivial to make correspondences between different types. The end
result is something extremely flexible and able to support huge code bases,
similar to programming in general.



## Hello World
-------------------------------------------------------------------------------
Here's a quick example to help you dive into Sharpmake.


### Create a C++ Source File
First, let's a create a *src\main.cpp* file for our sample:

```cpp
// src\main.cpp
#include <iostream>
int main(int, char**)
{
    std::cout << "Hello, World!" << endl;
}
```


### Create a Sharpmake File
Then, let's create a *hello.sharpmake.cs* file for creating an .sln and
.vcxproj for that example:

```cs
// hello.sharpmake.cs
using Sharpmake;
 
namespace HelloWorld
{
    [Sharpmake.Generate]
    public class HelloWorldProject : Project
    {
        public HelloWorldProject()
        {
            Name = "HelloWorld";
 
            AddTargets(new Target(
                    Platform.win32 | Platform.win64,
                    DevEnv.vs2008 | DevEnv.vs2010,
                    Optimization.Debug | Optimization.Release
            ));
 
            SourceRootPath = @"[project.SharpmakeCsPath]\src";
        }
 
        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\generated";
 
            // if not set, no precompile option will be used.
            //conf.PrecompHeader = "stdafx.h";
            //conf.PrecompSource = "stdafx.cpp";
        }
    }
 
    [Sharpmake.Generate]
    public class HelloWorldSolution : Sharpmake.Solution
    {
        public HelloWorldSolution()
        {
            Name = "HelloWorld";
 
            AddTargets(new Target(
                    Platform.win32 | Platform.win64,
                    DevEnv.vs2008 | DevEnv.vs2010,
                    Optimization.Debug | Optimization.Release
            ));
        }
 
        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\generated";
            conf.AddProject<HelloWorldProject>(target);
        }
    }

    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            arguments.Generate<HelloWorldSolution>();
        }
    }
}
```


### Launch Sharpmake
Run the Sharpmake executable to generate the project and solution.

```bat
Sharpmake.Application.exe "/sources(@"hello.sharpmake.cs") /verbose"
```


### Create a Debugging Environment
One advantage of Sharpmake is its ability to be naturally debugged in Visual
Studio. Here is how you can generate your own Debug Solution:

1.  Run sharpmake with the argument parameter `/generateDebugSolution`
    ```bat
       Sharpmake.Application.exe "/sources(@"hello.sharpmake.cs") /verbose /generateDebugSolution"
    ```

2.  A debug solution, *sharpmake_debugsolution.sln*, is created next to your
    sources entry point. It contains all of your sharpmake files and already
    references the sharpmake package you used to generate it. Also, its Debug
    Options are all set up for debugging.

3. Press F5!

If you want the debugger to break as soon as an an exception is raised, you
can do the following:

* *Tools > Options > Debugging > Enable Just My Code*: Checked
* *Debug > Exceptions... > Common Language Runtime Exception > Thrown*: Checked



## More Detailed Documentation
-------------------------------------------------------------------------------


### Main Attribute
Multiple attributes are usd to dictate behavior to Sharpmake. The first one is
`[Sharpmake.Main]`, which is put on the static method that will be called at
Sharpmake's launch:

```cs
[Sharpmake.Main]
public static void SharpmakeMain(Sharpmake.Arguments arguments)
{
    arguments.Generate<HelloWorldSolution>();
}
```


### Custom Argument Type
It's possible to define your own class to handle custom arguments. For
example, this is such a class from Assassin's Creed:

```cs
// class that defines custom command lines arguments
public class GameEngineArguments
{
    public bool BuildSystemHelper = true;
    public Filter Filter = Filter.None;
    public Strings ChangelistSourceFilesFilters;
    public int ChangelistNumber;
 
 
    [CommandLine.Option(
        "buildsystemhelper",
        @"Generate BuildSystemHelper helper files: ex: /buildsystemhelper(<true|false>)")]
    public void CommandLineBuildSystemHelper(bool value)
    {
        BuildSystemHelper = value;
    }
 
    [CommandLine.Option(
        "changelist",
        @"Generate project and solution for a specific changelist: ex: /changelist( 1234 , ""files.txt"")")]
    public void CommandLineChangelist(int changelistNumber, string changelistFile)
    {
        Filter = Filter.Changelist;
        ChangelistNumber = changelistNumber;
        ChangelistSourceFilesFilters = new Strings();
 
        try
        {
            FileInfo changelistFileInfo = new FileInfo(changelistFile);
            using (StreamReader projectFileStream = changelistFileInfo.OpenText())
            {
                string line = projectFileStream.ReadLine();
                while (line != null)
                {
                    if (line != string.Empty)
                    {
                        string filePath = Util.PathMakeStandard(line);
 
                        if (File.Exists(filePath))
                        {
                            ChangelistSourceFilesFilters.Add(filePath);
                        }
                        else
                        {
                            // try to find it if relative
                            string relativePath = Path.Combine(changelistFileInfo.DirectoryName, filePath);
                            if ( File.Exists(relativePath) )
                                ChangelistSourceFilesFilters.Add(new FileInfo(relativePath).FullName);
                            else
                                throw new Error("File path not found '{0}' in changelist file '{1}'", filePath, changelistFileInfo.FullName);
                        }
                    }
                    line = projectFileStream.ReadLine();
                }
            }
        }
        catch ( Exception e )
        {
            if (e is Error)
                throw e;
            else
                throw new Error("Cannot read changelist input file: {0}", changelistFile, e);
        }
    }
}
```

Then in the main:

```cs
Arguments = new GameEngineArguments();
CommandLine.ExecuteOnObject(Arguments);
```

The `CommandLine.ExecuteOnObject` will automatically search for the
`CommandLine.Option` attribute and parse options according to the
corresponding method signature.

Of course, any C# code can be executed in the `Main`, not just solution
building.


### Solution
As seen in HelloWorld, solutions are defined using a C# class with
`Sharpmake.Generate` attribute.

```cs
[Sharpmake.Generate]
public class HelloWorldSolution : Sharpmake.Solution
{
    public HelloWorldSolution()
    {
        Name = "HelloWorld";
 
        AddTargets(new Target(
                Platform.win32 | Platform.win64,
                DevEnv.vs2008 | DevEnv.vs2010,
                Optimization.Debug | Optimization.Release
        ));
    }
 
    [Configure()]
    public void ConfigureAll(Configuration conf, Target target)
    {
        conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
        conf.SolutionPath = @"[solution.SharpmakeCsPath]\generated";
        conf.AddProject<HelloWorldProject>(target);
    }
}
```

The projects are added to the solution in the `Configure` function, defined
through the `Sharpmake.Configure` attribute, as described later in this page.


### Project
As with solutions, C++ projects are defined with classes, overriding, and
inheritance being natural features in C#. Only classes with special attributes
will be considered, so it's possible to define your own base classes:

```cs
namespace GameEngine
{
    public class CommonProject : Project
    {
        // Preference for all projects here
        ...
    }
 
    [Sharpmake.Generate]
    public class MyProject : CommonProject
    {
        // Define project here
        ...
    }
}
```

#### Generate Attribute
As shown in previous examples, the `[Sharpmake.Generate]` attribute is used on
solutions and projects to indicate that they are completely generated in
Sharpmake. The attribute does not interfere with C# inheritance. Other
attributes allow for different strategies, such as `[Sharpmake.Compile]` and
`[Sharpmake.Export]` attributes. Built solutions and their added projects must
have one of these attributes: `[Sharpmake.Generate]`, `[Sharpmake.Compile]` or
`[Sharpmake.Export]`.

#### Compile Attribute
The attribute `[Sharpmake.Compile]` can be used to define projects with a
*.vcxproj* file already available. It can be useful when converting your code
base to Sharpmake, because it can support your old projects to facilitate an
incremental conversion of your projects. When defining a `[Sharpmake.Compile]`
project, you must define what is necessary for the project, it's
configurations, and the output for each.

```cs
namespace GameEngine
{
    [Sharpmake.Compile]
    public class GameLib : BaseProject
    {
        public string BasePath = @"[project.ExternPath]\gamelib";
        public string ProjectName = @"GameLib";
 
        public GameLib()
        {
            PerforceRootPath = @"[project.ExternPath]\gamelib";
        }
 
        [Configure()]
        public void Configure(Configuration conf, Target target)
        {
            string platform = (target.Platform == Platform.Win32 || target.Platform == Platform.Win64) ? "PC" : "[target.Platform]";
            conf.Name = platform + " [target.Optimization]";
            if (target.Mode == Mode.Tool)
            {
                conf.Name = "Editor [target.Optimization]";
                if (target.OutputType == OutputType.Dll)
                    conf.Name = "Editor DLL Export [target.Optimization]";
            }
 
            conf.SolutionFolder = "Extern";
 
            conf.IncludePaths.Add(@"[project.BasePath]\Sources");
 
            conf.ProjectPath = @"[project.BasePath]\Make";
            conf.ProjectFileName = "GameLib.2008";
 
            conf.TargetPath = @"[project.BasePath]\lib\vc2008\Projects\[conf.Name]";
            conf.TargetFileName = ProjectName;
 
            conf.Output = target.OutputType == OutputType.Lib ? Configuration.OutputType.Lib : Configuration.OutputType.Dll;
        }
    }
}
```

#### Export Attribute
Sharpmake has a dependency system which accepts various dependencies, not just
generated projects. The Export project type can be used to link with projects
when only the .lib files are available. For example, this is code from
Assassin's Creed to use Passenger:

```cs
namespace GameEngine
{
    [Sharpmake.Export]
    public class ExternalLib : Sharpmake.Project
    {
        public string ExternPath = Extern.Extern.ExternPath;
        public string BasePath = @"[project.ExternPath]\include\externlib";
 
        public ExternalLib()
        {
            AddTargets(new Sharpmake.Target(
                Platform.win32 | Platform.win64,
                DevEnv.vs2008 | DevEnv.vs2010,
                Optimization.Debug | Optimization.Release,
                OutputType.Dll));
        }
 
        [Configure()]
        public void Configure(Configuration conf, Target target)
        {
            conf.IncludePaths.Add(@"[project.BasePath]\include\externlib");
            conf.TargetFileName = @"gamex";
            conf.Output = Configuration.OutputType.Dll;
        }
    }
}
```

#### Preprocessor Define
It's also possible to define preprocessor macros and symbols, in both C/C++
and .NET languages:

```cs
[Configure(DotNetFramework.v4)]
public void DefineDotNet4(Configuration conf, Target target)
{
    conf.Defines.Add("DOT_NET_4");
}
 
[Configure(DotNetFramework.v4_5)]
public void DefineDotNet45(Configuration conf, Target target)
{
    conf.Defines.Add("DOT_NET_45");
}
 
[Configure(DevEnv.vs2010)]
public void DefineDevEnv2010(Configuration conf, Target target)
{
    conf.Defines.Add("DEVENV_VS2010");
}
 
[Configure(DevEnv.vs2012)]
public void DefineDevEnv2012(Configuration conf, Target target)
{
    conf.Defines.Add("DEVENV_VS2012");
}
```


### Projects and File Mapping
Something great about Sharpmake is that the number of different generated
files for the same project is implicit. The properties used to define the file
paths of projects and solutions can use variable that are dependent on
targets, and Sharpmake will automatically generate the appropriate number of
files with the appropriate targets. For example, on Assassin's Creed, multiple
solutions are used for the game engine, depending on platform, DirectX, and so
on. The definition looks like this:

```cs
namespace GameEngine
{
    public class CommonSolution : Sharpmake.Solution
    {
        [Configure()]
        public virtual void Configure(Configuration conf, Target target)
        {
            conf.SolutionPath = @"[solution.RootPath]\projects\[target.Platform]";
 
            // vs2008 is default
            if ( target.DevEnv == DevEnv.vs2008 )
                conf.SolutionFileName = @"[solution.Name].[target.Mode].[target.Platform]" + gfxAPI;
            else
                conf.SolutionFileName = @"[solution.Name].[target.Mode].[target.Platform]" + gfxAPI + ".[target.DevEnv]";
            // ...
```

But on Rainbow Six Siege, a single solution is used for the game engine,
containing all platforms:

```cs
namespace GameEngine
{
    public class CommonSolution : Sharpmake.Solution
    {
        [Configure()]
        public virtual void Configure(Configuration conf, Target target)
        {
            conf.SolutionPath = @"[solution.RootPath]\temp\sharpmake\solutions";
            conf.SolutionFileName = @"[solution.Name].[target.DevEnv]";
            // ...
```

And that is the beauty of it. Nothing other than the paths from Assassin's
Creed were changed on Rainbow Six to change the solution's granularity. From
the requested solution targets, Sharpmake will automatically deduce how many
project and solution files are needed and how many targets each of them
contain.

The same principle could be used to support even both Assassin's Creed and
Rainbow Six approaches on the same project. Suppose the time to open a
solution is influenced by the number of targets in it, and is wanted to
support both approaches: one solution for all targets and one solution per
target or target type. A simple fragment could be added to achieve that:

```cs
[Fragment, Flags]
public enum SolutionGrouping
{
    OneToRuleThemAll = 0x01,
    OnePerConfig = 0x02,
}
```

Then, the fragment is added to the main target class and is examined to
influence `ProjectFileName` and `SolutionFileName`. The binding between
solutions and projects is done implicitly through the target. The requested
target will contain just `OneToRuleThemAll | OnePerConfig` to request both,
and that's it. Two sets of solutions and project groups will be generated.


### Fragments
One of the core features and biggest strengths of Sharpmake is the use of
fragments. Fragments are what targets are made of. Every project can override
its configuration for any specific fragment value. Some fragments are defined
directly in Sharpmake while others can be defined in *.sharpmake.cs* scripts.

```cs
[Fragment, Flags]
public enum Optimization
{
    Debug       = 0x01,
    AiDebug     = 0x02,
    AiDebugEnc  = 0x04,
    Release     = 0x08,
    Profile     = 0x10,
    QCFinal     = 0x20,
    Final       = 0x40,
}
 
[Fragment, Flags]
public enum GraphicAPI
{
    DirectX9 = 0x01,
    DirectX11 = 0x02,
}
```

The 2 built-in fragments `Platform` and `DevEnv` are mandatory if you create
custom target types. Custom target types must have 2 public fields for
`Platform` and `DevEnv` types.


### Configure Attribute
As previously stated, it's possible to override the configuration for any
fragment value. The `[Sharpmake.Configure]` attribute is passed a list of
values where the associated method should be called. For example, this code is
in the project base class of our game engine:

```cs
[Configure(Mode.Engine, Platform.win32 | Platform.win64)]
public void ModeEngineWindows(Configuration conf, Target target)
{
    conf.Defines.Add("UBI_PLATFORM_PC");
}
```

It's possible to define multiple `Configure` methods, typically giving each
one different fragment values. The name of the method is not important,
because it's really the attribute that drives `Sharpmake`.

It is not mandatory to define multiple `Configure` methods. The preceding
example is the same thing as the following:

```cs
[Configure()]
public void Configure(Configuration conf, Target target)
{
    if (Mode == Mode.Engine && (Platform == Platform.win32 || Platform == Platform.win64)
        conf.Defines.Add("UBI_PLATFORM_PC");
}
```

According to preferences, code can be done in multiple ways, with
`switch/case` being another useful tool:

```cs
[Configure()]
public void Configure(Configuration conf, Target target)
{
    // ...
    switch (target.Optimization)
    {
        case Optimization.Profile:
        case Optimization.Retail:
            conf.Defines.Add("UBI_RETAIL");
            break;
        case Optimization.Debug:
            conf.Defines.Add("UBI_DEBUG");
            break;
        case Optimization.Release:
            conf.Defines.Add("UBI_RELEASE");
            break;
    }
    // ...
}
```

Having `using Sharpmake;` in the script files also gives access to extension
methods for some enum types:

```cs
[Configure()]
public void Configure(Configuration conf, Target target)
{
    // ...
    if (target.Platform.IsMicrosoft())
        conf.Defines.Add("_LIB");
 
    if (target.Platform.IsPC())
        conf.Defines.Add(
            "WIN95",
            "VISUAL",
            "USE_DBG_MODULE",
            "CAN_SET_OPTIONS",
            "DARE_PC_STATICLIB");
    // ...
}
```

It is important to note that only configuration should be modified in
`Configure` methods, not the `Project` itself. The `Project` should only be
modified in the constructor.

#### Configure Call Order
Sharpmake is calling `Configure` methods in the order they appear within the
project or solution class. It is however strongly advised to not depend on
that behavior. Adding multiple `Configure` methods to override specific
settings is much better:

```cs
public class BaseProject : Project
{
    // ...
    [Configure()]
    public virtual void ConfigureWarningAsError(Configuration conf, Target target)
    {
        conf.Options.Add(Sharpmake.Options.Vc.General.TreatWarningAsError.Enable);
    }
    // ...
}
```
```cs
[Sharpmake.Generate]
public class Bloomberg : BaseProject
{
    // ...
    public override void ConfigureWarningAsError(Configuration conf, Target target)
    {
        // Bloomberg, please fix your warnings
    }
    // ...
}
```

#### Old Configure Order
Old versions of Sharpmake were using another `Configure` order.  It was in the
order of declarations, but sub-classes overrides would affect that order.  The
problem was that sometimes empty overrides were kept just to have the same
`Configure` order. It's possible to ask for old `Configure` order:

```cs
[Sharpmake.Main]
public static void SharpmakeMain(Sharpmake.Arguments arguments)
{
    arguments.ConfigureOrder = ConfigureOrder.Old;
    // ...
}
```

#### ConfigurePriority Attribute
Again, it is not recommended to depend on `Configure` order. If necessary, by using `[ConfigurePriority]`, you ensure that the call order is respected even if the `Configure` methods are reordered. Sharpmake will sort them in ascending priority order. (-1 before 0, 0 before 1, etc.) Any `Configure` method without `[ConfigurePriority]` will have a default priority of 0.

Redefining priority is also supported across inheritance.

Here's an example of how `ConfigurePriority` can be used when using
inheritance:

* When `ParentProject` is generated, the sequence is: `Foo`, `Bar`
* When `ChildProject` is generated, the sequence is: `FooBar`, `Bar`, `Foo`
(`Foo` and `Bar` have been reordered)

```cs
[Sharpmake.Generate]
public class ParentProject : Project
{
    ...
    [Configure()]
    [ConfigurePriority(2)]
    public virtual void Bar(Configuration conf, Target target)
    {
        Debug.Assert(executedMethodFlags.Equals(ConfigureMethod.Foo),
            "ParentProject.Bar(...) assert failed",
            "ParentProject.Bar(...) should be the second configure to be invoked in this project");
 
        executedMethodFlags |= ConfigureMethod.Bar;
    }
 
    [Configure()]
    public virtual void Foo(Configuration conf, Target target)
    {
        Debug.Assert((int)executedMethodFlags == 0,
            "ParentProject.Foo(...) assert failed",
            "ParentProject.Foo(...) should be the first configure to be invoked in this project");
 
        executedMethodFlags |= ConfigureMethod.Foo;
    }
}
 
[Sharpmake.Generate]
public class ChildProject : ParentProject
{
    ...
    [ConfigurePriority(0)]
    public override void Bar(Configuration conf, Target target)
    {
        Debug.Assert(executedMethodFlags.Equals(ConfigureMethod.FooBar),
            "ChildProject.Bar(...) assert failed",
            "ChildProject.Bar(...) should be the second configure to be invoked in this project");
 
        executedMethodFlags |= ConfigureMethod.Bar;
    }
 
    [ConfigurePriority(1)]
    public override void Foo(Configuration conf, Target target)
    {
        Debug.Assert(executedMethodFlags.Equals(ConfigureMethod.FooBar | ConfigureMethod.Bar),
            "ChildProject.Foo(...) assert failed",
            "ChildProject.Foo(...) should be the third configure to be invoked in this project");
 
        executedMethodFlags |= ConfigureMethod.Foo;
    }
 
    [Configure()]
    [ConfigurePriority(-1)]
    public void FooBar(Configuration conf, Target target)
    {
        Debug.Assert((int)executedMethodFlags == 0,
            "ChildProject.FooBar(...) assert failed",
            "ChildProject.FooBar(...) should be the first configure to be invoked in this project");
 
        executedMethodFlags |= ConfigureMethod.FooBar;
    }
}
```

#### Enforce no Dependencies on Configure Order
As said more than once already, depending on `Configure` order is a bad idea. 
Once your code is clean regarding that, Sharpmake offers a feature to validate
it is ok, perfect for SubmitAssistant or build system validations. Just add
the argument `/test("QuickConfigure")` to the command-line arguments of
Sharpmake and the exit code will be non-zero if the `ConfigureOrder` cannot be
reversed. Note that the validation will still respect usages of
`ConfigurePriority` attributes, the reversing is done for `Configure` of the
same priority.


### Targets and Configurations
Sharpmake makes a clear distinction between *targets* and *configurations*.
The classes `Project.Configuration` and `Solution.Configuration` are used to
define the configurations in *.vcxproj* and *.sln* files. The targets bind
everything together. For example, the same target can use configurations with
different names depending on the project.

```cs
namespace GameEngine
{
    [Sharpmake.Compile]
    public class GameLib : Sharpmake.Project
    {
        public string ExternPath = Extern.Extern.ExternPath;
        public string BasePath = @"libs\GameLib";
        public string ProjectName = @"GameLib";
 
        public GameLib()
            : base(typeof(Target))
        {
            AddTargets(new Target(
                GameFolder.RainbowSix,
                Platform.win32 | Platform.win64 | Platform.ps3 | Platform.x360,
                DevEnv.vs2008,
                GameEngine.Optimization.Debug | GameEngine.Optimization.Release | GameEngine.Optimization.Profile | GameEngine.Optimization.Final,
                OutputType.Lib,
                Blob.NoBlob,
                GameEngine.Mode.Engine | GameEngine.Mode.Tool));
            PerforceRootPath = @"[project.ExternPath]\Library";
        }
 
        [Configure()]
        public void Configure(Configuration conf, Target target)
        {
            conf.Name = "[target.Platform] [target.Mode]";
            // ...
```

The virtual function will be called for Engine mode if the platform is Win32
or Win64. The name of the method is of no importance. Just make it unique, as
it's really the attribute that is examined. If the method is made virtual,
there is no need to specify the attribute in sub-classes.

We can also see in previous examples why fragments are defined as flags: the
C# "`|`" operator allows multiple fragments to be combined. A single `Target`
instance can be used to actually specify multiple targets.


### Sharpmake Strings
Every string in a Sharpmake solution, project, and configuration support uses
the `[obj.Property]` or `[Property]` format to insert values from properties
inside strings. The resolving is done as late as possible, making it possible
for more global properties to refer more local ones and vice-versa.

The following objects are supported:
* **target**: Property for the current target.
* **conf**: Property for the current configuration. It can be project or solution configuration, depending on which is being generated.
* **project**: Property of the current project.
* **solution**: Property of the current solution.

The built-in property `SharpmakeCsPath` has been already used in previous
examples. Provided as both `solution.SharpmakeCsPath` and
`project.SharpmakeCsPath`, it contains the path of the *.sharpmake.cs* file
where that solution or project was defined. This property is important: it can
be used for libraries to provide *.sharpmake.cs* files inside their library
packages, referring to the rest of the package with paths relative to
`SharpmakeCsPath`.

In addition, multiple properties are added by generators when generating (for
example) .sln or .vcxproj files, such as `solutionGuid`, `projectName`,
`projectFile`, `projectGuid`, `options.*`, and so on.

Note that static properties are supported at the moment.


### Dependencies
Sharpmake has built-in support for dependencies, making it easier to generate
solutions while ensuring they have everything needed. The dependency system
makes Sharpmake very scalable by allowing the definition of include paths,
library paths, library files and more in the definition of the appropriate
library. The dependency system will make these be appropriately inherited in
dependent projects. For example, dependencies in a solution will only be added
if necessary, that is if the target is not a static library. If the target is
a static library, the dependent executable will then have the dependencies.

For inherited properties such as include paths and library paths, Sharpmake
provides the option to choose between public and private dependencies. Private
dependencies are not propagated to dependent projects, so if a project needs
an include path to compile its own source files, but its headers don't need
it, a private include path can be included.

```cs
namespace Extern
{
    [Sharpmake.Export]
    public class SomeProject : MyExternProjectBase
    {
        public string BasePath = @"[project.ExternPath]\someproject";
 
        [Configure()]
        public void Configure(Configuration conf, Target target)
        {
            conf.IncludePaths.Add(@"[project.BasePath]\include");
            conf.PrivateIncludePaths.Add(@"[project.BasePath]\private\include");
            conf.LibraryPaths.Add(@"[project.BasePath]\lib");
            conf.LibraryFiles.Add(@"someproject");
        }
    }
}
```

Then, adding dependencies is as simple as calling `AddDependency` on a
project:
```cs
class MyProject : MyBaseProject
{
    [Configure()]
    public void Configure(Configuration conf, Target target)
    {
        conf.AddPublicDependency<SomeProject>(target);
    }
}
```

As seen in previous example, dependencies themselves can be completely public,
rely on their public/private definition, or be completely private.

Something important to note in the previous example is the target argument
passed to `AddPublicDependency`. This is target object binding the two
projects together. It might not be as simple as a target. For example, you
might to be able to control which third-party library is in debug in your
debug build, or even multiple debug builds. Another use case is something
Sharpmake specifically allows: defining reusable and shareable Sharpmake
script files for specific libraries.

Inherited elements from dependencies is not limited to include paths and
libraries. Files to be copied with the executable can be specified, which is
particularly useful with DLLs, when linking with an implib instead of a
complete static library:

```cs
[Configure()]
public void Configure(Configuration conf, Target target)
{
    conf.IncludePaths.Add(@"[project.BasePath]\include");
    if (target.Platform == Platform.win32)
        conf.LibraryFiles.Add("nvtt_win32");
    else if (target.Platform == Platform.win64)
        conf.LibraryFiles.Add("nvtt_win64");
    conf.LibraryPaths.Add(@"[project.BasePath]\lib");
    if (target.Platform == Platform.win32)
        conf.TargetCopyFiles.Add(@"[project.BasePath]\nvtt_win32.dll");
    else if (target.Platform == Platform.win64)
        conf.TargetCopyFiles.Add(@"[project.BasePath]\nvtt_x64.dll");
}
```


### Compiler, Linker and Other Options
Sharpmake supports a huge number of options when generating Visual C++ project
files and solutions, most them obviously being compiler and linker options. If
a given option is not supported, it's very easy to add support for a new one.
The availability of auto-completion in script files makes using new options
very easy:

```cs
[Configure()]
public void Configure(Configuration conf, Target target)
{
    base.Configure(conf, target);
    conf.Options.Add(new Sharpmake.Options.Vc.Compiler.DisableSpecificWarnings("4996", "4530"));
    conf.Options.Add(Sharpmake.Options.Vc.Compiler.Exceptions.EnableWithSEH);
}
```

All options are passed through `conf.Options`, and their types vary between
simple types and more complex types, as in the previous example. The
`Sharpmake.Options` static class contains all available options, classified
according to the tools and platforms. Setting an option for another platform
will simply have no effect as the underlying generator will not use it. For
this reason, the previous example would work with PS3 targets, but will have
no effect.

There's not much point in enumerating all the available options. Opening
*Options.cs* is simply enough and very easy to follow. This is what it looks
like:

```cs
namespace Sharpmake
{
    static public class Options
    {
        static public class Vc
        {
            static public class General
            {
                public enum CharacterSet
                {
                    Default,
                    Unicode,
                    [Default]
                    MultiByte
                }
 
                public enum WholeProgramOptimization
                {
                    [Default]
                    Disable,
                    LinkTime,
                    Instrument,
                    Optimize,
                    Update
                }
             // ...
```

The `Default` attribute is used to clearly set the default value and quickly
identify it when reading code.

#### Per-file Options
Sharpmake doesn't have precise per-file options to specify any compiler option
for any file. This could be added, but instead Sharpmake provides specific
features to be changed with file regular expressions, which is actually more
powerful and works well enough:

```cs
[Configure()]
public void Configure(Configuration conf, Target target)
{
    conf.PrecompSourceExclude.Add(
        @"\gameengine\audio\audioframework.cpp");
    if (target.Platform.IsPC())
    {
        conf.SourceFilesCompileAsCRegex.Add(@"oggvorbis\.bulk\.cpp$");
    }
    // ...
```


### File Including

```cs
[module: Sharpmake.Include("extern.sharpmake.cs")]
```

For better scalability, Sharpmake supports file including, as some previous
examples have demonstrated. Something to note is that the includes are
necessary for Sharpmake script files to be used outside the debugging
environment. They are not used in the debugging environment, where files are
instead compiled inside a DLL. Just remember to run generation with the
command line after making heavy changes inside the debugging environment.


### Blobbing Support
Blobbing support is native in Sharpmake. Sharmake has the following built-in
fragment:

```cs
[Fragment, Flags]
public enum Blob
{
    // Blob only project, another project references the source files
    Blob = 0x01,
 
    // Normal Visual Studio project without blobbing.
    // Can be combined with Blob inside same solution.
    NoBlob = 0x02,
}
```

The fragment does a single thing, non-blob projects will be present even in
blob configurations, so that all the source is present. The rest must be done
in Sharpmake script files by setting `conf.IsBlobbed` and similar options:

```cs
[Configure(Blob.Blob)]
public virtual void ConfigureBlob(Configuration conf, Target target)
{
    conf.IsBlobbed = true;
    conf.ProjectName += "_Blob";
    conf.SolutionFolder = "Blob";
    conf.ProjectFileName += ".blob";
    conf.IncludeBlobbedSourceFiles = false;
}
```

The following properties can be used to override blobbing default behavior:
* `project.SourceFilesBlobExclude`: Files to exclude from blobs.
* `project.SourceFilesBlobExcludeRegex`: Regex to exclude files from blobs.
* `project.SourceFilesBlobExtension`: Extension of files to be put in blobs.

Work blob numbers can be set in the constructor with `BlobWorkFileCount`. The
normal blob count is automatically provided by `BlobSize`. The property
`BlobSizeOverflow` is used as a threshold to exceed that size when files are
still in the same folder. This approach makes the blobs more stable.


### File Inclusion and Exclusion
Sharpmake provides several utilities to exclude and include files easily in
projects and blobs. The possibility of using C# also comes handy.

A project typically comes with three types of properties:
* `Excludes`: Used to exclude otherwise included files from the project.
* `Includes`: Used to include otherwise excluded files from the project.
* `Filters`: Used to specify exactly what can be included in a project, for the files already included in the project.

Here's a list of available properties in the `Project` class:
* `project.SourceRootPath`: Root to get source files from.
* `project.AdditionalSourceRootPaths`: Additional paths inspected to find source files.
* `project.SourceFiles`: Source files themselves.
* `project.SourceFilesExtension`: Extensions of source files to be added to the project.
* `project.SourceFilesCompileExtension`: Extensions of source files to be compiled in the project.
* `project.SourceFilesFilters`: If specified, only files in this list can be included.
* `project.SourceFilesExclude`: Files to exclude from the project.
* `project.SourceFilesIncludeRegex`: Files matching `SourceFilesIncludeRegex` and `SourceFilesExtension` from the source directory will make `SourceFiles`.
* `project.SourceFilesFiltersRegex`: If specified, only files matching the patterns can be included.
* `project.SourceFilesExcludeRegex`: Source files that match this regex will be excluded from the build.
* `project.SourceFilesBuildExclude`: Source files to exclude from the build from `SourceFiles`.
* `project.ResourceFiles`: Resource files themselves.
* `project.ResourceFilesExtension`: Extension to add resource files automatically from `SourceRootPath`.

Additionally the following are available in the configuration:
* `conf.SourceFilesBuildExclude`: Files to exclude from the project.
* `conf.SourceFilesBuildExcludeRegex`: Patterns to exclude files from the project.
* `conf.PrecompSourceExclude`: Files not using the precompiled header.
* `conf.PrecompSourceExcludeExtension`: Patterns to specify files not using the precompiled header.

Using C# can also provide interesting flexibility. For example, this is code
from the project base class on Osborn in `Configure()`, forcing suffixes like
*_win32.cpp* and folders like */xenon/somefile.cpp* to be excluded from some
configurations automatically:

```cs
var excludedFileSuffixes = new List<string>();
var excludedFolders = new List<string>();
if (target.Platform != Platform.X360)
{
    excludedFileSuffixes.Add("xenon");
    excludedFolders.Add("xenon");
}
if (target.Platform != Platform.Ps3)
{
    excludedFileSuffixes.Add("ps3");
    excludedFolders.Add("ps3");
}
if (target.Platform != Platform.win32)
{
    excludedFileSuffixes.Add("win32");
    excludedFolders.Add("win32");
}
if (target.Platform != Platform.win64)
{
    excludedFileSuffixes.Add("win64");
    excludedFolders.Add("win64");
}
conf.SourceFilesBuildExcludeRegex.Add(@"\.*_(" + string.Join("|", excludedFileSuffixes.ToArray()) + @")\.cpp$");
conf.SourceFilesBuildExcludeRegex.Add(@"\.*\\(" + string.Join("|", excludedFolders.ToArray()) + @")\\");
```


### Ordering String Values
Sharpmake sorts include paths, library paths and libraries, making things more
deterministic and readable, especially considering Sharpmake is fully
multi-threaded using a thread pool. However, it is likely to sometime need to
enforce some order for these things. To fulfill that needs, some Sharpmake
fields are using the `OrderableStrings` type instead of `Strings`, allowing to
optionally supply an integer prevalent in sorting. By default, the integer
value is 0. Negative values will be put first and positive values last. For
example the following can be specified to make these 2 include paths first
after the sort:

```cs
conf.IncludePaths.Add(@"[project.RootPath]\gameengine\audio", -2);
conf.IncludePaths.Add(@"[project.RootPath]\gameengine\renderer", -1);
```

Any integer value can used. For libraries, some are deduced from the
`TargetFilePath`:

```cs
[Sharpmake.Export]
public class SomeProject : Sharpmake.Project
{
    // ...

    [Configure()]
    public void Configure(Configuration conf, Target target)
    {

        // ...

        conf.TargetFileOrderNumber = 1000;  // Put the project last, it has compiled STL symbols that will clash with good ones from game engine.
    }

    // ...

}
```


### Filtering Targets Added in Base Class
It's common to make a base class for projects where targets are added. Instead
of calling the base class constructor differently, it is also possible to call
`AddFragmentMask` instead to filter the targets that should be used:

```cs
[Sharpmake.Generate]
public class LevelEditor : CommonProject
{

    // ...

    public LevelEditor()
    {
        // only in toolmode
        AddFragmentMask(Mode.Tool);

        // ...

    }

    // ...

}
```


### Limited Dependencies
By default, Sharpmake will make sure a project inherits from a dependency
everything needed to use it: other dependencies, used libraries, used include
paths, etc. In some extreme rare situations where this behavior is not wanted,
it is possible to specify exactly what to inherit from a dependency.

The `DependencySetting` enum contains flags for different use cases to specify
precisely what is wanted:

```cs
// InheritFromDependenciesDependencies to get all files to copy
conf.AddPrivateDependency<GameEngineDll>(target, DependencySetting.OnlyDependencyInSolution | DependencySetting.InheritFromDependenciesDependencies);
```

In the specific case of a static library with prebuild event that **must** be
executed before it's inclusion in subsequent project, use
`DependencySetting.ForcedDependencyInSolution`. Otherwise, it is recommended
to use the default option to optimize the compilation process:

```cs
conf.AddPublicDependency<StaticLibraryWithPrebuildEvent>(target, DependencySetting.ForcedDependencyInSolution);
```


### Preferences
Sharpmake has natural support for user preferences. Currently, a per-user
preference file, a *user.sharpmake.cs* file feature, has been developed for
Assassin's Creed, but the feature has been developed completely in
*.sharpmake.cs* files. In the future, we should investigate moving that
feature directly into to Sharpmake, at least as an option.

Already, features like Perforce integration are completely optional. Many
preferences turn out to be pipeline-specific. Like other solutions generating
everything offline and not submitting to Perforce, it allows user preferences
to affect the content of generated Visual C++ project files and solutions. In
the case of solutions, preferences are more likely to influence the number of
solutions generated, by creating solution files dedicated to programmer
targets. For projects, preferences are more likely to affect compiler
optimizations and Perforce integration.



## .NET Support
-------------------------------------------------------------------------------
In addition to generating C++ projects, Sharpmake also supports project
generation for C# and C++/CLI.


### C++/CLI Support
To use C++/CLI, instead of setting the `OutputType` to `Exe`, `Lib` or `Dll`,
use the .NET versions:

```cs
public override void ConfigureAll(Configuration conf, Target target)
{
    base.ConfigureAll(conf, target);
    conf.Output = Configuration.OutputType.DotNetConsoleApp;
}
```

The enumeration of all project types can be found in
*Project.Configuration.cs* in Sharpmake's source code, and looks like this:

```cs
public enum OutputType
{
    Exe,
    Lib,
    Dll,
    DotNetConsoleApp,
    DotNetClassLibrary,
    DotNetWindowsApp,
    None,
}
```


### C# Support
To use the C# version of Sharpmake, use the `CSharpProject` and
`CSharpSolution` base classes when writing your Sharpmake scripts.

> **Tip**: To help having the correct project folder structure, create a new `Project`
>          and then write your sharpmake file.

> **Warning**: To permit retro-compatibility, these classes are derived from
>              their C++ equivalent. That is the reason why `CSharpProject`s
>              have fields like `Blobs`, even though C# projects have no need
>              for blobbing.

#### C# *Hello, World!*

1. Create the *Hello, World!* project.

Create a C# source file with some "Hello, World!" code in it for our sample.

```cs
using System;
 
namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
```

2. Create the Sharpmake script.

Create the project generation script. Let's call it *hello.sharpmake.cs*. It
will create the solution and the project for this example.

```cs
using Sharpmake;
 
namespace CSharpHelloWorld
{
    [Sharpmake.Generate]
    public class HelloWorld : CSharpProject
    {
        public HelloWorld()
        {
            RootPath = @"[project.SharpmakeCsPath]\codebase\";
 
            AddTargets(new Target(
            Platform.anycpu,
            DevEnv.vs2010,
            Optimization.Debug | Optimization.Release,
            OutputType.Dll,
            DotNetFramework.v4));
 
            // This Path will be used to get all SourceFiles in this Folder and all subFolders
            SourceRootPath = @"[project.RootPath]\[project.Name]\source";
        }
        [Configure()]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name].[target.DevEnv].[target.Framework]";
            conf.ProjectPath = @"[project.RootPath]\[project.Name]";
        }
    }
 
    [Sharpmake.Generate]
    public class HelloWorldSolution : CSharpSolution
    {
        public HelloWorldSolution()
        {
            AddTargets(new Target(
                Platform.anycpu,
                DevEnv.vs2010,
                Optimization.Debug | Optimization.Release,
                OutputType.Dll,
                DotNetFramework.v4));
        }
 
        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = string.Format("{0}.{1}.{2}", Name, "[target.DevEnv]", "[target.Framework]");
 
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\codebase\";
 
            conf.AddProject<HelloWorld>(target);
        }
    }

    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            arguments.Generate<HelloWorldSolution>();
        }
    }
}
```

3. [Launch Sharpmake](#launch-sharpmake) as you would normally do for a C++
   project.

#### References
##### Project References
Adding project references works just like it does with C++ projects, with the
`Configuration.AddPrivateDependency<...>(target)` and
`Configuration.AddPublicDependency<...>(target)` methods.

```cs
public override void ConfigureAll(Configuration conf, Target target)
{

    //...

    conf.AddPrivateDependency<Libraries.Ubisoft_Core>(target);

    //...

}
```

##### .NET References
.NET references refers to any reference that you would add with the
*Project->Add Reference* command in Visual Studio, using the .NET tab.

![Visual Studio's Add .NET Reference Dialog](docs/img/AddDotNetRef.png)

```cs
public override void ConfigureAll(Configuration conf, Target target)
{

    //...

    conf.ReferencesByName.AddRange(new Strings(
        "System",
        "System.Core",
        "System.Xml.Linq",
        "System.Data.DataSetExtensions",
        "System.Data",
        "System.Xml"));

    //...

}
```

> **Careful**: In Visual Studio, new projects come with default references:
>    * System
>    * System.Core
>    * System.Data
>    * System.Data.DataSetExtensions
>    * System.Xml
>    * System.Xml.Linq
>
> On the other hand, when Sharpmake generates a .NET project, it will *not*
> put these references unless you tell it to, so you must specify those
> references if you need them.

##### External References
![Visual Studio's Add External Reference Dialog](/docs/img/AddExternalReference.png)

When there is a need for an external reference, use the
`configuration.AddReferenceByPath(@"path")` method.

```cs
[Configure()]
public override void ConfigureAll(Configuration conf, Target target)
{

    //...

    conf.ReferencesByPath.Add(@"[project.RootPath]\external\Divelements\SandDock for WPF\Divelements.SandDock.dll");

    //...

}
```


### Copy Local
`CopyLocal` can be defined per Reference Type using the
`Project.DependenciesCopyLocal` field.

The field represents the combination of `Project.DependenciesCopyLocalTypes`
flags.

```cs
[Flags]
public enum DependenciesCopyLocalTypes
{
    None = 0x00,
    ProjectReferences = 0x01,
    DotNetReferences = 0x02,
    ExternalReferences = 0x04,
}
```

The default settings for each type of reference are as follows:

| Reference Type      | Default value |
|---------------------|---------------|
| Project references  | `true`        |
| .NET references     | `false`       |
| External references | `true`        |

Example:

```cs
public AppsProject()
{
    //Making sure we have ProjectReferences and Externals for apps to run in outputFolder
    DependenciesCopyLocal = DependenciesCopyLocal |
        (DependenciesCopyLocalTypes.ProjectReferences | DependenciesCopyLocalTypes.ExternalReferences);
}
```


### Build Actions
![Visual Studio File Properties](/docs/img/ResourceBuildAction.png)
In Sharpmake most of the Action Builds are generated by the extension and the
path of files. For example, source files will have Compile Build Action, XAML
files will have *Page Build Action*.

Some Build Actions can't be determined only with those parameters, build
actions such as [Resource](#resource), [Content](#content) and [None](#none)
need either a path or extension match to identify the right file association.

#### Resource
Files with this build action will end in the assembly or executable.

By default, files in `"[project.RootPath]\Resources\"` are associated with
this build action. Use the `CSharpProject.ResourcesPath` field to change the
ResourcesRoot folder.

```cs
public class ProjectName : CSharpProject
{
     public ProjectName()
     {

         //...

         ResourcesPath = @"[project.RootPath]\images\";

         //...

     }
}
```

In addition to the resources folder it is also possible to use the file
extension as in C++ with the Project
([File Inclusion and Exclusion](#file-inclusion-and-exclusion)).

> **Warning**: Files embedded in the *.resx* file must not be added to
>              resource file list since it will create a copy in the output
>              assembly.

#### Content
Files with this build action will end in the output folder.

By default, files in `"[project.RootPath]\Content\"` have this build
action. Use the `CSharpProject.ContentPath` field to change the `ContentRoot`
folder.

```cs
public class ProjectName : CSharpProject
{
     public ProjectName()
     {

         //...

         ContentPath = @"[project.RootPath]\HtmlReferences\";

         //...

     }
}
```

It is also possible to add additional content files, with or without the
*Always Copy*, by using the `CSharpProject.AdditionalContent` and
`CSharpProject.AdditionalContentAlwaysCopy` properties. For example:

```cs
[Sharpmake.Generate]
class ExampleAdditionalContent : LibrariesProject
{
    public ExampleAdditionalContent()
    {

        // ...

        AdditionalContent.Add("additional-content-default-copy.txt");
        AdditionalContentAlwaysCopy.Add("additional-content-always-copy.txt");

        // ...

    }

    // ...

}
```

#### None
Files with this build action will not be copied to the output folder. Files
using the extensions listed below have this build action by default.

| Extension     | File type                                 |
|---------------|-------------------------------------------|
| *.config*     | C# project XML configuration files.       |
| *.settings*   | .NET setting definition file.             |
| *.map*        | Debugging maps.                           |
| *.wsdl*       | Web service description language.         |
| *.datasource* | WCF service reference file.               |
| *.cd*         | Microsoft Visual Studio class diagram.    |
| *.doc*        | Microsoft Word document. (Legacy format.) |
| *.docx*       | Microsoft Word document.                  |

You can associate new file extensions to the *None* build action using the
`CSharpProject.NoneExtension`.

```cs
public class ProjectName : CSharpProject
{
     public ProjectName()
     {

         //...

         NoneExtension.Add(".xlsx"); //adding Excel files to the project for developers

         //...

     }

     // ...

}
```


### Namespace
In case the project's name does not match the desired default namespace, you
can assign the correct one with the `CSharpProject.RootNamespace`.

```cs
[Sharpmake.Generate]
class Ubisoft_Core : ExternalProject
{
    public Ubisoft_Core()
    {
        Name = "Ubisoft.Core";
        RootNamespace = "Ubisoft";

        //...

    }

    // ...

}
```


### Output File Name
In case where the project name isn't the same as the output file name wanted,
change the value of `CSharpProject.AssemblyName`.

```cs
[Sharpmake.Generate]
class Ubisoft_Core_Interop : ExternalProject
{
    public Ubisoft_Core_Interop()
    {

        //...

        Name = "Ubisoft.Core";
        AssemblyName = "Ubisoft.Core.Interop";

        //...

    }

    // ...

}
```


### WebReferenceUrls
![Visual Studio Web References](/docs/img/WebReferences.png)

To add a Web reference to the project, create a new
`Sharpmake.WebReferenceUrl` instance and add it to the
`CSharpProject.WebReferenceUrls` list.

```cs
[Sharpmake.Generate]
class Ubisoft_Confluence : LibrariesProject
{
    public Ubisoft_Confluence()
    {
        Name = "Ubisoft.Confluence";
 
        WebReferenceUrls.Add(
            new WebReferenceUrl
            {
                Name = @"...an-url...",
                UrlBehavior = "Dynamic",
                RelPath = @"Web References\atlassian.confluence\",
                UpdateFromURL = @"...an-url...",
                CachedAppSettingsObjectName = "Settings",
                CachedSettingsPropName = "prop_map"
            });
    }

    //...

}
```
