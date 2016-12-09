﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.IO;
using SiliconStudio.Core.Yaml.Serialization;

namespace SiliconStudio.Core.Yaml
{
    // TODO: this works only for asset now. Allow to select the YamlSerializer to use to make it work for other scenario
    public static class DynamicYamlExtensions
    {
        public static T ConvertTo<T>(IDynamicYamlNode yamObject)
        {
            using (var memoryStream = new MemoryStream())
            {
                // convert Yaml nodes to string
                using (var streamWriter = new StreamWriter(memoryStream))
                {
                    var yamlStream = new YamlStream { new YamlDocument(yamObject.Node) };
                    yamlStream.Save(streamWriter, true, AssetYamlSerializer.Default.GetSerializerSettings().PreferredIndent);

                    streamWriter.Flush();
                    memoryStream.Position = 0;

                    // convert string to object
                    return (T)AssetYamlSerializer.Default.Deserialize(memoryStream, typeof(T));
                }
            }
        }

        public static IDynamicYamlNode ConvertFrom<T>(T dataObject)
        {
            using (var stream = new MemoryStream())
            {
                // convert data to string
                AssetYamlSerializer.Default.Serialize(stream, dataObject);

                stream.Position = 0;

                // convert string to Yaml nodes
                using (var reader = new StreamReader(stream))
                {
                    var yamlStream = new YamlStream();
                    yamlStream.Load(reader);
                    return (IDynamicYamlNode)DynamicYamlObject.ConvertToDynamic(yamlStream.Documents[0].RootNode);
                }
            }
        }
    }
}
