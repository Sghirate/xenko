// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using SiliconStudio.Core;
using SiliconStudio.Xenko.Rendering;

namespace SiliconStudio.Xenko.Assets.Models
{
    /// <summary>
    /// Represents a <see cref="MaterialInstance"/> in a 
    /// </summary>
    [DataContract]
    public class ModelMaterial
    {
        /// <summary>
        /// Gets or sets the material slot name in a <see cref="ModelAsset"/>.
        /// </summary>
        /// <value>
        /// The material slot name.
        /// </value>
        /// <userdoc>The .</userdoc>
        /// <userdoc>The name of the material as written in the imported model and the reference to the corresponding material asset.</userdoc>
        [DataMember(10)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the material stored in this slot.
        /// </summary>
        /// <value>
        /// The material.
        /// </value>
        [DataMember(20)]
        public MaterialInstance MaterialInstance { get; set; }
    }
}
