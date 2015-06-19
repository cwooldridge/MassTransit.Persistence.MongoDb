// MassTransit.Persistence.MongoDb - Copyright (c) 2015 CaptiveAire

using System;

using Magnum.StateMachine;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MassTransit.Persistence.MongoDb
{
    internal class StateSerializer<T> : BsonBaseSerializer
        where T : StateMachine<T>
    {
        public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
        {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.String)
            {
                string value = bsonReader.ReadString();
                return StateMachine<T>.GetState(value);
            }

            string message = string.Format("StateSerializer expects to find a String and it was {0}.", bsonType);
            throw new BsonSerializationException(message);
        }

        public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
        {
            var state = value as State<T>;
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