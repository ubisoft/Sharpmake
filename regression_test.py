#!/usr/bin/env python

# Script that runs the regression tests on the output of Sharpmake. It does not
# take any argument.
#
# This script supports Python 3.

import os.path
import sys

class Test:
    def __init__(self, directory, script_name, project_root = ""):
        self.directory = directory
        self.assembly = directory + ".dll" # same name as the directory for now
        self.script_name = script_name
        if project_root == "":
            self.project_root = directory
        else:
            self.project_root = project_root

    def run_test(self, as_source):
        entry_path = os.getcwd()
        try:
            pwd = os.path.join(entry_path, "samples")
            os.chdir(pwd)

            # Detects the path of the Sharpmake executable
            sharpmake_path = find_target_path(self.directory, "Sharpmake.Application.exe")

            # Builds the command line argument list.
            sources = "/sources(@\"{}\")".format(os.path.join(self.directory, self.script_name))
            assemblies = "/assemblies(@\"{}\")".format(find_target_path(self.directory, self.assembly))
            referencedir = "/referencedir(@\"{}\")".format(os.path.join(self.directory, "reference"))
            outputdir = "/outputdir(@\"{}\")".format(os.path.join(self.directory, "projects"))
            remaproot = "/remaproot(@\"{}\")".format(self.project_root)
            test = "/test(@\"Regression\")"
            verbose = "/verbose"
            args = "\"{} {} {} {} {} {}\"".format(
                sources if as_source else assemblies,
                referencedir,
                outputdir,
                remaproot,
                test,
                verbose
            )

            cmd_line = "{} {}".format(sharpmake_path, args)
            return os.system(cmd_line)

        except:
            raise

        finally:
            os.chdir(entry_path)

tests = [
    Test("ConfigureOrder", "main.sharpmake.cs"),
    Test("CPPCLI", "CLRTest.sharpmake.cs"),
    Test("CSharpHelloWorld", "HelloWorld.sharpmake.cs"),
    Test("HelloWorld", "HelloWorld.sharpmake.cs"),
    Test("CSharpVsix", "CSharpVsix.sharpmake.cs"),
    Test("PackageReferences", "PackageReferences.sharpmake.cs"),
    Test("QTFileCustomBuild", "QTFileCustomBuild.sharpmake.cs"),
    Test("SharpmakeGen", "SharpmakeGen.sharpmake.cs", "..")
]

def find_target_path(directory, target):
    optim_tokens = ["Debug", "Release"]
    for optim_token in optim_tokens:
        path = os.path.abspath(os.path.join(directory, "bin", optim_token, target))
        if os.path.isfile(path):
            return path

    raise IOError("Cannot find " + target)

def write_line(str):
    print(str)
    sys.stdout.flush()

# Those are not cross-platform!
def red_bg():
    os.system("color 4F")

def green_bg():
    os.system("color 2F")

def pause(timeout=None):
    if timeout is None:
        os.system("pause")
    else:
        os.system("timeout /t 5")

def launch_tests():
    entry_path = os.getcwd()
    try:
        # Change directory to the path of this.
        pwd = os.path.dirname(os.path.realpath(__file__))
        os.chdir(pwd)

        # Run each test. Break and exit on error.
        for test in tests:
            write_line("Regression test in source mode on {}...".format(test.directory))
            exit_code = test.run_test(True)
            if exit_code != 0:
                red_bg()
                write_line("Test failed.")
                return exit_code

            write_line("Regression test in assembly mode on {}...".format(test.directory))
            exit_code = test.run_test(False)
            if exit_code != 0:
                red_bg()
                write_line("Test failed.")
                return exit_code

        green_bg()
        write_line("Regression tests succeeded.")
        pause(5)
        return 0

    finally:
        os.chdir(entry_path)

exit_code = launch_tests()
sys.exit(exit_code)
