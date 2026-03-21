// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace RetreeCore.Internal
{
    internal static class FieldCache
    {
        private static readonly Dictionary<Type, FieldMetadata[]> _cache = new Dictionary<Type, FieldMetadata[]>();

        public static FieldMetadata[] GetFields(Type type)
        {
            if (_cache.TryGetValue(type, out var cached))
                return cached;

            var fields = DiscoverFields(type);
            _cache[type] = fields;
            return fields;
        }

        private static FieldMetadata[] DiscoverFields(Type type)
        {
            var result = new List<FieldMetadata>();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Walk the inheritance chain up to (but not including) RetreeNode/RetreeBase
            var currentType = type;
            while (currentType != null && currentType != typeof(RetreeNode) && currentType != typeof(RetreeBase) && currentType != typeof(object))
            {
                var declaredFields = currentType.GetFields(flags | BindingFlags.DeclaredOnly);
                foreach (var field in declaredFields)
                {
                    if (ShouldObserve(field))
                    {
                        var getter = CompileGetter(type, field);
                        var kind = ClassifyField(field.FieldType);
                        result.Add(new FieldMetadata(field.Name, field, getter, kind));
                    }
                }
                currentType = currentType.BaseType;
            }

            return result.ToArray();
        }

        private static bool ShouldObserve(FieldInfo field)
        {
            if (field.IsStatic) return false;
            if (field.IsLiteral) return false; // const
            if (field.IsInitOnly) return false; // readonly
            if (field.IsDefined(typeof(RetreeIgnoreAttribute), false)) return false;

            // Exclude compiler-generated backing fields for auto-properties
            if (field.Name.StartsWith("<") && field.Name.Contains(">k__BackingField"))
                return false;

            return true;
        }

        private static FieldKind ClassifyField(Type fieldType)
        {
            if (typeof(RetreeNode).IsAssignableFrom(fieldType))
                return FieldKind.RetreeNode;

            // Catches RetreeList<>, RetreeDictionary<,>, and any subclasses (e.g. SerializedRetreeDictionary<,>)
            if (typeof(RetreeBase).IsAssignableFrom(fieldType))
                return FieldKind.RetreeCollection;

            return FieldKind.Value;
        }

        private static Func<object, object> CompileGetter(Type ownerType, FieldInfo field)
        {
            // (object instance) => (object)((OwnerType)instance).fieldName
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var castInstance = Expression.Convert(instanceParam, ownerType);
            var fieldAccess = Expression.Field(castInstance, field);
            var castResult = Expression.Convert(fieldAccess, typeof(object));
            var lambda = Expression.Lambda<Func<object, object>>(castResult, instanceParam);
            return lambda.Compile();
        }

        internal static void Reset()
        {
            _cache.Clear();
        }
    }
}
