using System;
using System.ComponentModel;
using System.IO;

namespace Wire.ValueSerializers
{
    public class DateTimeSerializer : ValueSerializer
    {
        public const byte Manifest = 5;
        public const int Size = sizeof(long);
        public static readonly DateTimeSerializer Instance = new DateTimeSerializer();

        public override void WriteManifest(Stream stream, SerializerSession session)
        {
            stream.WriteByte(Manifest);
        }

        public override void WriteValue(Stream stream, object value, SerializerSession session)
        {
            var dateTime = (DateTime) value;
            var bytes = NoAllocBitConverter.GetBytes(dateTime.Ticks, session);
            stream.Write(bytes, 0, Size);
            var kindByte = (byte) dateTime.Kind;
            stream.WriteByte(kindByte);
        }

        public override object ReadValue(Stream stream, DeserializerSession session)
        {
            var buffer = session.GetBuffer(Size);
            stream.Read(buffer, 0, Size);
            var ticks = BitConverter.ToInt64(buffer, 0);
            var kind = (DateTimeKind) stream.ReadByte();
            var dateTime = new DateTime(ticks, kind);
            return dateTime;
        }

        public override Type GetElementType()
        {
            return typeof(DateTime);
        }
    }
}