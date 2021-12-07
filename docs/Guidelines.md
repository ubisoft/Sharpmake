# Sharpmake Guidelines

We get often asked what are the suggested guidelines to use Sharpmake.  Here a few guidelines that we suggest to follow.

## Understand the two possible approach with targets

There are 2 typical ways of defining targets in Sharpmake.  The first one is use different target types for different projects.  This is mostly useful for 3rd party libraries.  It implies to make a conversion between target types:

```csharp
namespace SomeLib
{
    class Target : ITarget { ... }
    class SomeLib : Project 
    {
        SomeLib() : Project(typeof(Target)) { ... }
        ...
    }
}
namespace MyNamespace
{
    class Target : ITarget
    {
        ...
        SomeLib.Target GetSomeLibTarget() { return ...; }
    }
    [Generate]
    class MyProject : Project
    {
        [Configure]
        void Configure(Configuration conf, Target target)
        {
            ...
            conf.AddPublicDependency<SomeLib.SomeLib>(target.GetSomeLibTarget());
        }
    }
}
```

The other approach is to simply use the same target type:

```csharp
namespace MyNamespace
{
    class SomeLib : Project 
    {
        SomeLib() : Project(typeof(Target)) { ... }
        ...
    }
    class Target : ITarget
    {
        ...
    }
    [Generate]
    class MyProject : Project
    {
        [Configure]
        void Configure(Configuration conf, Target target)
        {
            ...
            conf.AddPublicDependency<SomeLib>(target);
        }
    }
}
```

Which one is better?  It depends.  Different target types are providing mainly 2 advantages:
* It can make the library .sharpmake.cs file independent and reusable in other contexts.
* The number of targets added in the library can be smaller and minimal.

Using the same target type can however end up simpler, and we've seen projects at Ubisoft minimizing different target types to simplify code.  Different target types can end up tricky to do properly if you end up with diamond dependencies; Sharpmake supports them well, it's just that the conversion on both sides of the diamond must be coherent.

## Targets are compile-time tools

Whatever the solution you use, you don't want to give different meaning to the same Target type. Keep the target type as a compile-time tool, not run-time. It means you should **not** do the following:

```csharp
class Target
{
    ...
    Target GetSomeLibTarget() { return ...; }  // same target type; this is wrong
```

The problem with the previous example, is that the following would compile while what is wanted is to make a conversion:

```csharp
    [Generate]
    class MyProject : Project
    {
        [Configure]
        void Configure(Configuration conf, Target target)
        {
            ...
            conf.AddPublicDependency<SomeLib>(target);  // target.GetSomeLibTarget intended
        }
    }
```


## Write things once

Using C# in Sharpmake is offering multiple ways to write things a single time.

### Use a class hierarchy and virtual methods

It is strongly suggested to define a class hierarchy for projects to move common definition higher in hierarchy.  

Configure methods can be virtual and one possible approach is to do something like the following:

```csharp
    [Generate]
    class MyProject : Project
    {
        [Configure]
        virtual void Configure(Configuration conf, Target target)
        {
            ...
        }
    }
    [Generate]
    class MySubProject : MyProject
    {
        override void Configure(Configuration conf, Target target)
        {
            base.Configure(conf, target);
            ...
        }
    }
```

Another approach is to use other virtual methods to configure specific parts of a Configuration:

```csharp
    [Generate]
    class MyProject : Project
    {
        [Configure]
        void Configure(Configuration conf, Target target)
        {
            ConfigureOptimization(conf, target);
            ConfigureDefines(conf, target);
        }

        virtual void ConfigureOptimization(Configuration conf, Target target) { ... }
        virtual void ConfigureDefines(Configuration conf, Target target) { ... }
    }
    [Generate]
    class MySubProject : MyProject
    {
        override void Configure(Configuration conf, Target target)
        {
            base.Configure(conf, target);
            ...
        }
    }
```

The level of granularity of these virtual methods is up to you.  It can be very small:

```csharp
        [Configure]
        void Configure(Configuration conf, Target target)
        {
            ...
            ConfigureStringPooling(conf, target);
            ...
        }
        public virtual void ConfigureStringPooling(Configuration conf, Target target)
        {
            conf.Options.Add(Sharpmake.Options.Vc.Compiler.StringPooling.Disable);
        }
```

Multiple Configure functions can also be used, and Sharpmake is offering a feature to make them specific a to subset of the targets in the project:

```csharp
[Configure(Platform.win32 | Platform.win64)]
void ConfigureWindows(Configuration conf, Target target)
{
    ...
}

[Configure(Platform.orbis)]
void ConfigureOrbis(Configuration conf, Target target)
{
    ...
}
 
[Configure(Platform.durango)]
void ConfigureDurango(Configuration conf, Target target)
{
    ...
}
```

