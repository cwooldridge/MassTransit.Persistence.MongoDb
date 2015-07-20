using System.Linq;
using Automatonymous;
using MassTransit.Logging;
using MassTransit.Util;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MassTransit.Persistence.MongoDb.Automatonymous
{
    public class MongoDbAutomatonymousSagaRepository<TStateMachineInstance, TStateMachine> :
        MongoDbSagaRepository<TStateMachineInstance>
        where TStateMachineInstance : class, SagaStateMachineInstance where TStateMachine : StateMachine
    {
        public MongoDbAutomatonymousSagaRepository(MongoDatabase database, TStateMachine machine)
            : base(database)
        {
            if (Log == null)
                Log =
                    Logger.Get(
                        typeof (MongoDbAutomatonymousSagaRepository<TStateMachineInstance, TStateMachine>)
                            .ToFriendlyName());

            // BsonClassMap.RegisterClassMap<SagaStateMachineInstance>();

            BsonClassMap<TStateMachineInstance> map;

            if (BsonClassMap.IsClassMapRegistered(typeof (TStateMachineInstance)))
            {
                map =
                    (BsonClassMap<TStateMachineInstance>) BsonClassMap.GetRegisteredClassMaps()
                        .First(c => c.ClassType == typeof (TStateMachineInstance));
            }
            else
            {
                map = BsonClassMap.RegisterClassMap<TStateMachineInstance>(
                    cm =>
                    {
                        cm.AutoMap();
                        cm.MapIdField(s => s.CorrelationId);
                        cm.MapIdMember(s => s.CorrelationId);
                        cm.MapIdProperty(s => s.CorrelationId);
                        cm.UnmapProperty(s => s.Bus);
                    });
            }
            map.UnmapProperty(s => s.Bus);
            var properties = typeof (TStateMachineInstance).GetProperties();
            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.PropertyType == typeof (State))
                {
                    map.MapProperty(propertyInfo.Name).SetSerializer(new StateSerializer<TStateMachine>(machine));
                }
                else if (propertyInfo.PropertyType == typeof (CompositeEventStatus))
                {
                    map.MapProperty(propertyInfo.Name).SetSerializer(new CompositeEventStatusSerializer());
                }
            }
        }
    }
}

