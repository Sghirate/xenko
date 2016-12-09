// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.IO;
using SiliconStudio.Assets.Serializers;
using SiliconStudio.Core;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Core.Yaml;

namespace SiliconStudio.Assets
{
    public class AssetLoadResult<T>
    {
        public AssetLoadResult(T asset, ILogger logger, bool aliasOccurred, IDictionary<YamlAssetPath, OverrideType> overrides)
        {
            if (overrides == null) throw new ArgumentNullException(nameof(overrides));
            Asset = asset;
            Logger = logger;
            AliasOccurred = aliasOccurred;
            Overrides = overrides;
        }

        public T Asset { get; }

        public ILogger Logger { get; }

        public bool AliasOccurred { get; }

        public IDictionary<YamlAssetPath, OverrideType> Overrides { get; }

    }
    /// <summary>
    /// Main entry point for serializing/deserializing <see cref="Asset"/>.
    /// </summary>
    public static class AssetFileSerializer
    {
        private static readonly List<IAssetSerializerFactory> RegisteredSerializerFactories = new List<IAssetSerializerFactory>();

        /// <summary>
        /// The default serializer.
        /// </summary>
        public static readonly IAssetSerializer Default = new YamlAssetSerializer();

        static AssetFileSerializer()
        {
            Register((YamlAssetSerializer)Default);
            Register(SourceCodeAssetSerializer.Default);
        }

        /// <summary>
        /// Registers the specified serializer factory.
        /// </summary>
        /// <param name="serializerFactory">The serializer factory.</param>
        /// <exception cref="System.ArgumentNullException">serializerFactory</exception>
        public static void Register(IAssetSerializerFactory serializerFactory)
        {
            if (serializerFactory == null) throw new ArgumentNullException(nameof(serializerFactory));
            if (!RegisteredSerializerFactories.Contains(serializerFactory))
                RegisteredSerializerFactories.Add(serializerFactory);
        }

        /// <summary>
        /// Finds a serializer for the specified asset file extension.
        /// </summary>
        /// <param name="assetFileExtension">The asset file extension.</param>
        /// <returns>IAssetSerializerFactory.</returns>
        public static IAssetSerializer FindSerializer(string assetFileExtension)
        {
            if (assetFileExtension == null) throw new ArgumentNullException(nameof(assetFileExtension));
            assetFileExtension = assetFileExtension.ToLowerInvariant();
            for (int i = RegisteredSerializerFactories.Count - 1; i >= 0; i--)
            {
                var assetSerializerFactory = RegisteredSerializerFactories[i];
                var factory = assetSerializerFactory.TryCreate(assetFileExtension);
                if (factory != null)
                {
                    return factory;
                }
            }
            return null;
        }

        /// <summary>
        /// Deserializes an <see cref="Asset"/> from the specified stream.
        /// </summary>
        /// <typeparam name="T">Type of the asset</typeparam>
        /// <param name="filePath">The file path.</param>
        /// <param name="log">The logger.</param>
        /// <returns>An instance of Asset not a valid asset asset object file.</returns>
        public static AssetLoadResult<T> Load<T>(string filePath, ILogger log = null)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var result = Load<T>(stream, filePath, log);
                return result;
            }
        }

        public static AssetLoadResult<T> Load<T>(Stream stream, UFile filePath, ILogger log = null)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            var assetFileExtension = Path.GetExtension(filePath).ToLowerInvariant();

            var serializer = FindSerializer(assetFileExtension);
            if (serializer == null)
            {
                throw new InvalidOperationException("Unable to find a serializer for [{0}]".ToFormat(assetFileExtension));
            }
            bool aliasOccurred;
            Dictionary<YamlAssetPath, OverrideType> overrides;
            var asset = (T)serializer.Load(stream, filePath, log, out aliasOccurred, out overrides);
            // Let's fixup references after deserialization
            (asset as Asset)?.FixupPartReferences();
            return new AssetLoadResult<T>(asset, log, aliasOccurred, overrides ?? new Dictionary<YamlAssetPath, OverrideType>());
        }

        /// <summary>
        /// Serializes an <see cref="Asset" /> to the specified file path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="asset">The asset object.</param>
        /// <param name="log">The logger.</param>
        /// <param name="overrides"></param>
        /// <exception cref="System.ArgumentNullException">filePath</exception>
        public static void Save(string filePath, object asset, ILogger log = null, Dictionary<YamlAssetPath, OverrideType> overrides = null)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            // Creates automatically the directory when saving an asset.
            filePath = FileUtility.GetAbsolutePath(filePath);
            var directoryPath = Path.GetDirectoryName(filePath);
            if (directoryPath != null && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            using (var stream = new MemoryStream())
            {
                Save(stream, asset, log, overrides);
                File.WriteAllBytes(filePath, stream.ToArray());
            }
        }

        /// <summary>
        /// Serializes an <see cref="Asset" /> to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="asset">The asset object.</param>
        /// <param name="log">The logger.</param>
        /// <exception cref="System.ArgumentNullException">
        /// stream
        /// or
        /// assetFileExtension
        /// </exception>
        public static void Save(Stream stream, object asset, ILogger log = null, Dictionary<YamlAssetPath, OverrideType> overrides = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (asset == null) return;

            var assetFileExtension = AssetRegistry.GetDefaultExtension(asset.GetType());
            if (assetFileExtension == null)
            {
                throw new ArgumentException("Unable to find a serializer for the specified asset. No asset file extension registered to AssetRegistry");
            }

            var serializer = FindSerializer(assetFileExtension);
            if (serializer == null)
            {
                throw new InvalidOperationException($"Unable to find a serializer for [{assetFileExtension}]");
            }
            serializer.Save(stream, asset, log, overrides);
        }
    }
}
