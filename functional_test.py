#!/usr/bin/env python

# Script that runs the functional tests on the Sharpmake. It does not take any argument.
#
# This script supports Python 3.

import argparse
import os.path
import platform
import sys

if os.name != "nt":
    import select


class FunctionalTest(object):
    def __init__(self, directory, script_name, extra_args = [], project_root = ""):
        self.directory = directory
        self.script_name = script_name
        self.runs_on_unix = os.name == "posix"
        self.extra_args = extra_args
        if project_root == "":
            self.project_root = directory
        else:
            self.project_root = project_root

    def run_test(self, sharpmake_path):
        entry_path = os.getcwd()
        try:
            pwd = os.path.join(entry_path, "Sharpmake.FunctionalTests")
            os.chdir(pwd)

            # Builds the command line argument list.
            sources = "/sources(@\'{}\')".format(os.path.join(self.directory, self.script_name))
            verbose = "/verbose"

            args = [
                sources,
                verbose
            ]
            args.extend(self.extra_args)

            cmd_line = "{} \"{}\"".format(sharpmake_path, " ".join(args))
            if self.runs_on_unix:
                cmd_line = "mono --debug " + cmd_line

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
    def __init__(self, enable_multi_stamping):
        extra_args = []
        if enable_multi_stamping:
            extra_args.append("/enableLinkerMultiStamp(true)")
        super(FastBuildFunctionalTest, self).__init__("FastBuildFunctionalTest", "FastBuildFunctionalTest.sharpmake.cs", extra_args)

    def verifyCustomBuildEventsInTargetDir(self, targetDir):
        #verify copied files exist
        expected_copied_files = ["dummyfile_to_be_copied_to_buildoutput.txt", "main.cpp", "postbuildcopysinglefiletest.exe", "explicitlyorderedpostbuildtest.exe", "explicitlyorderedpostbuildtest.pdb"]
        for expected_file in expected_copied_files:
            expected_file = os.path.join(targetDir, "file_copy_destination", expected_file)
            if not os.path.isfile(expected_file):
                write_line("Expected file does not exist: {}...".format(expected_file))
                return 1

        #verify test execution created the correct output
        test_output = os.path.join(targetDir, "test_execution_output.txt")
        f = open(test_output, "r")
        if f.mode != "r":
            write_line("Unable to open file {}...".format(test_output))
            return 1

        file_content = f.read()
        if file_content != "Test successful.":
            write_line("Incorrect output of test node execution: {}...".format(file_content))
            return 1

        #verify post build stamper modified the executable correctly
        test_output = os.path.join(targetDir, "postbuildstamptest.exe")
        f = open(test_output, "rb")
        if f.mode != "rb":
            write_line("Unable to open file {}...".format(test_output))
            return 1

        file_content = f.read()
        if not file_content.endswith(b"_Stamp_Message"):
            write_line("Incorrect stamping for file {}...".format(test_output))
            return 1

        return 0

    def verifyCustomBuildEvents(self, projectDir):
        output_dir = os.path.join(projectDir, self.directory, "projects", "output")
        target_dirs = ["debug_fastbuild_noblob_vs2022", "debug_fastbuild_vs2022", "release_fastbuild_noblob_vs2022", "release_fastbuild_vs2022"]

        for target_dir in target_dirs:
            target_dir = os.path.join(output_dir, target_dir)

            verifyResult = self.verifyCustomBuildEventsInTargetDir(target_dir)
            if verifyResult != 0:
                return verifyResult

        return 0

    def build(self, projectDir):
        build_result = build_with_fastbuild(projectDir, self.directory)
        if build_result != 0:
            return build_result

        return self.verifyCustomBuildEvents(projectDir)


class NoAllFastBuildProjectFunctionalTest(FunctionalTest):
    def __init__(self):
        super(NoAllFastBuildProjectFunctionalTest, self).__init__("NoAllFastBuildProjectFunctionalTest", "NoAllFastBuildProjectFunctionalTest.sharpmake.cs")

    def build(self, projectDir):
        return build_with_fastbuild(projectDir, self.directory)

class OnlyNeededFastBuildTest(FunctionalTest):
    def __init__(self):
        super(OnlyNeededFastBuildTest, self).__init__("OnlyNeededFastBuildTest", "OnlyNeededFastBuildTest.sharpmake.cs")

    def build(self, projectDir):
        return build_with_fastbuild(projectDir, self.directory)

class SharpmakePackageFunctionalTest(FunctionalTest):
    def __init__(self):
        super(SharpmakePackageFunctionalTest, self).__init__("SharpmakePackageFunctionalTest", "SharpmakePackageFunctionalTest.sharpmake.cs", ["/generateDebugSolution"])

    def build(self, projectDir):
        return 0



def build_with_fastbuild(root_dir, test_dir):
    entry_path = os.getcwd()
    platformSystem = platform.system()
    fastBuildInfo = ("Linux-x64", "fbuild") if platformSystem == "Linux" else ("OSX-x64", "FBuild") if platformSystem == "Darwin" else ("Windows-x64", "FBuild.exe")
    fastBuildPath = os.path.join(entry_path, "tools", "FastBuild", fastBuildInfo[0], fastBuildInfo[1]);
    if not os.path.isfile(fastBuildPath):
        return -1

    working_dir = os.path.join(root_dir, test_dir, "projects")
    fdbFile = os.path.join(working_dir, test_dir + ".windows.fdb")
    if os.path.isfile(fdbFile):
        os.remove(fdbFile)
    cmd_line = fastBuildPath + " All-Configs -monitor -nosummaryonerror -clean -config " + test_dir + ".bff"

    os.chdir(working_dir)
    write_line(cmd_line)
    write_line("Working dir: " + working_dir)
    return os.system(cmd_line)

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

def launch_functional_tests(funcTests, sharpmake_exe):
    entry_path = os.getcwd()
    try:
        root_directory = os.path.dirname(os.path.realpath(__file__))
        write_line("root_directory: " + root_directory)

        # Detects the path of the Sharpmake executable
        sharpmake_path = find_sharpmake_path(root_directory, sharpmake_exe)
        write_line("Using sharpmake " + sharpmake_path)

        # Change directory to the path of this.
        os.chdir(root_directory)

        # Run each test. Break and exit on error.
        for test in funcTests:
            write_line("Functional test on {}...".format(test.directory))
            exit_code = test.run_test(sharpmake_path)
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

if __name__ == "__main__":

    parser = argparse.ArgumentParser()
    parser.add_argument("--enable_multi_stamping", action='store_true')
    parser.add_argument("--sharpmake_exe")
    args = parser.parse_args()

    funcTests = [
        FastBuildFunctionalTest(args.enable_multi_stamping),
        NoAllFastBuildProjectFunctionalTest(),
        OnlyNeededFastBuildTest(),
        SharpmakePackageFunctionalTest()
    ]

    black_bg()
    exit_code = launch_functional_tests(funcTests, args.sharpmake_exe)
    sys.exit(exit_code)
