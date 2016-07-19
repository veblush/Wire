using System;
using System.Collections.Concurrent;
using System.IO;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    public class ArraySerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type)
        {
            return type.IsArray && type.GetArrayRank() == 1;
        }

        public override bool CanDeserialize(Serializer serializer, Type type)
        {
            return CanSerialize(serializer, type);
        }

        private static void WriteValues(Array array, Stream stream, Type elementType, ValueSerializer elementSerializer, SerializerSession session)
        {
            stream.WriteInt32(array.Length);
            var preserveObjectReferences = session.Serializer.Options.PreserveObjectReferences;
            for (int i = 0; i < array.Length; i++)
            {
                var value = array.GetValue(i);
                stream.WriteObject(value, elementType, elementSerializer, preserveObjectReferences, session);
            }
        }

        private static Array ReadValues(int length, Stream stream, DeserializerSession session, Array array)
        {
            for (var i = 0; i < length; i++)
            {
                var value = stream.ReadObject(session);
                array.SetValue(value, i);
            }
            return array;
        }

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            ConcurrentDictionary<Type, ValueSerializer> typeMapping)
        {
            var arraySerializer = new ObjectSerializer(type);
            var elementType = type.GetElementType();
            var elementSerializer = serializer.GetSerializerByType(elementType);
            //TODO: code gen this part
            arraySerializer.Initialize((stream, session) =>
            {
                var length = stream.ReadInt32(session);
                var array = Array.CreateInstance(elementType, length); //create the array

                return ReadValues(length, stream, session, array);
            }, (stream, obj, session) =>
            {                
                WriteValues((Array)obj, stream,elementType,elementSerializer,session);   
            });
            typeMapping.TryAdd(type, arraySerializer);
            return arraySerializer;
        }
    }
}