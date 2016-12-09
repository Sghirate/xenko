﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Collections.Concurrent;
using System.IO;
using SiliconStudio.Assets;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.BuildEngine;
using SiliconStudio.Core;
using SiliconStudio.Core.IO;
using SiliconStudio.Xenko.Rendering;
using SiliconStudio.Xenko.Shaders.Compiler;

namespace SiliconStudio.Xenko.Assets.Effect
{
    /// <summary>
    /// Entry point to compile an <see cref="EffectShaderAsset"/>
    /// </summary>
    public class EffectShaderAssetCompiler : AssetCompilerBase
    {
        public static readonly PropertyKey<ConcurrentDictionary<string, string>> ShaderLocationsKey = new PropertyKey<ConcurrentDictionary<string, string>>("ShaderPathsKey", typeof(EffectShaderAssetCompiler));

        protected override void Compile(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var url = EffectCompilerBase.DefaultSourceShaderFolder + "/" + Path.GetFileName(assetItem.FullPath);
            var asset = (EffectShaderAsset)assetItem.Asset;

            var originalSourcePath = assetItem.FullPath;
            result.BuildSteps = new AssetBuildStep(assetItem) { new ImportStreamCommand { SourcePath = originalSourcePath, Location = url, SaveSourcePath = true } };
            var shaderLocations = (ConcurrentDictionary<string, string>)context.Properties.GetOrAdd(ShaderLocationsKey, key => new ConcurrentDictionary<string, string>());

            // Store directly this into the context TODO this this temporary
            shaderLocations[url] = originalSourcePath;
        }
    }
}
