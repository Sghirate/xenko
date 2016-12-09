﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SiliconStudio.Assets;
using SiliconStudio.Core;
using SiliconStudio.Core.VisualStudio;

namespace SiliconStudio.Xenko.Assets
{
    [DataContract("Xenko")]
    public sealed class XenkoConfig
    {
        public const string PackageName = "Xenko";

        private const string XamariniOSBuild = @"MSBuild\Xamarin\iOS\Xamarin.iOS.CSharp.targets";
        private const string XamarinAndroidBuild = @"MSBuild\Xamarin\Android\Xamarin.Android.CSharp.targets";

        private const string UniversalWindowsPlatformRuntimeBuild = @"MSBuild\Microsoft\WindowsXaml\v14.0\8.2\Microsoft.Windows.UI.Xaml.Common.Targets";
        private static readonly string ProgramFilesX86 = Environment.GetEnvironmentVariable(Environment.Is64BitOperatingSystem ? "ProgramFiles(x86)" : "ProgramFiles");

        public static readonly PackageVersion LatestPackageVersion = new PackageVersion(XenkoVersion.CurrentAsText);

        public static PackageDependency GetLatestPackageDependency()
        {
            return new PackageDependency(PackageName, new PackageVersionRange()
                {
                    MinVersion = LatestPackageVersion,
                    IsMinInclusive = true
                });
        }

