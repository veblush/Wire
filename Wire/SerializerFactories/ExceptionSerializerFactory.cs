﻿using System;
using System.Collections.Concurrent;
using System.Reflection;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    public class ExceptionSerializerFactory : ValueSerializerFactory
    {
        private static readonly TypeInfo ExceptionTypeInfo = typeof(Exception).GetTypeInfo();
        private readonly FieldInfo _className;
        private readonly FieldInfo _innerException;
        private readonly FieldInfo _stackTraceString;
        private readonly FieldInfo _remoteStackTraceString;
        private readonly FieldInfo _message;

        public ExceptionSerializerFactory()
        {
            _className = ExceptionTypeInfo.GetField("_className", BindingFlagsEx.All);
            _innerException = ExceptionTypeInfo.GetField("_innerException", BindingFlagsEx.All);
            _message = ExceptionTypeInfo.GetField("_message", BindingFlagsEx.All);
            _remoteStackTraceString = ExceptionTypeInfo.GetField("_remoteStackTraceString", BindingFlagsEx.All);
            _stackTraceString = ExceptionTypeInfo.GetField("_stackTraceString", BindingFlagsEx.All);
        }

        public override bool CanSerialize(Serializer serializer, Type type) => ExceptionTypeInfo.IsAssignableFrom(type.GetTypeInfo());

        public override bool CanDeserialize(Serializer serializer, Type type) => CanSerialize(serializer, type);

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            ConcurrentDictionary<Type, ValueSerializer> typeMapping)
        {
            var exceptionSerializer = new ObjectSerializer(type);
            exceptionSerializer.Initialize((stream, session) =>
            {
                var exception = Activator.CreateInstance(type);
                var className = stream.ReadString(session);
                var message = stream.ReadString(session);
                var remoteStackTraceString = stream.ReadString(session);
                var stackTraceString = stream.ReadString(session);
                var innerException = stream.ReadObject(session);

                _className.SetValue(exception,className);
                _message.SetValue(exception, message);
                _remoteStackTraceString.SetValue(exception, remoteStackTraceString);
                _stackTraceString.SetValue(exception, stackTraceString);
                _innerException.SetValue(exception,innerException);
                return exception;
            }, (stream, exception, session) =>
            {
                var className = _className.GetValue(exception);
                var message = _message.GetValue(exception);
                var remoteStackTraceString = _remoteStackTraceString.GetValue(exception);
                var stackTraceString = _stackTraceString.GetValue(exception);
                var innerException = _innerException.GetValue(exception);
                stream.WriteString(className);
                stream.WriteString(message);
                stream.WriteString(remoteStackTraceString);
                stream.WriteString(stackTraceString);
                stream.WriteObjectWithManifest(innerException, session);
            });
            typeMapping.TryAdd(type, exceptionSerializer);
            return exceptionSerializer;
        }
    }
}