﻿using SiliconStudio.Assets.Tracking;
using SiliconStudio.Core;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Core.Yaml;

namespace SiliconStudio.Assets.Quantum
{
    internal class Module
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            // Make sure that this assembly is registered
            AssetQuantumRegistry.RegisterAssembly(typeof(Module).Assembly);
        }
    }
}
