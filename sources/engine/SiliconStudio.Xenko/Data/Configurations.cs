﻿using System;
using System.ComponentModel;
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;

namespace SiliconStudio.Xenko.Data
{
    [Flags]
    public enum ConfigPlatforms
    {
        None = 0,
        Windows = 1 << PlatformType.Windows,
        UWP = 1 << PlatformType.UWP,
        [Obsolete("WindowsStore platform doesn't exist anymore, please use Windows 10 (UWP).")] WindowsStore = 1 << 29,
        [Obsolete("WindowsPhone platform doesn't exist anymore, please use Windows 10 (UWP).")] WindowsPhone = 1 << 30,
        iOS = 1 << PlatformType.iOS,
        Android = 1 << PlatformType.Android,
        Linux = 1 << PlatformType.Linux,
        macOS = 1 << PlatformType.macOS
    }

    [DataContract]
    public abstract class Configuration
    {
        [DataMemberIgnore]
        public bool OfflineOnly { get; protected set; }
    }

    [DataContract]
    public class ConfigurationOverride
    {
        [DataMember(10)]
        [InlineProperty]
        public ConfigPlatforms Platforms;

        [DataMember(20)]
        [DefaultValue(-1)]
        public int SpecificFilter = -1;

        [DataMember(30)]
        public Configuration Configuration;
    }
}
