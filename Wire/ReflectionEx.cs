﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if SERIALIZATION

#endif

namespace Wire
{
    public static class BindingFlagsEx
    {
        public const BindingFlags All = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    }

    public static class ReflectionEx
    {
        public static readonly Assembly CoreAssembly = typeof(int).GetTypeInfo().Assembly;

        public static FieldInfo[] GetFieldInfosForType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var fieldInfos = new List<FieldInfo>();
            var current = type;
            while (current != null)
            {
                var tfields =
                    current
                        .GetTypeInfo()
                        .GetFields(BindingFlagsEx.All)
#if SERIALIZATION
                        .Where(f => !f.IsDefined(typeof(NonSerializedAttribute)))
#endif
                        .Where(f => !f.IsStatic)
                        .Where(f => f.FieldType != typeof(IntPtr))
                        .Where(f => f.FieldType != typeof(UIntPtr))
                        .Where(f => f.Name != "_syncRoot"); //HACK: ignore these 

                fieldInfos.AddRange(tfields);
                current = current.GetTypeInfo().BaseType;
            }
            var fields = fieldInfos.OrderBy(f => f.Name).ToArray();
            return fields;
        }
    }
}