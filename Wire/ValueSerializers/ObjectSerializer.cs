using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Wire.ValueSerializers
{
    public class ObjectSerializer : ValueSerializer
    {
        public const byte ManifestFull = 255;
        public const byte ManifestIndex = 254;
      

        private static readonly ConcurrentDictionary<byte[], Type> TypeNameLookup =
            new ConcurrentDictionary<byte[], Type>(new ByteArrayEqualityComparer());

        private readonly byte[] _manifest;

        private volatile bool _isInitialized;
        private ValueReader _reader;
        private ValueWriter _writer;

        public ObjectSerializer(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            Type = type;
            var typeName = type.GetShortAssemblyQualifiedName();
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once AssignNullToNotNullAttribute
            var typeNameBytes = Encoding.UTF8.GetBytes(typeName);

            //precalculate the entire manifest for this serializer
            //this helps us to minimize calls to Stream.Write/WriteByte 
            _manifest =
                new[] {ManifestFull}
                    .Concat(BitConverter.GetBytes(typeNameBytes.Length))
                    .Concat(typeNameBytes)
                    .ToArray(); //serializer id 255 + assembly qualified name

            //initialize reader and writer with dummy handlers that wait until the serializer is fully initialized
            _writer = (stream, o, session) =>
            {
#if !NET35
                SpinWait.SpinUntil(() => _isInitialized);
#else
                while (_isInitialized == false) { }
#endif
                WriteValue(stream, o, session);
            };

            _reader = (stream, session) =>
            {
#if !NET35
                SpinWait.SpinUntil(() => _isInitialized);
#else
                while (_isInitialized == false) { }
#endif
                return ReadValue(stream, session);
            };
        }

        public Type Type { get; }

        public override void WriteManifest(Stream stream, Type type, SerializerSession session)
        {
            if (session.ShouldWriteTypeManifest(type))
            {
                stream.Write(_manifest);
            }
            else
            {
                var typeIdentifier = session.GetTypeIdentifier(type);
                stream.Write(new[] {ManifestIndex});
                stream.WriteUInt16((ushort) typeIdentifier);
            }
        }

        public override void WriteValue(Stream stream, object value, SerializerSession session)
        {
            _writer(stream, value, session);
        }

        public override object ReadValue(Stream stream, DeserializerSession session)
        {
            return _reader(stream, session);
        }

        public override Type GetElementType()
        {
            return Type;
        }

        public void Initialize(ValueReader reader, ValueWriter writer)
        {
            _reader = reader;
            _writer = writer;
            _isInitialized = true;
        }

        private static Type GetTypeFromManifestName(Stream stream, DeserializerSession session)
        {
            var bytes = (byte[]) ByteArraySerializer.Instance.ReadValue(stream, session);

            return TypeNameLookup.GetOrAdd(bytes, b =>
            {
                var shortName = Encoding.UTF8.GetString(b);
                var typename = Utils.ToQualifiedAssemblyName(shortName);
                return Type.GetType(typename, true);
            });
        }

        public static Type GetTypeFromManifestFull(Stream stream, DeserializerSession session)
        {
            var type = GetTypeFromManifestName(stream, session);
            session.TrackDeserializedType(type);
            return type;
        }

        public static Type GetTypeFromManifestIndex(Stream stream, DeserializerSession session)
        {
            var typeId = stream.ReadUInt16(session);
            var type = session.GetTypeFromTypeId(typeId);
            return type;
        }
    }
}