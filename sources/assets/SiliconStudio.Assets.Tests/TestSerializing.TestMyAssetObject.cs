﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Collections;
using System.Collections.Generic;
using SiliconStudio.Core;

namespace SiliconStudio.Assets.Tests
{
    public partial class TestSerializing
    {
        [DataContract("MyAsset")]
        [AssetDescription(".xkobj")]
        public class MyAsset : Asset
        {
            public MyAsset()
            {
                CustomReferences = new List<AssetReference>();
                SeqItemsEmpty = new List<string>();
                SeqItems1 = new List<string>();
                // TODO: Re-enable non-pure collections here once we support them for serialization!
                //SeqItems2 = new MyCollection();
                SeqItems3 = new MyCollectionPure();
                SeqItems4 = new List<string>();
                // TODO: Re-enable non-pure collections here once we support them for serialization!
                //SeqItems5 = new MyCollection();
                MapItemsEmpty = new Dictionary<object, object>();
                MapItems1 = new Dictionary<object, object>();
                // TODO: Re-enable non-pure collections here once we support them for serialization!
                //MapItems2 = new MyDictionary();
                MapItems3 = new MyDictionaryPure();
                CustomObjectWithProtectedSet = new CustomObject { Name = "customObject" };
            }

            [DataMember(0)]
            public string Description { get; set; }

            public object AssetDirectory { get; set; }

            public object AssetUrl { get; set; }

            public AssetReference CustomReference2 { get; set; }

            public List<AssetReference> CustomReferences { get; set; }

            public List<string> SeqItemsEmpty { get; set; }

            public List<string> SeqItems1 { get; set; }

            // TODO: Re-enable non-pure collections here once we support them for serialization!
            //public MyCollection SeqItems2 { get; set; }

            public MyCollectionPure SeqItems3 { get; set; }

            public IList SeqItems4 { get; }

            // TODO: Re-enable non-pure collections here once we support them for serialization!
            //public IList SeqItems5 { get; set; }

            public Dictionary<object, object> MapItemsEmpty { get; set; }

            public Dictionary<object, object> MapItems1 { get; set; }

            // TODO: Re-enable non-pure collections here once we support them for serialization!
            //public MyDictionary MapItems2 { get; set; }

            public MyDictionaryPure MapItems3 { get; set; }

            public object CustomObjectWithProtectedSet { get; protected set; }
        }

        [DataContract("CustomObject")]
        public class CustomObject
        {
            public string Name { get; set; }
        }

        [DataContract("MyCollection")]
        public class MyCollection : List<string>
        {
            public string Name { get; set; }
        }

        [DataContract("MyCollectionPure")]
        public class MyCollectionPure : List<string>
        {
        }

        [DataContract("MyDictionary")]
        public class MyDictionary : Dictionary<object, object>
        {
            public string Name { get; set; }
        }

        [DataContract("MyDictionaryPure")]
        public class MyDictionaryPure : Dictionary<object, object>
        {
        }
    }
}
