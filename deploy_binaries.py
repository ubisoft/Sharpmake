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
                  dest="target_dir", default="deploy",
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
parser.add_option("-e", "--extra-dir",
                  dest="extra_directories", action="append",
                  help="Additional directories with binaries to deploy. Repeat for each directory to add.")
parser.add_option("-a", "--deploy-all",
                  dest="deploy_all", default=False, action="store_true",
                  help="Deploy all files that come with the binaries.")
(options, args) = parser.parse_args()

root_dir = os.path.abspath(options.root_dir)
target_dir = os.path.join(root_dir, options.target_dir)
deploy_pdb = options.deploy_pdb or options.deploy_all
deploy_xmldoc = options.deploy_xmldoc or options.deploy_all
config = options.config
extra_directories = options.extra_directories

# Validate the configuration.
if config.upper() == "RELEASE":
    config = "Release"
elif config.upper() == "DEBUG":
    config = "Debug"
else:
    print("Unknown configuration: {}".format(config))
    sys.exit(-1)

# Check if there are actual DLLs to copy, otherwise it must be compiled in VS.
if not os.path.isfile(os.path.join(root_dir, "tmp/bin/{}/sharpmake/Sharpmake.dll".format(config))):
    print("Please build Sharpmake in its {} configuration.".format(config))
    sys.exit(1)

# If the directory exists, make sure that it is empty.
if not os.path.isdir(target_dir):
    os.makedirs(target_dir)

################################################################################
def copy_file(src):
    if os.path.isfile(src) and os.path.normcase(os.path.normpath(os.path.dirname(src))) != os.path.normcase(os.path.normpath(target_dir)):
        print("Copying {} to {}".format(os.path.join(root_dir, src), target_dir))
        shutil.copy2(src, target_dir)

# Simple wrapper class that represents an output folder,
# ie: bin/Release or bin/Debug
class BinarySite:
    def __init__(self, name):
        self.name = name

    def copy(self):
        # Copy the DLL.
        dll_path = os.path.join(root_dir, "tmp", "bin", config, self.name, self.name + ".dll")
        copy_file(dll_path)
        copy_file(dll_path + ".config")

        # Copy the executable.
        exe_path = os.path.join(root_dir, "tmp", "bin", config, self.name, self.name + ".exe")
        copy_file(exe_path)
        copy_file(exe_path + ".config")

        # Copy the program debug database and mdb files.
        if deploy_pdb:
            copy_file(dll_path + ".mdb")
            copy_file(exe_path + ".mdb")
            copy_file(os.path.join(root_dir, "tmp", "bin", config, self.name, self.name + ".pdb"))

        # Copy the XML API doc if it exists.
        if deploy_xmldoc:
            copy_file(os.path.join(root_dir, "tmp", "bin", config, self.name, self.name + ".xml"))

    def __str__(self):
        return self.name

# The list of files to copy. We omit the extension because we want to try to
# copy more files than just the DLL.
copy_list = [
    BinarySite("Sharpmake"),
    BinarySite("Sharpmake.Application"),
    BinarySite("Sharpmake.Generators")
]

# Add the Extensions to the list of files to copy.
extensions_dir = os.path.join(root_dir, "Sharpmake.Extensions")
if os.path.isdir(extensions_dir):
    for extension_dir_name in os.listdir(extensions_dir):
        if os.path.isdir(os.path.join(extensions_dir, extension_dir_name)):
            site = BinarySite(extension_dir_name)
            copy_list.append(site)

# Add the platforms to the list of files to copy.
platforms_dir = os.path.join(root_dir, "Sharpmake.Platforms")
if os.path.isdir(platforms_dir):
    for platform_dir_name in os.listdir(platforms_dir):
        if os.path.isdir(os.path.join(platforms_dir, platform_dir_name)):
            site = BinarySite(platform_dir_name)
            copy_list.append(site)

# Add extra directories (if any) to the list of files to copy.
if extra_directories:
    for extra_dir in extra_directories:
        extra_dir = os.path.abspath(extra_dir)
        if os.path.isdir(extra_dir):
            for extra_dir_name in os.listdir(extra_dir):
                if os.path.isdir(os.path.join(extra_dir, extra_dir_name)):
                    site = BinarySite(extra_dir_name)
                    copy_list.append(site)

# Finally, do the copying.
for site in copy_list:
    site.copy()

# Also copy the interop allowing the detection of visual studio
vs_interop = os.path.join(root_dir, "tmp", "bin", config, "sharpmake", "Microsoft.VisualStudio.Setup.Configuration.Interop.dll")
if os.path.isfile(vs_interop):
    copy_file(vs_interop)

