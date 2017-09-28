# Sharpmake


## Introduction
Sharpmake is a solution to generate Visual Studio project files for C++, C#,
VSIX, as well as FastBuild bff files, and linux makefiles.

It is similar to Premake, but in C#. Originally developed on Assassin's Creed 3
in 2011, Sharpmake is today used by most projects at Ubisoft.


## Documentation
The best place for the Sharpmake documentation is the
[wiki on GitHub](https://github.com/ubisoftinc/Sharpmake/wiki). The Sharpmake
source code also comes with samples that you can study.


## Building Sharpmake
Building Sharpmake is quite straightforward. Clone the repo on GitHub, open the
solution in Visual Studio and build the solution in *Release*. The binaries
will be found in the *Sharpmake.Application/bin/Release*. You can run the
*deploy_binaries.py* script to automatically fetch the binaries and copy them
in a *Binaries* folder.


## More Platforms
Sharpmake originally had support for game consoles, but Ubisoft pulled it out
because those could not be open sourced. Sharpmake now has an extension system
that allows support for these consoles to be added back at runtime.

If you need support for these platforms and are an authorized developper, you
can contact the SDK provider to get platform extension for Sharpmake.


## Contributing

### Tests
We will only accept merge requests that pass every tests. The unit tests are
written with NUnit and the regression tests are ran by comparing the samples'
output with a reference output. You can run the *regression_tests.py* script
after having built the solution in Visual Studio to run the regression tests.

Because the regression tests just do a direct comparison with the output, it is
possible to get a false negative after having done a good change. In that case,
please update the tests so they match the output after your change. You can run
the *UpdateSamplesOutput.bat* and *UpdateSharpmakeProjects.bat* batch files to
automatically overwrite the reference output files.

Naturally, we also recommend that you put your own tests after fixing a bug or
adding a feature to help us avoid regressions.

### Additional Platforms
If you want to add support for an additional platform, please make sure that
the platform is open and that you are not breaking your NDA. Ubisoft has not
published platform support for most video game consoles for that exact reason.
We will not accept merge requests for new platforms that are not completely
open for developmemt.
