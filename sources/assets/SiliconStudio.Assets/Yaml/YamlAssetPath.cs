using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SiliconStudio.Core.Reflection;

namespace SiliconStudio.Core.Yaml
{
    [DataContract]
    public class YamlAssetPath : IEquatable<YamlAssetPath>
    {
        public enum ItemType
        {
            Member,
            Index,
            ItemId
        }

        public struct Item : IEquatable<Item>
        {
            public readonly ItemType Type;
            public readonly object Value;
            public Item(ItemType type, object value)
            {
                Type = type;
                Value = value;
            }
            public string AsMember() { if (Type != ItemType.Member) throw new InvalidOperationException("This item is not a Member"); return (string)Value; }
            public ItemId AsItemId() { if (Type != ItemType.ItemId) throw new InvalidOperationException("This item is not a item Id"); return (ItemId)Value; }

            public bool Equals(Item other)
            {
                return Type == other.Type && Equals(Value, other.Value);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                return obj is Item && Equals((Item)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)Type*397) ^ (Value?.GetHashCode() ?? 0);
                }
            }
        }

        private readonly List<Item> items = new List<Item>(16);

        public IReadOnlyList<Item> Items => items;

        public void PushMember(string memberName)
        {
            items.Add(new Item(ItemType.Member, memberName));
        }

        public void PushIndex(object index)
        {
            items.Add(new Item(ItemType.Index, index));
        }

        public void PushItemId(ItemId itemId)
        {
            items.Add(new Item(ItemType.ItemId, itemId));
        }

        public void RemoveFirstItem()
        {
            for (var i = 1; i < items.Count; ++i)
            {
                items[i - 1] = items[i];
            }
            if (items.Count > 0)
            {
                items.RemoveAt(items.Count - 1);
            }
        }

        public YamlAssetPath Clone()
        {
            var clone = new YamlAssetPath();
            clone.items.AddRange(items);
            return clone;
        }

        public bool Equals(YamlAssetPath other)
        {
            if (Items.Count != other?.Items.Count)
                return false;

            for (var i = 0; i < Items.Count; ++i)
            {
                if (!Items[i].Equals(other.Items[i]))
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((YamlAssetPath)obj);
        }

        public override int GetHashCode()
        {
            return items.Aggregate(0, (hashCode, item) => (hashCode * 397) ^ item.GetHashCode());
        }

        public static bool operator ==(YamlAssetPath left, YamlAssetPath right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(YamlAssetPath left, YamlAssetPath right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("(object)");
            foreach (var item in items)
            {
                switch (item.Type)
                {
                    case ItemType.Member:
                        sb.Append('.');
                        sb.Append(item.Value);
                        break;
                    case ItemType.Index:
                        sb.Append('[');
                        sb.Append(item.Value);
                        sb.Append(']');
                        break;
                    case ItemType.ItemId:
                        sb.Append('{');
                        sb.Append(item.Value);
                        sb.Append('}');
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return sb.ToString();
        }

        public static bool IsCollectionWithIdType(Type type, object key, out ItemId id, out object actualKey)
        {
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(CollectionWithItemIds<>))
                {
                    id = (ItemId)key;
                    actualKey = key;
                    return true;
                }
                if (type.GetGenericTypeDefinition() == typeof(DictionaryWithItemIds<,>))
                {
                    var keyWithId = (IKeyWithId)key;
                    id = keyWithId.Id;
                    actualKey = keyWithId.Key;
                    return true;
                }
            }

            id = ItemId.Empty;
            actualKey = key;
            return false;
        }

        public static bool IsCollectionWithIdType(Type type, object key, out ItemId id)
        {
            object actualKey;
            return IsCollectionWithIdType(type, key, out id, out actualKey);
        }

        public YamlAssetPath Append(YamlAssetPath other)
        {
            var result = new YamlAssetPath();
            result.items.AddRange(items);
            result.items.AddRange(other.items);
            return result;
        }
    }
}
