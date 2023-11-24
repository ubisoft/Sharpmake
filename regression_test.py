#!/usr/bin/env python

# Script that runs the regression tests on the output of Sharpmake. It does not
# take any argument.
#
# This script supports Python 3.

import argparse
import os.path
import sys

if os.name != "nt":
    import select

class Test:
    def __init__(self, directory, script_name, project_root = ""):
        self.directory = directory
        self.script_name = script_name
        self.runs_on_unix = os.name == "posix"
        if project_root == "":
            self.project_root = "."
        else:
            self.project_root = project_root

    def run_test(self, sharpmake_path):
        root_directory = os.getcwd()
        try:
            sample_dir = os.path.join("samples", self.directory)
            os.chdir(sample_dir)

            # Builds the command line argument list.
            sources = "/sources(@\'{}\')".format(self.script_name)
            referencedir = "/referencedir(@\'{}\')".format("reference")
            outputdir = "/outputdir(@\'{}\')".format("projects")
            remaproot = "/remaproot(@\'{}\')".format(self.project_root)
            test = "/test(@\'Regression\')"
            verbose = "/verbose"

            args = [
                sources,
                referencedir,
                outputdir,
                remaproot,
                test,
                verbose
            ]

            cmd_line = "{} \"{}\"".format(sharpmake_path, " ".join(args))
            if self.runs_on_unix:
                cmd_line = "mono --debug " + cmd_line

            return os.system(cmd_line)

        except:
            raise

        finally:
            os.chdir(root_directory)

def find_sharpmake_path(root_directory, sharpmake_exe):
    if sharpmake_exe is not None:
        if not os.path.isfile(sharpmake_exe):
            raise IOError("Cannot find " + sharpmake_exe)

        if os.path.isabs(sharpmake_exe):
            return sharpmake_exe
        else:
            return os.path.abspath(sharpmake_exe)

    return find_file_in_path(root_directory, "Sharpmake.Application/bin", "Sharpmake.Application.exe")

def find_file_in_path(root_directory, directory, filename):
    optim_tokens = ["debug", "Debug", "release", "Release"]
    for optim_token in optim_tokens:
        dir_path = os.path.abspath(os.path.join(root_directory, directory, optim_token))
        for root, dirs, files in os.walk(dir_path):
            for framework_dir in dirs:
                path = os.path.join(dir_path, framework_dir, filename)
                if os.path.isfile(path):
                    return path

    raise IOError("Cannot find " + filename)

def write_line(str):
    print(str)
    sys.stdout.flush()

# Those are not cross-platform!
def red_bg():
    if os.name == "nt":
        os.system("color 4F")

def green_bg():
    if os.name == "nt":
        os.system("color 2F")

def black_bg():
    if os.name == "nt":
        os.system("color 0F")

def pause(timeout=None):
    if timeout is None:
        input("Press any key to continue . . .")
    else:
        timeoutSeconds = int(timeout) if int(timeout) > 0 else 5
        if os.name == "nt":
            os.system("timeout /t " + str(timeoutSeconds))
        else:
            stop_waiting = False
            for s in range(0, timeout):
                if stop_waiting:
                    break

                display = "Waiting for " + str(timeoutSeconds - s) + " seconds, press a key to continue ..."
                sys.stdout.write("\r" + display)
                sys.stdout.flush()

                poll_frequency = 100 # ms
                for ms in range(0, int(1000/poll_frequency)):
                    i,o,e = select.select([sys.stdin],[],[],poll_frequency/1000)
                    for s in i:
                        if s == sys.stdin:
                            sys.stdin.readline()
                            stop_waiting = True
                            break
                    if stop_waiting:
                        break

def launch_tests():
    entry_path = os.getcwd()
    try:
        parser = argparse.ArgumentParser()
        parser.add_argument("--sharpmake_exe")
        args = parser.parse_args()

        root_directory = os.path.dirname(os.path.realpath(__file__))

        # Detects the path of the Sharpmake executable
        sharpmake_path = find_sharpmake_path(root_directory, args.sharpmake_exe)
        write_line("Using sharpmake " + sharpmake_path)

        # Change directory to the path of this.
        os.chdir(root_directory)

        tests = [
            Test("ConfigureOrder", "main.sharpmake.cs"),
            Test("CPPCLI", "CLRTest.sharpmake.cs"),
            Test("CSharpHelloWorld", "HelloWorld.sharpmake.cs"),
            Test("HelloWorld", "HelloWorld.sharpmake.cs"),
            Test("JumboBuild", "JumboBuild.sharpmake.cs"),
            Test("HelloLinux", "HelloLinux.Main.sharpmake.cs"),
            Test("HelloAssembly", "HelloAssembly.sharpmake.cs"),
            Test("CSharpVsix", "CSharpVsix.sharpmake.cs"),
            Test("CSharpWCF", "CSharpWCF.sharpmake.cs"),
            Test("CSharpImports", "CSharpImports.sharpmake.cs"),
            Test("PackageReferences", "PackageReferences.sharpmake.cs"),
            #Test("QTFileCustomBuild", "QTFileCustomBuild.sharpmake.cs"), # commented out since output has discrepancies between net472 and net5.0
            Test("SimpleExeLibDependency", "SimpleExeLibDependency.sharpmake.cs"),
            Test("NetCore\\DotNetOSMultiFrameworksHelloWorld", "HelloWorld.sharpmake.cs"),
        ]

        # Run each test. Break and exit on error.
        for test in tests:
            write_line("Regression test on {}...".format(test.directory))
            exit_code = test.run_test(sharpmake_path)
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

if __name__ == "__main__":
    black_bg()
    exit_code = launch_tests()
    sys.exit(exit_code)
