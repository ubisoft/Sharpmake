#!/usr/bin/env python

# Python script that grabs all the binaries produced when building the
# Sharpmake solution and copies them in a directory. (By default, in a
# directory named /deploy.) Without that script, you need to manually copy
# and paste the binaries of every platform implementation assembly you have
# built.
#
# You typically use it like this:
#     py deploy-binaries.py --config <Release or Debug> --target-dir=<Where to copy the binaries>
#
# Please use the -h or --help options for more info about the command line
# arguments that this script accepts.
#
# This script supports Python 3.

import os.path
import shutil
import sys
from optparse import OptionParser

# Parses the command line options.
parser = OptionParser()
parser.add_option("-r", "--root-dir",
                  dest="root_dir", default=os.getcwd(),
                  help="The root path of the Sharpmake source code.",
                  metavar="DIR")
parser.add_option("-t", "--target-dir",
                  dest="target_dir", default="Binaries",
                  help="The directory where to deploy the files.",
                  metavar="DIR")
parser.add_option("-c", "--configuration",
                  dest="config", default="release",
                  help="Select the configuration to deploy to. (Debug or Release) The default is Release.",
                  metavar="CONFIG")
parser.add_option("-d", "--deploy-pdb",
                  dest="deploy_pdb", default=False, action="store_true",
                  help="Deploy program debug database files (.PDB) along with the binaries.")
parser.add_option("-x", "--deploy-xmldoc",
                  dest="deploy_xmldoc", default=False, action="store_true",
                  help="Deploy XML API documentation along with the binaries.")
parser.add_option("-a", "--deploy-all",
                  dest="deploy_all", default=False, action="store_true",
                  help="Deploy all files that come with the binaries.")
(options, args) = parser.parse_args()

root_dir = options.root_dir
target_dir = os.path.join(root_dir, options.target_dir)
deploy_pdb = options.deploy_pdb or options.deploy_all
deploy_xmldoc = options.deploy_xmldoc or options.deploy_all
config = options.config

# Validate the configuration.
if config.upper() == "RELEASE":
    config = "Release"
elif config.upper() == "DEBUG":
    config = "Debug"
else:
    print("Unknown configuration: {}".format(config))
    sys.exit(-1)

# Check if there are actual DLLs to copy, otherwise it must be compiled in VS.
if not os.path.isfile(os.path.join(root_dir, "Sharpmake/bin/{}/Sharpmake.dll".format(config))):
    print("Please build Sharpmake in it's {} configuration.".format(config))
    sys.exit(1)

# If the directory exists, make sure that it is empty.
if not os.path.isdir(target_dir):
    os.mkdir(target_dir)

# Simple wrapper class that represents an output folder,
# ie: Sharpmake/bin/Release
class BinarySite:
    def __init__(self, name, path):
        self.name = name
        self.path = os.path.join(root_dir, path)

    def copy_file(self, src):
        if os.path.isfile(src):
            print("Copying {} to {}".format(os.path.join(root_dir, src), target_dir))
            shutil.copy2(src, target_dir)

    def copy(self):
        # Copy the DLL.
        dll_path = os.path.join(self.path, "bin", config, self.name + ".dll")
        if os.path.isfile(dll_path):
            self.copy_file(dll_path)

        # Copy the executable.
        exe_path = os.path.join(self.path, "bin", config, self.name + ".exe")
        if os.path.isfile(exe_path):
            self.copy_file(exe_path)

        # Copy the program debug database if it exists.
        if deploy_pdb:
            self.copy_file(os.path.join(self.path, "bin", config, self.name + ".pdb"))

        # Copy the XML API doc if it exists.
        if deploy_xmldoc:
            self.copy_file(os.path.join(self.path, "bin", config, self.name + ".xml"))

    def __str__(self):
        return "{} ({})".format(self.name, self.path)

# The list of files to copy. We omit the extension because we want to try to
# copy more files than just the DLL.
copy_list = [
    BinarySite("Sharpmake", "Sharpmake"),
    BinarySite("Sharpmake.Application", "Sharpmake.Application"),
    BinarySite("Sharpmake.Generators", "Sharpmake.Generators"),
    BinarySite("Sharpmake.NuGet", "Sharpmake.NuGet")
]

# Add the platforms to the list of files to copy.
if os.path.isdir("Sharpmake.Platforms"):
    for platform_dir in os.listdir("Sharpmake.Platforms"):
        name = platform_dir
        path = os.path.join("Sharpmake.Platforms", platform_dir)
        site = BinarySite(name, path)
        copy_list.append(site)

# Finally, do the copying.
for site in copy_list:
    site.copy()
