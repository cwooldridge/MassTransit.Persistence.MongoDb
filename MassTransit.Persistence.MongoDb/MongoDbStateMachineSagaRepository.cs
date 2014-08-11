// Copyright 2013 CaptiveAire Systems
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.

using System;
using Magnum.StateMachine;
using MassTransit.Logging;
using MassTransit.Saga;
using MassTransit.Util;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MassTransit.Persistence.MongoDb
{
    public class MongoDbStateMachineSagaRepository<TSaga> : MongoDbSagaRepository<TSaga>
        where TSaga : SagaStateMachine<TSaga>, ISaga, new()
    {

        /// <summary> Initializes a new instance of the MongoDbSagaRepository class. </summary>
        /// <exception cref="ArgumentNullException"> Thrown when one or more required arguments are null. </exception>
        /// <param name="database"> The database. </param>
        public MongoDbStateMachineSagaRepository(MongoDatabase database) : base(database)
        {
            if (Log == null)
            {
                Log = Logger.Get(typeof (MongoDbStateMachineSagaRepository<TSaga>).ToFriendlyName());
            }

            BsonClassMap.RegisterClassMap<StateMachine<TSaga>>(cm =>
            {
                cm.MapField("_currentState").SetSerializer(new StateSerializer<TSaga>());
                cm.SetIsRootClass(true);
            });

            BsonClassMap.RegisterClassMap<SagaStateMachine<TSaga>>();


            BsonClassMap.RegisterClassMap<TSaga>(
                cm =>
                {
                    cm.AutoMap();
                    cm.MapIdField(s => s.CorrelationId);
                    cm.MapIdMember(s => s.CorrelationId);
                    cm.MapIdProperty(s => s.CorrelationId);
                    cm.UnmapProperty(s => s.Bus);
                });
        }
    }
}