If doing multiple Configure functions, it is strongly recommended to avoid depending on Configure methods execution order.  If you do, the attribute ```[ConfigureOrder]``` can be used.  Sharpmake is using the order of declaration in the class as much as possible, but then changing a Configure method from virtual to override with code moved in a base class can change the execution order.  Many programmers are completely surprised when simply moving code around is changing the produced result.  For these reasons it is suggested that different Configure functions work on different and independent things.

### Dependency System

The dependency system is offering tools to write things about a library a single time, making sure dependent projects get all the required preprocessor definitions, includes, etc.  It is suggested to use the dependency system to write things only once.

For example, while Sharpmake allows you to do the following:

```csharp
conf.LibraryFiles.Add("Iphlpapi.lib");
```

It might be better to take the time to define everything for that library once:

```csharp
[Sharpmake.Export]
class IPHelperAPI
{
    void Configure(Configuration conf, Target target)
    {
        ...  // configure for target .lib, needed include path and define for dependents
    }
}
```

That project can then be added as a dependency.

### Public static functions

C# public static functions can offer an easy way to write code a single time.  Suppose you have the following:

```csharp
class MyBaseProject : Project
{
    public MyBaseProject : base(typeof(Target))
    {
        AddTargets(new Target(
            Platform.win32 | Platform.win64, 
            DevEnv.vs2015,
            ...));
    }
}
```

Even if you add targets in base class, in a big code base you might add targets in multiple classes.  And when you do, there's probably something common between all these, like DevEnv version or platforms.  You can choose to call functions dedicated to these:

```csharp
class MyBaseProject : Project
{
    public MyBaseProject : base(typeof(Target))
    {
        AddTargets(new Target(
            Settings.GetDefaultPlatforms(),
            Settings.GetDefaultDevEnvs(),
            ...));
    }
}
```

You can then in a single place configure these default values:

```csharp
static class Settings
{
    Platform GetDefaultPlatforms() { return Platform.win32 | Platform.win64; }
    DevEnv GetDefaultDevEnvs() { return DevEnv.vs2015; }
}
```

Here, in a transition between 2 DevEnv versions for example, you could generate 2 versions with a single line change:

```csharp
    DevEnv GetDefaultDevEnvs() { return DevEnv.vs2015 | DevEnv.vs2017; }
```

These can be also enriched to be configured by preference files per user, but that's another topic.

## Don't destroy base classes

We've seen in the past some programmers clear completely targets added in a base class to replace them completely:

```csharp
class Perforce : CommonProject
{
    Perforce()
    {
        ClearTargets();
        AddTargets(...);
    }
```

This is something to avoid, as more of a pain to maintain.  This is typically done because these programmers ignore that Sharpmake provides a feature to keep only a subset of the targets from the base class:

```csharp
class Perforce : CommonProject
{
    Perforce()
    {
        AddFragmentMask(Mode.Tool, Optimization.Debug | Optimization.Release);
    }
```

This is much better, even if the Is-A relationship is slightly incorrect.  At least this technique is easier to maintain.  It can be used for any combination of fragments and can be useful to minimize multiplying the number of base classes.  Another example:

```csharp
class MyBaseProject : Project
{
    public MyBaseProject : base(typeof(Target))
    {
        AddTargets(new Target(Platform.win32 | Platform.win64, ...));
    }
}
[Generate]
class MyProject : MyBaseProject
{
    public MyProject
    {
        AddFragmentMask(Platform.win64);  // don't need win32
    }    
}
```




## Consider switch/case with exception for big fragments

Here's the Optimization fragment from Rainbow Six Siege project:

```csharp
[Fragment, Flags]
public enum Optimization
{
    Debug = 0x01,
    AiDebug = 0x02,
    AiDebugEnc = 0x04,
    Release = 0x08,
    Profile = 0x10,
    MemTagFinal = 0x20,
    QCFinal = 0x40,
    Final = 0x80,
    FinalLTO = 0x100,
}
```

This is a big enum and some entries in it are driven by per-user preferences to avoid generating them uselessly.  Nevertheless, the .sharpmake.cs code files must support all entries, and using a switch/case in C# can provide the advantage of not forgetting any entry:

```csharp
switch (target.Optimization)
{
    case Optimization.Debug:
        conf.Defines.Add("_DEBUG");
        break;
    case Optimization.FinalLTO:
    case Optimization.Profile:
    case Optimization.MemTagFinal:
    case Optimization.QCFinal:
    case Otimization.Final:
        conf.Defines.Add("NDEBUG", "UBI_FINAL");
        break;
    case Optimization.AiDebug:
    case Optimization.AiDebugEnc:
    case Optimization.Release:
        conf.Defines.Add("NDEBUG");
        break;
    default:
        throw new Error("Bad optimization type in target. {0}", this);
}
```

## Use a C# IDE like Visual Studio

Using a C# IDE with auto-complete like Visual Studio is making it easier to use and learn Sharpmake.  Sharpmake is designed to provide all compiler and linker option through options discoverable through auto-complete/Intellisense.  Using an IDE is suggested.


