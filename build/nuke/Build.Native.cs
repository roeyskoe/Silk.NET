// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tooling.ProcessTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    [Parameter("Build native code")] readonly bool Native;

    [CanBeNull] string AndroidHomeValue;

    string AndroidHome
    {
        get
        {
            if (AndroidHomeValue is not null)
            {
                return AndroidHomeValue;
            }

            var utils = RootDirectory / "build" / "utilities";
            DotNet($"build \"{utils / "android_probe.proj"}\" /t:GetAndroidJar");
            AndroidHomeValue = (AbsolutePath) File.ReadAllText(utils / "android.jar.gen.txt") / ".." / ".." / "..";
            Logger.Info($"Android Home: {AndroidHomeValue}");
            return AndroidHomeValue;
        }
    }

    Target BuildLibSilkDroid => CommonTarget
    (
        x => x.Before(Compile)
            .After(Clean)
            .Executes
            (
                () =>
                {
                    if (!Native)
                    {
                        Logger.Warn("Skipping gradlew build as the --native parameter has not been specified.");
                        return Enumerable.Empty<Output>();
                    }

                    var sdl = RootDirectory / "build" / "submodules" / "SDL";
                    var silkDroid = SourceDirectory / "Windowing" / "SilkDroid";
                    var xcopy = new (string, string)[]
                    {
                        (sdl / "android-project" / "app" / "src" / "main" / "java",
                            silkDroid / "app" / "src" / "main" / "java"),
                        (sdl, silkDroid / "app" / "jni" / "SDL2")
                    };

                    foreach (var (from, to) in xcopy)
                    {
                        if (!Directory.Exists(from))
                        {
                            ControlFlow.Fail
                                ($"\"{from}\" does not exist (did you forget to recursively clone the repo?)");
                        }

                        CopyDirectoryRecursively(from, to, DirectoryExistsPolicy.Merge, FileExistsPolicy.Overwrite);
                    }

                    var envVars = Environment.GetEnvironmentVariables()
                        .Cast<DictionaryEntry>()
                        .ToDictionary(x => (string) x.Key, x => (string) x.Value);
                    envVars["ANDROID_HOME"] = AndroidHome;

                    foreach (var ndk in Directory.GetDirectories((AbsolutePath) AndroidHome / "ndk")
                                 .OrderByDescending(x => Version.Parse(Path.GetFileName(x))))
                    {
                        envVars["ANDROID_NDK_HOME"] = ndk;
                    }

                    using var process = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                        ? StartProcess("bash", "-c \"./gradlew build\"", silkDroid, envVars)
                        : StartProcess("cmd", "/c \".\\gradlew build\"", silkDroid, envVars);
                    process.AssertZeroExitCode();
                    var ret = process.Output;
                    CopyFile
                    (
                        silkDroid / "app" / "build" / "outputs" / "aar" / "app-release.aar",
                        SourceDirectory / "Windowing" / "Silk.NET.Windowing.Sdl" / "Android" / "app-release.aar",
                        FileExistsPolicy.Overwrite
                    );
                    return ret;
                }
            )
    );

    AbsolutePath GLFWPath => RootDirectory / "build" / "submodules" / "GLFW";
    Target GLFW => CommonTarget
    (
        x => x.Before(Compile)
            .Executes
            (
                () =>
                {
                    var @out = GLFWPath / "build";
                    var prepare = "cmake -S. -B build -D BUILD_SHARED_LIBS=ON";
                    var build = "cmake --build build --config Release";
                    EnsureCleanDirectory(@out);
                    var runtimes = RootDirectory / "src" / "Native" / "Silk.NET.GLFW.Native" / "runtimes";
                    if (OperatingSystem.IsWindows())
                    {
                        InheritedShell($"{prepare} -A X64", GLFWPath)
                            .AssertZeroExitCode();
                        InheritedShell(build, GLFWPath)
                            .AssertZeroExitCode();
                        CopyAll(@out.GlobFiles("src/Release/glfw3.dll"), runtimes / "win-x64" / "native");
                        
                        EnsureCleanDirectory(@out);
                        
                        InheritedShell($"{prepare} -A Win32", GLFWPath)
                            .AssertZeroExitCode();
                        InheritedShell(build, GLFWPath)
                            .AssertZeroExitCode();
                        
                        CopyAll(@out.GlobFiles("src/Release/glfw3.dll"), runtimes / "win-x86" / "native");
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        InheritedShell($"{prepare} -DCMAKE_SYSTEM_PROCESSOR=x86_64", GLFWPath)
                            .AssertZeroExitCode();
                        InheritedShell(build, GLFWPath)
                            .AssertZeroExitCode();
                        CopyAll(@out.GlobFiles("src/libglfw.so"), runtimes / "linux-x64" / "native");
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        InheritedShell($"{prepare} -DCMAKE_OSX_ARCHITECTURES=x86_64", GLFWPath)
                            .AssertZeroExitCode();
                        InheritedShell(build, GLFWPath)
                            .AssertZeroExitCode();
                        CopyAll(@out.GlobFiles("src/libglfw.3.dylib"), runtimes / "osx-x64" / "native");

                        EnsureCleanDirectory(@out);
                        
                        InheritedShell($"{prepare} -DCMAKE_OSX_ARCHITECTURES=arm64", GLFWPath)
                            .AssertZeroExitCode();
                        InheritedShell(build, GLFWPath)
                            .AssertZeroExitCode();
                        
                        CopyAll(@out.GlobFiles("src/libglfw.3.dylib"), runtimes / "osx-arm64" / "native");
                    }
                }
            )
    );

    AbsolutePath ALSoftPath => RootDirectory / "build" / "submodules" / "OpenALSoft";
    Target OpenALSoft => CommonTarget
    (
        x => x.Before(Compile)
            .Executes
            (
                () =>
                {
                    var @out = ALSoftPath / "build";
                    var prepare = "cmake -S. -B build -D BUILD_SHARED_LIBS=ON -DCMAKE_BUILD_TYPE=Release";
                    var build = "cmake --build build --config Release";
                    EnsureCleanDirectory(@out);
                    //var runtimes = RootDirectory / "src" / "Native" / "Silk.NET.OpenAL.Soft.Native" / "runtimes";
                    var runtimes = ALSoftPath / "tmp";
                    if (OperatingSystem.IsWindows())
                    {
                        InheritedShell($"{prepare} -A X64 -DALSOFT_BUILD_ROUTER=ON -DALSOFT_REQUIRE_WINMM=ON -DALSOFT_REQUIRE_DSOUND=ON -DALSOFT_REQUIRE_WASAPI=ON", ALSoftPath)
                            .AssertZeroExitCode();
                        InheritedShell(build, ALSoftPath)
                            .AssertZeroExitCode();
                        CopyAll(@out.GlobFiles("Release/soft_oal.dll"), runtimes / "win-x64" / "native");

                        EnsureCleanDirectory(@out);

                        InheritedShell($"{prepare} -A Win32 -DALSOFT_BUILD_ROUTER=ON -DALSOFT_REQUIRE_WINMM=ON -DALSOFT_REQUIRE_DSOUND=ON -DALSOFT_REQUIRE_WASAPI=ON", ALSoftPath)
                            .AssertZeroExitCode();
                        InheritedShell(build, ALSoftPath)
                            .AssertZeroExitCode();

                        CopyAll(@out.GlobFiles("Release/soft_oal.dll"), runtimes / "win-x86" / "native");
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        InheritedShell($"{prepare} -DCMAKE_SYSTEM_PROCESSOR=x86_64 -DALSOFT_REQUIRE_ALSA=ON -DALSOFT_REQUIRE_OSS=ON -DALSOFT_REQUIRE_PORTAUDIO=ON -DALSOFT_REQUIRE_PULSEAUDIO=ON -DALSOFT_REQUIRE_JACK=ON", ALSoftPath)
                            .AssertZeroExitCode();
                        InheritedShell(build, ALSoftPath)
                            .AssertZeroExitCode();
                        CopyAll(@out.GlobFiles("libopenal.so"), runtimes / "linux-x64" / "native");

                        EnsureCleanDirectory(@out);

                        InheritedShell($"{prepare} -DCMAKE_TOOLCHAIN_FILE={AndroidHome}/ndk/build/cmake/android.toolchain.cmake", ALSoftPath)
                            .AssertZeroExitCode();
                        InheritedShell(build, ALSoftPath)
                            .AssertZeroExitCode();
                        CopyAll(@out.GlobFiles("*"), runtimes / "Android" / "native");

                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        InheritedShell($"{prepare} -DCMAKE_OSX_ARCHITECTURES=x86_64 -DCMAKE_BUILD_TYPE=Release -DALSOFT_REQUIRE_COREAUDIO=ON", ALSoftPath)
                            .AssertZeroExitCode();
                        InheritedShell(build, ALSoftPath)
                            .AssertZeroExitCode();
                        CopyAll(@out.GlobFiles("libopenal.dylib"), runtimes / "osx-x64" / "native");
                    }
                }
            )
    );

}
