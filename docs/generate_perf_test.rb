#!/usr/bin/env ruby

# ----------------------------------------------------------------------------
# This ruby script generates fake C++ source code and build scripts for
# testing and comparing the performance of various "make" generators when a
# very large number of configurations is required, which is one of Sharpmake's
# strengths. It currently generates build scripts for CMake, Premake and
# Sharpmake.
#
# We tried to be as fair as possible but we are not specialists for Premake
# and Sharpmake. If you have a solution that would improve their performance,
# you are very welcome to propose a change to this script, and we will update
# the reported numbers.
#
# Simply run the script by calling `ruby generate_perf_test.rb` and it will
# generate a lot of code folders for the stub code, plus the following 3
# folders for the generators:
#   /cmake
#   /premake
#   /sharpmake
#
# You can then try to generate the project with all its configurations with
# the generator of your choice.
#
# The stub C++ code generated is designed to (grossly) ressemble the structure
# of the code base for a typical AAA video game.
# ----------------------------------------------------------------------------


# To get reasonable times running the tools, you may want to change those
# global variables to adjust the amount of work needed. This will modify how
# many files and projects will be generated for the tests.
$num_files_modifier = 0.5
$num_projects_modifier = 0.5

require 'securerandom'
require 'stringio'

$next_id = 0

# Generates some unique name. Just use sequential numbers for the unique part,
# should still be unique.
def generate_name(prefix, extension = nil)
  unless extension.nil? || extension.empty?
    extension.prepend('.') unless extension[0] == '.'
  end
  name = "#{prefix}#{$next_id}#{extension}"
  $next_id += 1
  return name
end

# Recursively create a directory.
def make_dir_path(*path_seq)
  path_seq = path_seq.collect!{|dir| dir.to_s}
  dir_path = ''
  path_seq.each do |dir|
    dir_path = dir_path.empty? ? dir : File.join(dir_path, dir)
    Dir.mkdir(dir_path) unless File.directory?(dir_path)
  end
  dir_path
end


# Represents a project. As far as this script is concerned, a project is just
# a group of source files compiled together. (Careful, this is called a target
# in CMake.)
class Project
  attr_reader :name
  attr_reader :path
  attr_reader :project_group
  attr_reader :files
  attr_reader :dependencies
  def initialize(name, path, num_files, project_group)
    if $quick_generation
      num_files = 1
    end

    @name = name
    @path = path
    @num_files = num_files
    @project_group = project_group
    @dependencies = []
  end

  def generated?
    !@files
  end

  def generate!
    Dir.mkdir(@path) unless File.directory?(@path)
    @files = @num_files.times.collect do
      file_name = generate_name('file') + '.cpp'
      file_path = File.join(path, file_name)
      File.write(file_path, '// generated') unless File.exists?(file_path)
      file_path
    end
  end

  def each_file(&block)
    @files.each {|file| yield file}
  end

  def to_s
    @name
  end

  def generate_dependencies!(projects)
    num_dependencies = @dependencies.length
    projects.each_with_index do |dep, index|
      num_remaining = num_dependencies - index
      sample_size = num_dependencies / 4
      @dependencies = projects.drop(index + 1).sample(sample_size)
    end
  end
end


# Represents a group of projects, aka a "solution" in Visual Studio, a
# "workspace" in Premake and a "project" (sigh...) in CMake.
class ProjectGroup
  attr_reader :name
  attr_reader :projects
  def initialize(name, num_projects, num_files_per_project)
    @name = name
    @num_projects = [1, (num_projects * $num_projects_modifier || 1).to_i].max
    @num_files_per_project = [1, (num_files_per_project * $num_files_modifier || 1).to_i].max

    @projects = @num_projects.times.collect do
      num_files = @num_files_per_project
      project_name = generate_name('project')
      project_path = File.join(@name, project_name)
      Project.new(project_name, project_path, num_files, self)
    end
  end

  def generated?
    !@projects
  end

  def generate!
    Dir.mkdir(@name) unless File.directory?(@name)
    @projects.each do |project|
      project.generate!
      project.generate_dependencies!(@projects)
    end
  end

  def each_project(&block)
    @projects.each {|project| yield project}
  end

  def count
    @projects.count
  end

  def size
    count
  end

  def to_s
    @name
  end
