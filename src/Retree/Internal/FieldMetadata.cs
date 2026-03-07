// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;

namespace Retree.Internal
{
    internal enum FieldKind
    {
        Value,
        RetreeNode,
        RetreeCollection
    }

    internal sealed class FieldMetadata
    {
        public string Name { get; }
        public FieldInfo FieldInfo { get; }
        public Func<object, object> Getter { get; }
        public FieldKind Kind { get; }

        public FieldMetadata(string name, FieldInfo fieldInfo, Func<object, object> getter, FieldKind kind)
        {
            Name = name;
            FieldInfo = fieldInfo;
            Getter = getter;
            Kind = kind;
        }

        public bool IsReferenceComparison =>
            Kind == FieldKind.RetreeNode || Kind == FieldKind.RetreeCollection ||
            FieldInfo.FieldType == typeof(string);
    }
}
