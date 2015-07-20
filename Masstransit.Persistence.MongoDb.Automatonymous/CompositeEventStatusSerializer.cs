using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Automatonymous;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MassTransit.Persistence.MongoDb.Automatonymous
{
    public class CompositeEventStatusSerializer : BsonBaseSerializer
    {
        public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
        {
            BsonType bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Int32)
            {
                var value = bsonReader.ReadInt32();

                return new CompositeEventStatus(value);
            }

            string message = string.Format("StateSerializer expects to find a String and it was {0}.", bsonType);
            throw new BsonSerializationException(message);
        }

        public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
        {
            if (value is CompositeEventStatus)
            {
                bsonWriter.WriteInt32(((CompositeEventStatus)value).Bits);
            }
            else
            {
                bsonWriter.WriteNull();
            }
          
        }
    }
}
