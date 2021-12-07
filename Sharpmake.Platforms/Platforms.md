Platforms
=========


Open Platforms
--------------
Sharpmake ships with the platform extension *Sharpmake.CommonPlatforms.dll*.
This is a .NET class library that contains everything you need to generate
Visual Studio solutions for C++, C#, and C++/CLI code for Windows. Sharpmake
also has support for generating Xcode and GNU Make-based projects for Mac and
Linux.


NDA Platforms
-------------
Sharpmake was originally designed with built-in support for generating
solutions for video game consoles, which are platforms under strict NDA and
private SDKs. In order release Sharpmake to the open-source community, we had
to strip support for those NDA platforms out of the code base and create an
extension mechanism that allows Ubisoft developers to plug these platforms
back in to compile code that depend on it.

We have pulled the code out simply to comply with our NDA. If you are an
authorized developer outside of Ubisoft and would like to try Sharpmake to
generate projects for one of the platforms listed below, please contact one of
the Sharpmake maintainers on the GitHub repository at
https://github.com/UbisoftInc/Sharpmake

* Sony PlayStation 3
* Sony PlayStation 4
* Microsoft Xbox 360
* Microsoft Xbox One
* Nintendo Wii
* Nintendo WiiU
* Nintendo Switch
* nVidia Shield


Platform References
-------------------
Platforms are shipped as ordinary .NET class libraries that are referenced into
Sharpmake script. Because scripts are standalone though, there is no csproj to
add that reference to. Instead, you specify a platform reference with the
`Sharpmake.Reference` module attribute:
```cs
    [module: Reference("<path-of-your-platform>.dll")]
```

Sharpmake will look for DLL files in the executable's directory, although a
relative path from the executable should work.

Please note that *Sharpmake.CommonPlatforms.dll* is always referenced, so
putting `[module: Reference("Sharpmake.CommonPlatforms.dll")]` in your scripts
is redundant.

There is no problem referencing platforms directly from scripts. The platform
system is designed to isolate Sharpmake itself from the platforms, not the
scripts.