        /// <summary>
        /// Registers the solution platforms supported by Xenko.
        /// </summary>
        internal static void RegisterSolutionPlatforms()
        {
            var solutionPlatforms = new List<SolutionPlatform>();

            // Define CoreCLR configurations
            var coreClrRelease = new SolutionConfiguration("CoreCLR_Release");
            var coreClrDebug = new SolutionConfiguration("CoreCLR_Debug");
            coreClrDebug.IsDebug = true;
            // Add CoreCLR specific properties
            coreClrDebug.Properties.AddRange(new[]
            {
                "<SiliconStudioRuntime Condition=\"'$(SiliconStudioProjectType)' == 'Executable'\">CoreCLR</SiliconStudioRuntime>",
                "<SiliconStudioBuildDirExtension Condition=\"'$(SiliconStudioBuildDirExtension)' == ''\">CoreCLR</SiliconStudioBuildDirExtension>",
                "<DefineConstants>SILICONSTUDIO_RUNTIME_CORECLR;$(DefineConstants)</DefineConstants>"
            });
            coreClrRelease.Properties.AddRange(new[]
            {
                "<SiliconStudioRuntime Condition=\"'$(SiliconStudioProjectType)' == 'Executable'\">CoreCLR</SiliconStudioRuntime>",
                "<SiliconStudioBuildDirExtension Condition=\"'$(SiliconStudioBuildDirExtension)' == ''\">CoreCLR</SiliconStudioBuildDirExtension>",
                "<DefineConstants>SILICONSTUDIO_RUNTIME_CORECLR;$(DefineConstants)</DefineConstants>"
            });

            // Windows
            var windowsPlatform = new SolutionPlatform()
                {
                    Name = PlatformType.Windows.ToString(),
                    IsAvailable = true,
                    Alias = "Any CPU",
                    Type = PlatformType.Windows
                };
            windowsPlatform.PlatformsPart.Add(new SolutionPlatformPart("Any CPU"));
            windowsPlatform.PlatformsPart.Add(new SolutionPlatformPart("Mixed Platforms") { Alias = "Any CPU"});
            windowsPlatform.DefineConstants.Add("SILICONSTUDIO_PLATFORM_WINDOWS");
            windowsPlatform.DefineConstants.Add("SILICONSTUDIO_PLATFORM_WINDOWS_DESKTOP");
            windowsPlatform.Configurations.Add(new SolutionConfiguration("Testing"));
            windowsPlatform.Configurations.Add(new SolutionConfiguration("AppStore"));
            windowsPlatform.Configurations.Add(coreClrDebug);
            windowsPlatform.Configurations.Add(coreClrRelease);
            foreach (var part in windowsPlatform.PlatformsPart)
            {
                part.Configurations.Clear();
                part.Configurations.AddRange(windowsPlatform.Configurations);
            }
            solutionPlatforms.Add(windowsPlatform);

            // Universal Windows Platform (UWP)
            var uwpPlatform = new SolutionPlatform()
            {
                Name = PlatformType.UWP.ToString(),
                Type = PlatformType.UWP,
                IsAvailable = IsFileInProgramFilesx86Exist(UniversalWindowsPlatformRuntimeBuild),
                UseWithExecutables = false,
                IncludeInSolution = false,
            };

            uwpPlatform.DefineConstants.Add("SILICONSTUDIO_PLATFORM_WINDOWS");
            uwpPlatform.DefineConstants.Add("SILICONSTUDIO_PLATFORM_UWP");
            uwpPlatform.Configurations.Add(new SolutionConfiguration("Testing"));
            uwpPlatform.Configurations.Add(new SolutionConfiguration("AppStore"));
            uwpPlatform.Configurations["Release"].Properties.Add("<NoWarn>;2008</NoWarn>");
            uwpPlatform.Configurations["Debug"].Properties.Add("<NoWarn>;2008</NoWarn>");
            uwpPlatform.Configurations["Testing"].Properties.Add("<NoWarn>;2008</NoWarn>");
            uwpPlatform.Configurations["AppStore"].Properties.Add("<NoWarn>;2008</NoWarn>");

            uwpPlatform.Configurations["Release"].Properties.Add("<UseDotNetNativeToolchain>true</UseDotNetNativeToolchain>");
            uwpPlatform.Configurations["Testing"].Properties.Add("<UseDotNetNativeToolchain>true</UseDotNetNativeToolchain>");
            uwpPlatform.Configurations["AppStore"].Properties.Add("<UseDotNetNativeToolchain>true</UseDotNetNativeToolchain>");

            foreach (var cpu in new[] { "x86", "x64", "ARM" })
            {
                var uwpPlatformCpu = new SolutionPlatformPart(uwpPlatform.Name + "-" + cpu)
                {
                    LibraryProjectName = uwpPlatform.Name,
                    ExecutableProjectName = cpu,
                    Cpu = cpu,
                    InheritConfigurations = true,
                    UseWithLibraries = false,
                    UseWithExecutables = true,
                };
                uwpPlatformCpu.Configurations.Clear();
                uwpPlatformCpu.Configurations.AddRange(uwpPlatform.Configurations);

                uwpPlatform.PlatformsPart.Add(uwpPlatformCpu);
            }

            solutionPlatforms.Add(uwpPlatform);

            // Linux
            var linuxPlatform = new SolutionPlatform()
            {
                Name = PlatformType.Linux.ToString(),
                IsAvailable = true,
                Type = PlatformType.Linux,
            };
            linuxPlatform.DefineConstants.Add("SILICONSTUDIO_PLATFORM_UNIX");
            linuxPlatform.DefineConstants.Add("SILICONSTUDIO_PLATFORM_LINUX");
            linuxPlatform.Configurations.Add(coreClrRelease);
            linuxPlatform.Configurations.Add(coreClrDebug);
            solutionPlatforms.Add(linuxPlatform);

            // Disabling macOS for time being
            if (false)
            {
                // macOS
                var macOSPlatform = new SolutionPlatform()
                {
                    Name = PlatformType.macOS.ToString(),
                    IsAvailable = true,
                    Type = PlatformType.macOS,
                };
                macOSPlatform.DefineConstants.Add("SILICONSTUDIO_PLATFORM_UNIX");
                macOSPlatform.DefineConstants.Add("SILICONSTUDIO_PLATFORM_MACOS");
                macOSPlatform.Configurations.Add(coreClrRelease);
                macOSPlatform.Configurations.Add(coreClrDebug);
                solutionPlatforms.Add(macOSPlatform);
            }

            // Android
            var androidPlatform = new SolutionPlatform()
            {
                Name = PlatformType.Android.ToString(),
                Type = PlatformType.Android,
                IsAvailable = IsFileInProgramFilesx86Exist(XamarinAndroidBuild)
            };
            androidPlatform.DefineConstants.Add("SILICONSTUDIO_PLATFORM_MONO_MOBILE");
            androidPlatform.DefineConstants.Add("SILICONSTUDIO_PLATFORM_ANDROID");
            androidPlatform.Configurations.Add(new SolutionConfiguration("Testing"));
            androidPlatform.Configurations.Add(new SolutionConfiguration("AppStore"));
            androidPlatform.Configurations["Debug"].Properties.AddRange(new[]
                {
                    "<AndroidUseSharedRuntime>True</AndroidUseSharedRuntime>",
                    "<AndroidLinkMode>None</AndroidLinkMode>",
                });
            androidPlatform.Configurations["Release"].Properties.AddRange(new[]
                {
                    "<AndroidUseSharedRuntime>False</AndroidUseSharedRuntime>",
                    "<AndroidLinkMode>SdkOnly</AndroidLinkMode>",
                });
            androidPlatform.Configurations["Testing"].Properties.AddRange(androidPlatform.Configurations["Release"].Properties);
            androidPlatform.Configurations["AppStore"].Properties.AddRange(androidPlatform.Configurations["Release"].Properties);
            solutionPlatforms.Add(androidPlatform);

            // iOS: iPhone
            var iphonePlatform = new SolutionPlatform()
            {
                Name = PlatformType.iOS.ToString(),
                SolutionName = "iPhone", // For iOS, we need to use iPhone as a solution name
                Type = PlatformType.iOS,
                IsAvailable = IsFileInProgramFilesx86Exist(XamariniOSBuild)
            };
            iphonePlatform.PlatformsPart.Add(new SolutionPlatformPart("iPhoneSimulator"));
            iphonePlatform.DefineConstants.Add("SILICONSTUDIO_PLATFORM_MONO_MOBILE");
            iphonePlatform.DefineConstants.Add("SILICONSTUDIO_PLATFORM_IOS");
            iphonePlatform.Configurations.Add(new SolutionConfiguration("Testing"));
            iphonePlatform.Configurations.Add(new SolutionConfiguration("AppStore"));
            var iPhoneCommonProperties = new List<string>
                {
                    "<ConsolePause>false</ConsolePause>",
                    "<MtouchUseSGen>True</MtouchUseSGen>",
                    "<MtouchArch>ARMv7, ARMv7s, ARM64</MtouchArch>"
                };

            iphonePlatform.Configurations["Debug"].Properties.AddRange(iPhoneCommonProperties);
            iphonePlatform.Configurations["Debug"].Properties.AddRange(new []
                {
                    "<MtouchDebug>True</MtouchDebug>",
                    "<CodesignKey>iPhone Developer</CodesignKey>",
                    "<MtouchUseSGen>True</MtouchUseSGen>",
                });
            iphonePlatform.Configurations["Release"].Properties.AddRange(iPhoneCommonProperties);
            iphonePlatform.Configurations["Release"].Properties.AddRange(new[]
                {
                    "<CodesignKey>iPhone Developer</CodesignKey>",
                });
            iphonePlatform.Configurations["Testing"].Properties.AddRange(iPhoneCommonProperties);
            iphonePlatform.Configurations["Testing"].Properties.AddRange(new[]
                {
                    "<MtouchDebug>True</MtouchDebug>",
                    "<CodesignKey>iPhone Distribution</CodesignKey>",
                    "<BuildIpa>True</BuildIpa>",
                });
            iphonePlatform.Configurations["AppStore"].Properties.AddRange(iPhoneCommonProperties);
            iphonePlatform.Configurations["AppStore"].Properties.AddRange(new[]
                {
                    "<CodesignKey>iPhone Distribution</CodesignKey>",
                });
            solutionPlatforms.Add(iphonePlatform);

            // iOS: iPhoneSimulator
            var iPhoneSimulatorPlatform = iphonePlatform.PlatformsPart["iPhoneSimulator"];
            iPhoneSimulatorPlatform.Configurations["Debug"].Properties.AddRange(new[]
                {
                    "<MtouchDebug>True</MtouchDebug>",
                    "<MtouchLink>None</MtouchLink>",
                    "<MtouchArch>i386, x86_64</MtouchArch>"
                });
            iPhoneSimulatorPlatform.Configurations["Release"].Properties.AddRange(new[]
                {
                    "<MtouchLink>None</MtouchLink>",
                    "<MtouchArch>i386, x86_64</MtouchArch>"
                });

            AssetRegistry.RegisterSupportedPlatforms(solutionPlatforms);
        }

        internal static bool IsFileInProgramFilesx86Exist(string path)
        {
            return (ProgramFilesX86 != null && File.Exists(Path.Combine(ProgramFilesX86, path)));
        }
    }
}
