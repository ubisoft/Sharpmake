# Why Sharpmake?

In 2011, after evaluation of different solutions like Premake and CMake, the team of Assassin's Creed 3 decided to develop Sharpmake, a solution to generate .vcproj and .sln files.  Here's the list of needs from 2011:

> Needs:
> * Easy to add a new lib, should take no more than 5 min. We planned to split engine in many pieces so this point is the very important.
> * Way to define only once project options and reuse it.
> * Include file from directory, no need to merge project anymore
> * Only one tool for all platforms
> * Solution generation support
> * Custom target generation: to minimize project file size, assassin2.vcprog is 311000 line long... probably making VS to lag. Ex: AI programmers should be able to generate only project they work on (win tool release, x360 engine release) that’s it.
> * Separate settings file for all projects including third party. Ex: havoc setting contains export include, lib, path, etc.
> * Easy to debug
> * Easy to edit, no needs to know the knots of the system to change it.
> * Vs2010 support
> * Have common share section for general settings.
> 
> Nice to have:
> * Generate blob projects as well. Chisel will only need to move edit file to work blob.
> * Support many platform in the same project ( x32, x64, x360 and ps3 )
> * Support for C# solutions (and projects?)
> * Generate project for a sub set of files to use from SubmitAssistant.

After an evaluation of Premake and CMake (remember it was in their state in 2011), the conclusion was that developing Sharpmake was worth it on Assassin's Creed.

Premake and CMake have been used for years at Ubisoft and over the years different productions and products inside Ubisoft switched from them to Sharpmake.  This is typically not imposed at all, since Ubisoft is extremely bottom-up; it's even the opposite, for a lot of projects, using something external can be considered better (since maintained by more people).  For productions, Sharpmake became the natural choice for multiple reasons:
* Support for consoles, even those unannounced to public.
* Support to generate FastBuild .bff files.
* Most libraries already have .sharpmake.cs files made on another project.
* Fast generation.
* C# .csproj support.  Support for mixing with .vcxproj in same generated .sln.
* Both C++ and C# programmers are comfortable editing .sharpmake.cs files.
* Intellisense and debugging for .sharpmake.cs files.

At the beginning of 2017, serious discussions were made inside Ubisoft to finally make Sharpmake open-source.  While legal discussions were made, a few developers improved Sharpmake to isolate platform implementation in single .dll files, easing isolation of platforms under NDA.  On September 22th 2017, Sharpmake was finally pushed on Github.

The reason we made Sharpmake open-source is because we still believe it is superior for our needs than any other open-source alternative.  That may not be always the case in the future, so we decided it would be better to let Sharpmake compete with alternatives outside Ubisoft.  The same way FastBuild was adopted outside Ubisoft after being mentioned at CppCon 2014, we think Sharpmake could be interesting outside Ubisoft as well, so it was presented in a lightning talk at CppCon 2017.

Sharpmake is shining with big C++ code bases that may be using C# and FastBuild and may be targeting video game consoles.  This is the context for many projects inside the game industry and we are curious to see how interesting it will make it outside Ubisoft.
