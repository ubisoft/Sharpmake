#!/usr/bin/env python

# Script that runs the functional tests on the Sharpmake. It does not take any argument.
#
# This script supports Python 3.

import os.path
import sys

if os.name != "nt":
    import select

class FunctionalTest:
    def __init__(self, directory, script_name, project_root = ""):
        self.directory = directory
        self.script_name = script_name
        self.use_mono = os.name == "posix"
        if project_root == "":
            self.project_root = directory
        else:
            self.project_root = project_root

    def run_test(self):
        entry_path = os.getcwd()
        try:
            pwd = os.path.join(entry_path, "Sharpmake.FunctionalTests")
            os.chdir(pwd)

            # Detects the path of the Sharpmake executable
            sharpmake_path = find_target_path(os.path.join(entry_path, "bin", "release"), "Sharpmake.Application.exe")

            write_line("Using sharpmake " + sharpmake_path)

            # Builds the command line argument list.
            sources = "/sources(@\"{}\")".format(os.path.join(self.directory, self.script_name))
            verbose = "/verbose"

            args = [
                sources,
                verbose
            ]

            if self.use_mono:
                args_string = "\" \"".join([arg.replace('"','\\"') for arg in args])
                cmd_line = "mono --debug {} \"{}\"".format(sharpmake_path, args_string)
            else:
                cmd_line = "{} \"{}\"".format(sharpmake_path, " ".join(args))

            generation_exit_code = os.system(cmd_line)

            if generation_exit_code != 0:
                return generation_exit_code
            
            os.chdir(entry_path)
            return self.build(pwd)

        except:
            raise

        finally:
            os.chdir(entry_path)

    def build(self, projectDir):
        print("implement build method")
        return -1


class FastBuildFunctionalTest(FunctionalTest):

    def build(self, projectDir):
        entry_path = os.getcwd()
        fastBuildPath = os.path.join(entry_path, "tools", "FastBuild", "FBuild.exe");
        if not os.path.isfile(fastBuildPath):
            return -1
        
        os.chdir(os.path.join(projectDir, self.directory, "projects"))

        return os.system(fastBuildPath + " All-Configs -vs -summary -verbose -config " + self.directory + ".bff")

class MSBuildFunctionalTest(FunctionalTest):

    def build(self, projectDir):
        entry_path = os.getcwd()
        os.chdir(os.path.join(projectDir, self.directory, "projects"))

        return os.system("msbuild " + self.directory + ".sln")

class CustomBuildFunctionalTest(FunctionalTest):

    def build(self, projectDir):
        entry_path = os.getcwd()
        os.environ["tools"] = os.path.join(entry_path, "tools")
        os.chdir(os.path.join(projectDir, self.directory))

        return os.system("build.bat");

funcTests = [
    FastBuildFunctionalTest("FastBuildFunctionalTest", "FastBuildFunctionalTest.sharpmake.cs")
]

def find_target_path(directory, target):
    path = os.path.abspath(os.path.join(directory, target))
    if os.path.isfile(path):
        return path

    raise IOError("Cannot find " + target)

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

def launch_functional_tests():
    entry_path = os.getcwd()
    try:
        # Change directory to the path of this.
        pwd = os.path.dirname(os.path.realpath(__file__))
        os.chdir(pwd)

        # Run each test. Break and exit on error.
        for test in funcTests:
            write_line("Functional test on {}...".format(test.directory))
            exit_code = test.run_test()
            if exit_code != 0:
                red_bg()
                write_line("Test failed.")
                return exit_code

        green_bg()
        write_line("Functional tests succeeded.")
        pause(5)
        return 0

    finally:
        os.chdir(entry_path)

exit_code = launch_functional_tests()
sys.exit(exit_code)
