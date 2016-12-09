using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Core.Yaml.Serialization;
using SiliconStudio.Core.Yaml.Serialization.Serializers;

namespace SiliconStudio.Core.Yaml
{
    /// <summary>
    /// An implementation of <see cref="CollectionWithIdsSerializerBase"/> for actual collections.
    /// </summary>
    [YamlSerializerFactory("Assets")]
    public class CollectionWithIdsSerializer : CollectionWithIdsSerializerBase
    {
        /// <summary>
        /// A collection serializer used in case we determine that the given collection should not be serialized with ids.
        /// </summary>
        private readonly CollectionSerializer collectionSerializer = new CollectionSerializer();

        /// <inheritdoc/>
        public override IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            if (typeDescriptor is CollectionDescriptor)
            {
                var dataStyle = typeDescriptor.Type.GetCustomAttribute<DataStyleAttribute>();
                if (dataStyle == null || dataStyle.Style != DataStyle.Compact)
                    return this;
            }
            return null;
        }

        /// <inheritdoc/>
        protected override void ReadYamlAfterTransform(ref ObjectContext objectContext, bool transformed)
        {
            if (transformed)
                base.ReadYamlAfterTransform(ref objectContext, true);
            else
                GetCollectionSerializerForNonTransformedObject().ReadYaml(ref objectContext);
        }

        /// <inheritdoc/>
        protected override void WriteYamlAfterTransform(ref ObjectContext objectContext, bool transformed)
        {
            if (transformed)
                base.WriteYamlAfterTransform(ref objectContext, true);
            else
                GetCollectionSerializerForNonTransformedObject().WriteYaml(ref objectContext);
        }

        protected virtual CollectionSerializer GetCollectionSerializerForNonTransformedObject()
        {
            return collectionSerializer;
        }

        /// <inheritdoc/>
        protected override void TransformObjectAfterRead(ref ObjectContext objectContext)
        {
            InstanceInfo info;
            if (!objectContext.Properties.TryGetValue(InstanceInfoKey, out info))
            {
                base.TransformObjectAfterRead(ref objectContext);

                if (AreCollectionItemsIdentifiable(ref objectContext))
                {
                    // This is to be backward compatible with previous serialization. We fetch ids from the ~Id member of each item
                    var enumerable = objectContext.Instance as IEnumerable;
                    if (enumerable != null)
                    {
                        var ids = CollectionItemIdHelper.GetCollectionItemIds(objectContext.Instance);
                        var i = 0;
                        foreach (var item in enumerable)
                        {
                            var id = item != null ? IdentifiableHelper.GetId(item) : Guid.NewGuid();
                            ids[i] = id != Guid.Empty ? new ItemId(id.ToByteArray()) : ItemId.New();
                            ++i;
                        }
                    }
                }
                return;
            }

            var instance = info.Instance ?? objectContext.SerializerContext.ObjectFactory.Create(info.Descriptor.Type);
            ICollection<ItemId> deletedItems;
            objectContext.Properties.TryGetValue(DeletedItemsKey, out deletedItems);
            TransformAfterDeserialization((IDictionary)objectContext.Instance, info.Descriptor, instance, deletedItems);
            objectContext.Instance = instance;

            base.TransformObjectAfterRead(ref objectContext);
        }

        /// <inheritdoc/>
        protected override object TransformForSerialization(ITypeDescriptor descriptor, object collection)
        {
            var instance = CreatEmptyContainer(descriptor);
            CollectionItemIdentifiers identifier;
            if (!CollectionItemIdHelper.TryGetCollectionItemIds(collection, out identifier))
            {
                identifier = new CollectionItemIdentifiers();
            }
            var i = 0;
            foreach (var item in (IEnumerable)collection)
            {
                ItemId id;
                if (!identifier.TryGet(i, out id))
                {
                    id = ItemId.New();
                    identifier.Add(i, id);
                }
                instance.Add(id, item);
                ++i;
            }

            return instance;
        }

        /// <inheritdoc/>
        protected override IDictionary CreatEmptyContainer(ITypeDescriptor descriptor)
        {
            var collectionDescriptor = (CollectionDescriptor)descriptor;
            var type = typeof(CollectionWithItemIds<>).MakeGenericType(collectionDescriptor.ElementType);
            if (type.GetConstructor(Type.EmptyTypes) == null)
                throw new InvalidOperationException("The type of collection does not have a parameterless constructor.");
            return (IDictionary)Activator.CreateInstance(type);
        }

        /// <inheritdoc/>
        protected override void TransformAfterDeserialization(IDictionary container, ITypeDescriptor targetDescriptor, object targetCollection, ICollection<ItemId> deletedItems = null)
        {
            var collectionDescriptor = (CollectionDescriptor)targetDescriptor;
            var type = typeof(CollectionWithItemIds<>).MakeGenericType(collectionDescriptor.ElementType);
            if (!type.IsInstanceOfType(container))
                throw new InvalidOperationException("The given container does not match the expected type.");
            var identifier = CollectionItemIdHelper.GetCollectionItemIds(targetCollection);
            identifier.Clear();
            var i = 0;
            var enumerator = container.GetEnumerator();
            while (enumerator.MoveNext())
            {
                collectionDescriptor.Add(targetCollection, enumerator.Value);
                identifier.Add(i, (ItemId)enumerator.Key);
                ++i;
            }
            if (deletedItems != null)
            {
                foreach (var deletedItem in deletedItems)
                {
                    identifier.MarkAsDeleted(deletedItem);
                }
            }
        }

        protected override void WriteDeletedItems(ref ObjectContext objectContext)
        {
            ICollection<ItemId> deletedItems;
            objectContext.Properties.TryGetValue(DeletedItemsKey, out deletedItems);
            if (deletedItems != null)
            {
                var keyValueType = new KeyValuePair<Type, Type>(typeof(ItemId), typeof(string));
                foreach (var deletedItem in deletedItems)
                {
                    var entry = new KeyValuePair<object, object>(deletedItem, YamlDeletedKey);
                    WriteDictionaryItem(ref objectContext, entry, keyValueType);
                }
            }
        }

        protected override KeyValuePair<object, object> ReadDeletedDictionaryItem(ref ObjectContext objectContext, object keyResult)
        {
            var valueResult = objectContext.ObjectSerializerBackend.ReadDictionaryValue(ref objectContext, typeof(string), keyResult);
            var id = (ItemId)keyResult;
            return new KeyValuePair<object, object>(id, valueResult);
        }
    }
}
