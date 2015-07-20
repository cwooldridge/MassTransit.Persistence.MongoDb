// MassTransit.Persistence.MongoDb - Copyright (c) 2015 CaptiveAire

using System;
using System.Collections.Generic;
using Automatonymous;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MassTransit.Persistence.MongoDb.Automatonymous
{
    internal class StateSerializer<T> : BsonBaseSerializer where T : StateMachine
    {
        private readonly T _machine;

        public StateSerializer(T machine)
        {
            _machine = machine;
        }

        public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
        {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.String)
            {
                string value = bsonReader.ReadString();

                return _machine.GetState(value);
            }

            string message = string.Format("StateSerializer expects to find a String and it was {0}.", bsonType);
            throw new BsonSerializationException(message);
        }

        public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
        {
            var state = value as State;
            if (state == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                bsonWriter.WriteString(state.Name);
            }
        }
    }
}