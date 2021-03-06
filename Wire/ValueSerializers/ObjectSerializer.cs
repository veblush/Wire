using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Wire.ValueSerializers
{
    public class ObjectSerializer : ValueSerializer
    {
        public const byte ManifestVersion = 251;
        public const byte ManifestFull = 255;
        public const byte ManifestIndex = 254;

        private readonly byte[] _manifest;
        private readonly byte[] _manifestWithVersionInfo;

        private volatile bool _isInitialized;
        private ObjectReader _reader;
        private ObjectWriter _writer;

        public ObjectSerializer(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            Type = type;
            //TODO: remove version info
            var typeName = type.GetShortAssemblyQualifiedName();
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once AssignNullToNotNullAttribute
            var typeNameBytes = typeName.ToUtf8Bytes();

            var fields = ReflectionEx.GetFieldInfosForType(type);
            var fieldNames = fields.Select(field => field.Name.ToUtf8Bytes()).ToList();
            var versionInfo = TypeEx.GetTypeManifest(fieldNames);

            //precalculate the entire manifest for this serializer
            //this helps us to minimize calls to Stream.Write/WriteByte 
            _manifest =
                new[] {ManifestFull}
                    .Concat(BitConverter.GetBytes(typeNameBytes.Length))
                    .Concat(typeNameBytes)
                    .ToArray(); //serializer id 255 + assembly qualified name

            //this is the same as the above, but including all field names of the type, in alphabetical order
            _manifestWithVersionInfo =
                new[] { ManifestVersion }
                    .Concat(BitConverter.GetBytes(typeNameBytes.Length))
                    .Concat(typeNameBytes)
                    .Concat(versionInfo)
                    .ToArray(); //serializer id 255 + assembly qualified name + versionInfo

            //initialize reader and writer with dummy handlers that wait until the serializer is fully initialized
            _writer = (stream, o, session) =>
            {
                SpinWait.SpinUntil(() => _isInitialized);
                WriteValue(stream, o, session);
            };

            _reader = (stream, session) =>
            {
                SpinWait.SpinUntil(() => _isInitialized);
                return ReadValue(stream, session);
            };
        }

        public Type Type { get; }

        public override void WriteManifest(Stream stream, SerializerSession session)
        {
            ushort typeIdentifier;
            if (session.ShouldWriteTypeManifest(Type,out typeIdentifier))
            {
                session.TrackSerializedType(Type);
                if (session.Serializer.Options.VersionTolerance)
                    stream.Write(_manifestWithVersionInfo);
                else
                    stream.Write(_manifest);
            }
            else
            {
                stream.Write(new[] {ManifestIndex});
                stream.WriteUInt16(typeIdentifier);
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

        public void Initialize(ObjectReader reader, ObjectWriter writer)
        {
            _reader = reader;
            _writer = writer;
            _isInitialized = true;
        }
    }
}