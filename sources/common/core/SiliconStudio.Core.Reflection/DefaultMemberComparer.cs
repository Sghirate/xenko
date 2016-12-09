﻿using System;
using System.Collections.Generic;

namespace SiliconStudio.Core.Reflection
{
    public class DefaultMemberComparer : IComparer<object>
    {
        public int Compare(object x, object y)
        {
            var left = x as IMemberDescriptor;
            var right = y as IMemberDescriptor;
            if (left != null && right != null)
            {
                // If order is defined, first order by order
                if (left.Order.HasValue | right.Order.HasValue)
                {
                    var leftOrder = left.Order ?? int.MaxValue;
                    var rightOrder = right.Order ?? int.MaxValue;
                    return leftOrder.CompareTo(rightOrder);
                }

                // try to order by class hierarchy + token (same as declaration order)
                var leftMember = (x as MemberDescriptorBase)?.MemberInfo;
                var rightMember = (y as MemberDescriptorBase)?.MemberInfo;
                if (leftMember != null || rightMember != null)
                {
                    var comparison = leftMember.CompareMetadataTokenWith(rightMember);
                    if (comparison != 0)
                        return comparison;
                }

                // else order by name (dynamic members, etc...)
                return left.DefaultNameComparer.Compare(left.Name, right.Name);
            }

            var sx = x as string;
            var sy = y as string;
            if (sx != null && sy != null)
            {
                return string.CompareOrdinal(sx, sy);
            }

            var leftComparable = x as IComparable;
            if (leftComparable != null)
            {
                return leftComparable.CompareTo(y);
            }

            var rightComparable = y as IComparable;
            return rightComparable?.CompareTo(y) ?? 0;
        }
    }
}