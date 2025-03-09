# Contributing

## Tests

We will only accept merge requests that pass all tests. The unit tests are written with NUnit and the regression tests are ran by comparing the samples' output with a reference output. You can run the `regression_tests.py` script after having built the solution in Visual Studio to run the regression tests.

Because the regression tests just do a direct comparison with the output, it is possible to get a false negative after having done a good change. In that case, please update the tests so they match the output after your change. You can run the `UpdateSamplesOutput.bat` batch files to automatically overwrite the reference output files.

Naturally, we also recommend that you put your own tests after fixing a bug or adding a feature to help us avoid regressions.

Functional tests are generating test projects and building them to test functionality.

## Additional Platforms

If you want to add support for an additional platform, please make sure that the platform is open and not tied to any NDA. Ubisoft has not published platform support for most video game consoles for that exact reason.

We will not accept merge requests for new platforms that are not completely open for development.