end


# Generates a Sharpmake generation script that can build the generated
# projects and solutions.
class SharpmakeGenerator
  def initialize(project_groups)
    @project_groups = project_groups
  end

  def generate!
    init_sharpmake_dir
    @project_groups.each {|group| generate_solution(group)}
    generate_master_solution(@project_groups)
    generate_batch_file
  end

  def to_s
    'Sharpmake Generator'
  end

private
  def generate_batch_file
    File.open('run-sharpmake.bat', 'w') do |f|
      f.puts '@echo off'
      f.puts 'Sharpmake.Application.exe /sources(@"sharpmake\\main.sharpmake.cs")'
    end
  end

  def init_sharpmake_dir
    Dir.mkdir('sharpmake') unless File.directory?('sharpmake')
    sharpmake_files = StringIO.new
    generate_solutions_code = StringIO.new
    @project_groups.each do |group|
      sharpmake_files.puts("[module: Sharpmake.Include(\"#{group}.sharpmake.cs\")]")
      generate_solutions_code.puts("        args.Generate<Solution_#{group.name.capitalize}>();")
    end
    File.open(File.join('sharpmake', 'main.sharpmake.cs'), 'w') do |f|
      f.puts "
using System;
using System.IO;
using Sharpmake;

[module: Sharpmake.Reference(\"Sharpmake.Durango.dll\")]
[module: Sharpmake.Include(\"allprojects.sharpmake.cs\")]
#{sharpmake_files.string}

[Fragment, Flags]
public enum BuildTypes
{
    Debug =         0x01,
    Release =       0x02,
    Retail =        0x04,
    QC =            0x08,
    Profile =       0x10
}

[Fragment, Flags]
public enum Grouping
{
    Individual =    0x01,
    All =           0x02
}

[Fragment, Flags]
public enum RenderingApi
{
    DirectX11 = 0x01,
    DirectX12 = 0x02,
    OpenGL4_x = 0x08,
    Vulkan = 0x10
}

public class Target : ITarget
{
    public Platform Platform;
    public DevEnv DevEnv;
    public BuildTypes BuildType;
    public OutputType LibraryOutputType;
    public Grouping PlatformGrouping;
    public RenderingApi RenderingApi;

    public Target() { }
    public Target(Platform platform, DevEnv devEnv, BuildTypes buildType, OutputType libraryOutputType, Grouping platformGrouping, RenderingApi renderingApi)
    {
        Platform = platform;
        DevEnv = devEnv;
        BuildType = buildType;
        LibraryOutputType = libraryOutputType;
        PlatformGrouping = platformGrouping;
        RenderingApi = renderingApi;
    }
}

public static class Common
{
    public static ITarget[] GetTargets()
    {
        return new[]
        {
            new Target(
                Platform.win32 | Platform.win64 | Platform.durango,
                /*DevEnv.vs2015 | */DevEnv.vs2017,
                (BuildTypes)0x1F,
                OutputType.Lib | OutputType.Dll,
                Grouping.Individual | Grouping.All,
                RenderingApi.DirectX11 | RenderingApi.DirectX12 | RenderingApi.OpenGL4_x | RenderingApi.Vulkan)
        };
    }

    [Main]
    public static void SharpmakeMain(Arguments args)
    {
        args.Generate<Solution_AllProjects>();
#{generate_solutions_code.string}
    }
}"
    end
  end

  def generate_master_solution(solutions)
    File.open(File.join('sharpmake', 'allprojects.sharpmake.cs'), 'w') do |f|
      f.puts "
using Sharpmake;

[Generate]
public class Solution_AllProjects : Solution
{
    public Solution_AllProjects()
        : base(typeof(Target))
    {
        Name = \"AllProjects\";
        AddTargets(Common.GetTargets());
    }

    [Configure]
    public void Configure(Configuration conf, Target target)
    {
        string platformName = target.PlatformGrouping == Grouping.All ? \"all\" : target.Platform.ToString();
        conf.SolutionFileName = @\"[solution.Name]_[target.DevEnv]_\" + platformName;
        conf.SolutionPath = @\"[solution.SharpmakeCsPath]/generated\";
#{solutions.collect {|solution| solution.projects.collect{|project| "        conf.AddProject<#{project.to_s.capitalize}>(target);"}}.flatten.join("\n")}
    }
}"
    end
  end

  def generate_solution(solution)
    File.open(File.join('sharpmake', "#{solution.to_s}.sharpmake.cs"), 'w') do |f|
      f.puts "
using System;
using System.IO;
using Sharpmake;

#{solution.projects.collect {|project| generate_project(solution, project)}.join("\n")}

[Generate]
public class Solution_#{solution.name.capitalize} : Solution
{
    public Solution_#{solution.name.capitalize}()
        : base(typeof(Target))
    {
        Name = \"#{solution.name}\";
        AddTargets(Common.GetTargets());
    }

    [Configure]
    public void Configure(Configuration conf, Target target)
    {
        conf.Name = \"[target.BuildType]_[target.LibraryOutputType]_[target.RenderingApi]\";
        string platformName = target.PlatformGrouping == Grouping.All ? \"all\" : target.Platform.ToString();
        conf.SolutionFileName = @\"[solution.Name]_[target.DevEnv]_\" + platformName;
        conf.SolutionPath = @\"[solution.SharpmakeCsPath]/generated\";
#{solution.projects.collect {|project| "        conf.AddProject<#{project.to_s.capitalize}>(target);"}.join("\n")}
    }
}"
    end
  end

  def generate_project(solution, project)
    ss = StringIO.new
    ss.puts "
[Generate]
public class #{project.to_s.capitalize} : Project
{
    public #{project.to_s.capitalize}()
        : base(typeof(Target))
    {
        Name = \"#{project.to_s}\";
        SourceRootPath = @\"[project.SharpmakeCsPath]/../#{File.join(solution.to_s, project.to_s)}\";
        AddTargets(Common.GetTargets());
    }

    [Configure]
    public void Configure(Configuration conf, Target target)
    {
        conf.Name = \"[target.BuildType]_[target.LibraryOutputType]_[target.RenderingApi]\";
        string platformName = target.PlatformGrouping == Grouping.All ? \"all\" : target.Platform.ToString();
        conf.ProjectFileName = \"[project.Name]_[target.DevEnv]_\" + platformName;
        conf.ProjectPath = \"[project.SharpmakeCsPath]/generated/[project.Name]/[target.DevEnv]/\" + platformName;
        conf.Output = Configuration.OutputType.Lib;
"
    project.dependencies.each do |dep|
      ss.puts "        conf.AddPublicDependency<#{dep.to_s.capitalize}>(target);"
    end
    ss.puts "    }
}"
    ss.string
  end
end


# Generate a premake script that can build the generated files.
class PremakeGenerator
  PLATFORMS = [
    :win32,
    :win64,
    :durango]
  BUILD_TYPES = [
    :debug,
    :release,
    :retail,
    :qc,
    :profile]
  RENDERING_API = [
    :directx11,
    :directx12,
    :opengl4,
    :vulkan]
  OUTPUT_TYPES = [
    :static,
    :shared
  ]
  CONFIGURATIONS = BUILD_TYPES.product(
    RENDERING_API,
    OUTPUT_TYPES
  )
  def initialize(project_groups)
    @workspaces = project_groups
  end

  def generate!
    init_premake_dir
    generate_workspaces
    generate_batch_file
  end

  def to_s
    'Premake Generator'
  end

  private
  def init_premake_dir
    Dir.mkdir('premake') unless File.directory?('premake')
  end

  def generate_workspaces
    # Due to Premake's RAM consumption, we cannot make it generate the whole
    # thing unless we get a 64-bit build. So we split it into multiple files,
    # one per solution.
    @workspaces.each do |workspace|
      content = generate_workspace(workspace)
      script = "#{workspace.name}-premake5.lua"
      File.write(File.join('premake', script), content)
    end
  end

  def generate_batch_file
    # Generate a batch file that will run premake for each of these solutions.
    File.open('run-premake.bat', 'w') do |ss|
      ss.puts '@echo off'
      @workspaces.each do |workspace|
        premake_file = File.join('premake', workspace.name + '-premake5.lua')
        ss.puts "echo Generate #{workspace.name}..."
        ss.puts "premake5 --file=premake/#{workspace.name}-premake5.lua vs2017"
      end
    end
  end

  def generate_workspace(workspace)
    ss = StringIO.new

    # Declare each project
    workspace.each_project do |project|
      (PLATFORMS + [:all]).each do |platform|
        # Define the solution's configurations
        ss.puts "workspace \"#{workspace.name}_#{platform}\""
        ss.puts "    configurations {"
        ss.puts CONFIGURATIONS.collect {|conf| "        \"#{conf.join('_')}\"" }.join(",\n")
        ss.puts "    }"
        ss.puts
        if platform == :all
          ss.puts "    platforms { #{PLATFORMS.collect {|platform| "\"#{platform}\""}.join(', ')  } }"
        else
          ss.puts "    platforms { \"#{platform}\" }"
        end
        ss.puts
        ss.puts generate_project(project, platform)
      end
    end

    ss.string
  end

  def generate_project(project, platform_name)
    ss = StringIO.new

    ss.puts generate_project_general(project, platform_name)
    ss.puts generate_build_type_filter

    ss.puts
    ss.string
  end

  def generate_project_general(project, platform_name)
    ss = StringIO.new
    ss.puts "project \"#{project.name}_#{platform_name}\""
    ss.puts '    language "C++"'
    ss.puts '    targetdir "bin/%{cfg.buildcfg}"'
    ss.puts '    files {'
    ss.puts "        \"../#{project.path}/**.hpp\","
    ss.puts "        \"../#{project.path}/**.cpp\""
    ss.puts '    }'
    ss.puts
    ss.puts '    filter "configurations:*static*"'
    ss.puts '        kind "StaticLib"'
    ss.puts
    ss.puts '    filter "configurations:*shared*"'
    ss.puts '        kind "SharedLib"'
    ss.puts
    ss.string
  end

  def generate_build_type_filter
    ss = StringIO.new
    ss.puts "    filter \"configurations:*debug*\""
    ss.puts '        defines { "DEBUG", "_DEBUG" }'
    ss.puts '        symbols "On"'
    ss.puts '        optimize "Off"'
    ss.puts
    ss.puts "    filter \"configurations:not *debug*\""
    ss.puts '        defines { "NDEBUG" }'
    ss.puts '        optimize "On"'
    ss.puts
    ss.string
  end

  def platform_name(platform)
    case platform
    when :win32
      'Win32'
    when :win64
      'Win64'
    when :durango
      'XboxOne'
    else
      raise 'What platform???'
    end
  end

  def architecture_name(platform)
    case platform
    when :win32
      'x32'
    when :win64, :durango
      'x64'
    else
      raise 'What platform???'
    end
  end
end


# Generates projects and solutions for a CMake project. This will output a
# generic CMake script configurable via the command line (or cmake-gui) and a
# batch file that will run it against the large number of configurations we
# need.
#
# While this may be unfair against Premake and Sharpmake that generate a lot of
# configurations with a single script, alternatives are limited. CMake's
# support of Visual Studio configurations is very limited. With CMake, you
# typically set the configuration *before* generating the projects by using
# cmake-gui (or the command line.)
class CMakeGenerator
  def initialize(project_groups)
    # Generate the projects. The terminology is annoyingly confusing because
    # what we call a workspace or a solution is called a Project in CMake, and
    # what we refer to as a project is called a Target. I think this is GNU
    # Make terminology but whatever. It's confusing. Will just call them
    # project groups here, since it's basically what a solution is.
    @project_groups = project_groups
  end

  def generate!
    install_xdk_module
    generate_cmake
    generate_batch_file
  end

  def to_s
    "CMake Generator"
  end

  private
  def install_xdk_module
    file_content = <<CONTENT
# Source: Autodesk Stingray
# https://raw.githubusercontent.com/AutodeskGames/stingray-plugin/master/cmake/Toolchain-XBoxOne.cmake


# This module is shared; use include blocker.
if( _XB1_TOOLCHAIN_ )
  return()
endif()
set(_XB1_TOOLCHAIN_ 1)

# XB1 XDK version requirement
set(REQUIRED_XB1_TOOLCHAIN_VERSION "160305")

# Get XDK environment
if( EXISTS "$ENV{DurangoXDK}" AND IS_DIRECTORY "$ENV{DurangoXDK}" )
  string(REGEX REPLACE "\\\\" "/" XDK_ROOT $ENV{DurangoXDK})
  string(REGEX REPLACE "//" "/" XDK_ROOT ${XDK_ROOT})
endif()

# Fail if XDK not found
if( NOT XDK_ROOT )
  if( PLATFORM_TOOLCHAIN_ENVIRONMENT_ONLY )
    return()
  endif()
  message(FATAL_ERROR "Engine requires XB1 XDK to be installed in order to build XB1 platform.")
endif()

# Get toolchain version
get_filename_component(XDK_TOOLCHAIN_VERSION "[HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Durango XDK\\${REQUIRED_XB1_TOOLCHAIN_VERSION};EditionVersion]" NAME)

if( XDK_TOOLCHAIN_VERSION STREQUAL REQUIRED_XB1_TOOLCHAIN_VERSION )
  message(STATUS "Found required XDK toolchain version (${XDK_TOOLCHAIN_VERSION})")
else()
  get_filename_component(XDK_TOOLCHAIN_VERSION "[HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Durango XDK;Latest]" NAME)
  message(WARNING "Could not find required XDK toolchain version (${REQUIRED_XB1_TOOLCHAIN_VERSION}), using latest version instead (${XDK_TOOLCHAIN_VERSION})")
endif()

# If we only want the environment values, exit now
if( PLATFORM_TOOLCHAIN_ENVIRONMENT_ONLY )
  return()
endif()

# Find XDK compiler directory
if( CMAKE_GENERATOR STREQUAL "Visual Studio 11 2012" )
  set(XDK_COMPILER_DIR "${XDK_ROOT}/${XDK_TOOLCHAIN_VERSION}/Compilers/dev11.1")
elseif( CMAKE_GENERATOR STREQUAL "Visual Studio 14 2015" )
  get_filename_component(XDK_COMPILER_DIR "[HKEY_CURRENT_USER\\Software\\Microsoft\\VisualStudio\\14.0_Config\\Setup\\VC;ProductDir]" DIRECTORY)
  if( DEFINED XDK_COMPILER_DIR )
    string(REGEX REPLACE "\\\\" "/" XDK_COMPILER_DIR ${XDK_COMPILER_DIR})
    string(REGEX REPLACE "//" "/" XDK_COMPILER_DIR ${XDK_COMPILER_DIR})
  endif()
  if( NOT XDK_COMPILER_DIR )
    message(FATAL_ERROR "Can't find Visual Studio 2015 installation path.")
  endif()
else()
  message(FATAL_ERROR "Unsupported Visual Studio version!")
endif()

# Tell CMake we are cross-compiling to XBoxOne (Durango)
set(CMAKE_SYSTEM_NAME Durango)
set(XBOXONE True)

# Set CMake system root search path
set(CMAKE_SYSROOT "${XDK_COMPILER_DIR}")

# Set the compilers to the ones found in XboxOne XDK directory
set(CMAKE_C_COMPILER "${XDK_COMPILER_DIR}/vc/bin/amd64/cl.exe")
set(CMAKE_CXX_COMPILER "${XDK_COMPILER_DIR}/vc/bin/amd64/cl.exe")
set(CMAKE_ASM_COMPILER "${XDK_COMPILER_DIR}/vc/bin/amd64/ml64.exe")

# Force compilers to skip detecting compiler ABI info and compile features
set(CMAKE_C_COMPILER_FORCED True)
set(CMAKE_CXX_COMPILER_FORCED True)
set(CMAKE_ASM_COMPILER_FORCED True)

# Only search the XBoxOne XDK, not the remainder of the host file system
set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_PACKAGE ONLY)

# Global variables
set(XBOXONE_SDK_REFERENCES "Xbox Services API, Version=8.0;Xbox GameChat API, Version=8.0")
CONTENT

    make_dir_path('cmake', 'modules')
    File.write(File.join('cmake', 'modules', 'Toolchain-XboxOne.cmake'), file_content)
  end

  def generate_cmake
    ss = StringIO.new
    ss.puts 'CMAKE_MINIMUM_REQUIRED(VERSION 3.7)'
    ss.puts

    # If we need a module we get it there.
    ss.puts 'SET(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} ${CMAKE_SOURCE_DIR}/modules)'
    ss.puts

    # The properties that we want to configure from the command line.
    ss.puts generate_value_choice('CONFIG_PLATFORM', 'The platform to generate files for.',
      'WIN32',
      'WIN64',
      'XBOXONE')

    # Building libs as shared or static libs?
    ss.puts generate_value_choice('CONFIG_LIBRARY_KIND', 'The kind of libraries to generate.',
      'STATIC',
      'SHARED')

    # The Rendering API to use for building the project.
    ss.puts generate_value_choice('CONFIG_RENDERING_API', 'The rendering API/tech to use.',
      'DIRECTX11',
      'DIRECTX12',
      'OPENGL4',
      'VULKAN')

    # Create a macro for the boilerplace so that the script does not have 50k
    # LOC.
    ss.puts 'MACRO(SET_COMPILER_OPTIONS)'
    # Build types are more or less the debug/release switch in VS, so add the
    # other ones we need and copy the flags between them.
    ss.puts '  SET(CMAKE_CONFIGURATION_TYPES "DEBUG;RELEASE;RETAIL;QC;PROFILE")'
    ss.puts '  SET(CMAKE_CXX_FLAGS_RETAIL ${CMAKE_CXX_FLAGS})'
    ss.puts '  SET(CMAKE_CXX_FLAGS_QC ${CMAKE_CXX_FLAGS})'
    ss.puts '  SET(CMAKE_CXX_FLAGS_PROFILE ${CMAKE_CXX_FLAGS})'
    ss.puts '  SET(CMAKE_C_FLAGS_RETAIL ${CMAKE_C_FLAGS})'
    ss.puts '  SET(CMAKE_C_FLAGS_QC ${CMAKE_C_FLAGS})'
    ss.puts '  SET(CMAKE_C_FLAGS_PROFILE ${CMAKE_C_FLAGS})'
    ss.puts '  SET(CMAKE_SHARED_LINKER_FLAGS_RETAIL ${CMAKE_SHARED_LINKER_FLAGS})'
    ss.puts '  SET(CMAKE_SHARED_LINKER_FLAGS_QC ${CMAKE_SHARED_LINKER_FLAGS})'
    ss.puts '  SET(CMAKE_SHARED_LINKER_FLAGS_PROFILE ${CMAKE_SHARED_LINKER_FLAGS})'

    # Set the system info for cross-compiling to consoles, etc.
    ss.puts '  IF(${CONFIG_PLATFORM} STREQUAL WIN32)'
    ss.puts '      SET(CMAKE_SYSTEM_NAME WINDOWS)'
    ss.puts '      SET(CMAKE_SYSTEM_PROCESSOR x32)'
    ss.puts '  ELSEIF(${CONFIG_PLATFORM} STREQUAL WIN64)'
    ss.puts '      SET(CMAKE_SYSTEM_NAME WINDOWS)'
    ss.puts '      SET(CMAKE_SYSTEM_PROCESSOR x64)'
    ss.puts '  ELSEIF(${CONFIG_PLATFORM} STREQUAL XBOXONE)'
    ss.puts '      INCLUDE("Toolchain-XBoxOne")'
    ss.puts '  ELSE()'
    ss.puts '      MESSAGE(ERROR "Unknown platform.")'
    ss.puts '  ENDIF()'
    ss.puts 'ENDMACRO()'

    @project_groups.each do |group|
      ss.puts "PROJECT(#{group.name}_${CONFIG_NAME} CXX)"
      ss.puts 'SET_COMPILER_OPTIONS()'
      ss.puts

      # Because we don't want to concat those every time.
      ss.puts 'SET(CONFIG_NAME ${CONFIG_PLATFORM}_${CONFIG_LIBRARY_KIND}_${CONFIG_RENDERING_API})'

      group.each_project do |project|
        ss.puts "ADD_LIBRARY(#{project.name}_${CONFIG_NAME} ${CONFIG_LIBRARY_KIND}"
        project.each_file {|file| ss.puts "    ../#{file}\n"}
        ss.puts '    )'
        ss.puts
      end
    end

    File.write(File.join('cmake', 'CMakeLists.txt'), ss.string)
  end

  def generate_batch_file
    platforms = ['WIN32', 'WIN64', 'DURANGO']
    lib_kinds = ['STATIC', 'SHARED']
    rendering_apis = ['DIRECTX11', 'DIRECTX12', 'OPENGL4', 'VULKAN']
    build_types = ['DEBUG', 'RELEASE', 'RETAIL', 'QC', 'PROFILE']
    num_configs = platforms.length * lib_kinds.length * rendering_apis.length * build_types.length
    ss = StringIO.new
    ss.puts '@echo off'
    ss.puts 'setlocal enableextensions'
    ss.puts "echo Invoking CMake for each of the #{num_configs} configurations."
    config_index = 0
    platforms.each do |platform|
      lib_kinds.each do |lib_kind|
        rendering_apis.each do |rendering_api|
          build_types.each do |build_type|
            config_index += 1
            ss.puts "echo #{config_index}/#{num_configs} Generating configuration {#{platform}; #{lib_kind}; #{rendering_api}; #{build_type}}"
            ss.puts "    mkdir cmake\\#{platform}\\#{lib_kind}\\#{rendering_api}\\#{build_type}"
            ss.puts "    pushd cmake\\#{platform}\\#{lib_kind}\\#{rendering_api}\\#{build_type}"
            ss.puts "    cmake -DCONFIG_PLATFORM=#{platform} -DCONFIG_LIBRARY_KIND=#{lib_kind} -DCONFIG_RENDERING_API=#{rendering_api} -DCMAKE_BUILD_TYPE=#{build_type} ../../../../"
            ss.puts "    if not %errorlevel% == 0 goto error_exit"
            ss.puts "    popd"
          end
        end
      end
    end
    ss.puts 'exit'
    ss.puts
    ss.puts ':error_exit'
    ss.puts 'popd'
    ss.puts 'echo "Error during generation. Aborting."'

    File.write('run-cmake.bat', ss.string)
  end

  def generate_value_choice(name, desc, *values)
    ss = StringIO.new
    ss.puts "SET(#{name} CACHE STRING \"#{desc}\")"
    ss.puts "SET_PROPERTY(CACHE #{name} PROPERTY STRINGS #{values.join(' ')})"
    ss.puts
    ss.string
  end
end

project_groups = [
  ProjectGroup.new('game', 2, 3500),
  ProjectGroup.new('server', 7, 1500),
  ProjectGroup.new('editor', 4, 4000),
  ProjectGroup.new('engine', 10, 4000),
  ProjectGroup.new('tools', 80, 100),
  ProjectGroup.new('tests', 20, 100)]

puts "Generating stub C++ code."
project_groups.each do |group|
  puts "    Generating #{group.name}..."
  group.generate!
end

generators = [
  SharpmakeGenerator.new(project_groups),
  PremakeGenerator.new(project_groups),
  CMakeGenerator.new(project_groups)
]

generators.each do |generator|
  puts "Running #{generator}"
  generator.generate!
end
