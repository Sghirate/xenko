﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SiliconStudio.Assets;
using SiliconStudio.Xenko.Assets.Entities;
using SiliconStudio.Xenko.Engine;

namespace SiliconStudio.Xenko.Assets.Tests
{
    [TestFixture]
    public class TestPrefabAsset
    {
        [Test]
        public void TestSerialization()
        {
            var originAsset = CreateOriginAsset();

            using (var stream = new MemoryStream())
            {
                AssetFileSerializer.Save(stream, originAsset);

                stream.Position = 0;
                var serializedVersion = Encoding.UTF8.GetString(stream.ToArray());
                Console.WriteLine(serializedVersion);

                stream.Position = 0;
                var newAsset = AssetFileSerializer.Load<PrefabAsset>(stream, "Prefab.xkprefab").Asset;

                CheckAsset(originAsset, newAsset);
            }
        }

        [Test]
        public void TestClone()
        {
            var originAsset = CreateOriginAsset();
            var newAsset = AssetCloner.Clone(originAsset);
            CheckAsset(originAsset, newAsset);
        }

        private static PrefabAsset CreateOriginAsset()
        {
            // Basic test of entity serialization with links between entities (entity-entity, entity-component)
            // E1
            //   | E2 + link to E1 via TestEntityComponent
            // E3
            // E4 + link to E3.Transform component via TestEntityComponent

            var originAsset = new PrefabAsset();

            {
                var entity1 = new Entity() { Name = "E1" };
                var entity2 = new Entity() { Name = "E2", Group = EntityGroup.Group1 }; // Use group property to make sure that it is properly serialized
                var entity3 = new Entity() { Name = "E3" };
                var entity4 = new Entity() { Name = "E4", Group = EntityGroup.Group2 };

                // TODO: Add script link

                entity1.Transform.Children.Add(entity2.Transform);

                // Test a link between entity1 and entity2
                entity2.Add(new TestEntityComponent() { EntityLink = entity1 });

                // Test a component link between entity4 and entity 3
                entity4.Add(new TestEntityComponent() { EntityComponentLink = entity3.Transform });

                originAsset.Hierarchy.Parts.Add(new EntityDesign(entity1));
                originAsset.Hierarchy.Parts.Add(new EntityDesign(entity2));
                originAsset.Hierarchy.Parts.Add(new EntityDesign(entity3));
                originAsset.Hierarchy.Parts.Add(new EntityDesign(entity4));

                originAsset.Hierarchy.RootPartIds.Add(entity1.Id);
                originAsset.Hierarchy.RootPartIds.Add(entity3.Id);
                originAsset.Hierarchy.RootPartIds.Add(entity4.Id);
            }
            return originAsset;
        }

        private static void CheckGenericAsset(PrefabAsset originAsset, PrefabAsset newAsset)
        {
            // Check that we have exactly the same root entities
            Assert.AreEqual(originAsset.Hierarchy.RootPartIds, newAsset.Hierarchy.RootPartIds);
            Assert.AreEqual(originAsset.Hierarchy.Parts.Count, newAsset.Hierarchy.Parts.Count);

            foreach (var entityDesign in originAsset.Hierarchy.Parts)
            {
                var newEntityDesign = newAsset.Hierarchy.Parts[entityDesign.Entity.Id];
                Assert.NotNull(newEntityDesign);

                // Check properties
                Assert.AreEqual(entityDesign.Entity.Name, newEntityDesign.Entity.Name);
                Assert.AreEqual(entityDesign.Entity.Group, newEntityDesign.Entity.Group);

                // Check that we have the same amount of components
                Assert.AreEqual(entityDesign.Entity.Components.Count, newEntityDesign.Entity.Components.Count);

                // Check that we have the same children
                Assert.AreEqual(entityDesign.Entity.Transform.Children.Count, newEntityDesign.Entity.Transform.Children.Count);

                for (int i = 0; i < entityDesign.Entity.Transform.Children.Count; i++)
                {
                    var children = entityDesign.Entity.Transform.Children[i];
                    var newChildren = newEntityDesign.Entity.Transform.Children[i];
                    // Make sure that it is the same entity id
                    Assert.AreEqual(children.Entity.Id, newChildren.Entity.Id);

                    // Make sure that we resolve to the global entity and not a copy
                    Assert.True(newAsset.Hierarchy.Parts.ContainsKey(newChildren.Entity.Id));
                    Assert.AreEqual(newChildren.Entity, newAsset.Hierarchy.Parts[newChildren.Entity.Id].Entity);
                }
            }
        }

        private static void CheckAsset(PrefabAsset originAsset, PrefabAsset newAsset)
        {
            CheckGenericAsset(originAsset, newAsset);

            var entity1 = originAsset.Hierarchy.Parts.First(it => it.Entity.Name == "E1").Entity;
            var entity2 = originAsset.Hierarchy.Parts.First(it => it.Entity.Name == "E2").Entity;
            var entity3 = originAsset.Hierarchy.Parts.First(it => it.Entity.Name == "E3").Entity;
            var entity4 = originAsset.Hierarchy.Parts.First(it => it.Entity.Name == "E4").Entity;

            // Check that we have exactly the same root entities
            var newEntityDesign1 = newAsset.Hierarchy.Parts[entity1.Id];
            var newEntityDesign2 = newAsset.Hierarchy.Parts[entity2.Id];
            var newEntityDesign3 = newAsset.Hierarchy.Parts[entity3.Id];
            var newEntityDesign4 = newAsset.Hierarchy.Parts[entity4.Id];

            // Check that Transform.Children is correctly setup
            Assert.AreEqual(newEntityDesign2.Entity.Transform, newEntityDesign1.Entity.Transform.Children.FirstOrDefault());

            // Test entity-entity link from E2 to E1
            {
                var component = newEntityDesign2.Entity.Get<TestEntityComponent>();
                Assert.NotNull(component);
                Assert.AreEqual(newEntityDesign1.Entity, component.EntityLink);
            }

            // Test entity-component link from E4 to E3
            {
                var component = newEntityDesign4.Entity.Get<TestEntityComponent>();
                Assert.NotNull(component);
                Assert.AreEqual(newEntityDesign3.Entity.Transform, component.EntityComponentLink);
            }
        }
    }
}
