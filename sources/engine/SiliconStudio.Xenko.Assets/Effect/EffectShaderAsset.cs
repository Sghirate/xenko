﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SiliconStudio.Assets;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.Core;
using SiliconStudio.Core.IO;
using SiliconStudio.Xenko.Shaders.Parser;
using SiliconStudio.Xenko.Shaders.Parser.Mixins;

namespace SiliconStudio.Xenko.Assets.Effect
{
    /// <summary>
    /// Describes a shader effect asset (xksl).
    /// </summary>
    [DataContract("EffectShader")]
    [AssetDescription(FileExtension, AlwaysMarkAsRoot = true, AllowArchetype = false)]
    [AssetCompiler(typeof(EffectShaderAssetCompiler))]
    [Display(920, "Effect Shader")]
    public sealed class EffectShaderAsset : ProjectSourceCodeWithFileGeneratorAsset
    {
        /// <summary>
        /// The default file extension used by the <see cref="EffectShaderAsset"/>.
        /// </summary>
        public const string FileExtension = ".xksl;.pdxsl";

        public static Regex Regex = new Regex(@"(^|\s)(class)($|\s)");

        public override string Generator => "XenkoShaderKeyGenerator";

        public override void Save(Stream stream)
        {
            //regex the shader name if it has changed
            Text = Regex.Replace(Text, "$1shader$3");

            base.Save(stream);
        }

        public override void SaveGeneratedAsset(AssetItem assetItem)
        {
            //generate the .cs files
            // Always output a result into the file
            string result;
            try
            {
                var parsingResult = XenkoShaderParser.TryPreProcessAndParse(Text, assetItem.FullPath);

                if (parsingResult.HasErrors)
                {
                    result = "// Failed to parse the shader:\n" + parsingResult;
                }
                else
                {
                    // Try to generate a mixin code.
                    var shaderKeyGenerator = new ShaderMixinCodeGen(parsingResult.Shader, parsingResult);

                    shaderKeyGenerator.Run();
                    result = shaderKeyGenerator.Text ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                result = "// Unexpected exceptions occurred while generating the file\n" + ex;
            }

            // We force the UTF8 to include the BOM to match VS default
            var data = Encoding.UTF8.GetBytes(result);
           
            File.WriteAllBytes(assetItem.GetGeneratedAbsolutePath(), data);
        }
    }
}
