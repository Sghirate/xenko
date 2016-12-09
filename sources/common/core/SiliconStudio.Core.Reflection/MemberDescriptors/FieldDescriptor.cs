﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SiliconStudio.Core.Reflection
{
    /// <summary>
    /// A <see cref="IMemberDescriptor"/> for a <see cref="FieldInfo"/>
    /// </summary>
    public class FieldDescriptor : MemberDescriptorBase
    {
        public FieldDescriptor(ITypeDescriptor typeDescriptor, FieldInfo fieldInfo, StringComparer defaultNameComparer)
            : base(fieldInfo, defaultNameComparer)
        {
            if (fieldInfo == null) throw new ArgumentNullException(nameof(fieldInfo));

            FieldInfo = fieldInfo;
            TypeDescriptor = typeDescriptor;
        }

        /// <summary>
        /// Gets the property information attached to this instance.
        /// </summary>
        /// <value>The property information.</value>
        public FieldInfo FieldInfo { get; }

        public override Type Type => FieldInfo.FieldType;

        public override bool IsPublic => FieldInfo.IsPublic;

        public override bool HasSet => true;

        public override object Get(object thisObject)
        {
            return FieldInfo.GetValue(thisObject);
        }

        public override void Set(object thisObject, object value)
        {
            FieldInfo.SetValue(thisObject, value);
        }

        public override IEnumerable<T> GetCustomAttributes<T>(bool inherit)
        {
            return FieldInfo.GetCustomAttributes<T>(inherit);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return $"Field [{Name}] from Type [{(FieldInfo.DeclaringType != null ? FieldInfo.DeclaringType.FullName : string.Empty)}]";
        }
    }
}
