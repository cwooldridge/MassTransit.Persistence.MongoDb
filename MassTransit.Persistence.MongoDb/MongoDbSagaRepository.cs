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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MassTransit.Exceptions;
using MassTransit.Logging;
using MassTransit.Pipeline;
using MassTransit.Saga;
using MassTransit.Util;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MassTransit.Persistence.MongoDb
{
    public class MongoDbSagaRepository<TSaga> : ISagaRepository<TSaga>
        where TSaga : class, ISaga
    {
        private readonly MongoDatabase _database;
        protected ILog Log;

        /// <summary> Initializes a new instance of the MongoDbSagaRepository class. </summary>
        /// <exception cref="ArgumentNullException"> Thrown when one or more required arguments are null. </exception>
        /// <param name="database"> The database. </param>
        public MongoDbSagaRepository(MongoDatabase database)
        {
            if (database == null) throw new ArgumentNullException("database");

            if (Log == null) Log = Logger.Get(typeof (MongoDbSagaRepository<TSaga>).ToFriendlyName());

            _database = database;

            if (!BsonClassMap.IsClassMapRegistered(typeof (TSaga)))
            {
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

        /// <summary> Gets the collection. </summary>
        /// <value> The collection. </value>
        protected MongoCollection<TSaga> Collection
        {
            get
            {
                //because queries seem to have a limit of 127 bytes in the ns
                //we are going to make this collection name a little smaller
                var collectionName = typeof (TSaga).ToFriendlyName().Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries).Last();
                var dbName = string.Format("MTSagas.{0}", collectionName);

                return _database.GetCollection<TSaga>(dbName, WriteConcern.Acknowledged);
            }
        }

        /// <summary> Gets the queryable. </summary>
        /// <value> The queryable. </value>
        protected IQueryable<TSaga> Queryable
        {
            get { return Collection.AsQueryable(); }
        }

        /// <summary> Enumerates find in this collection. </summary>
        /// <param name="filter"> A filter specifying the. </param>
        /// <returns> An enumerator that allows foreach to be used to process find in this collection. </returns>
        public IEnumerable<Guid> Find(ISagaFilter<TSaga> filter)
        {
            return Where(filter, x => x.CorrelationId);
        }

        /// <summary> Enumerates get saga in this collection. </summary>
        /// <exception cref="ArgumentNullException"> Thrown when one or more required arguments are null. </exception>
        /// <exception cref="SagaException"> Thrown when a saga error condition occurs. </exception>
        /// <typeparam name="TMessage"> Type of the message. </typeparam>
        /// <param name="context"> The context. </param>
        /// <param name="sagaId"> Identifier for the saga. </param>
        /// <param name="selector"> The selector. </param>
        /// <param name="policy"> The policy. </param>
        /// <returns> An enumerator that allows foreach to be used to process get saga&lt; t message&gt; in this collection. </returns>
        public IEnumerable<Action<IConsumeContext<TMessage>>> GetSaga<TMessage>(
            IConsumeContext<TMessage> context,
            Guid sagaId,
            InstanceHandlerSelector<TSaga, TMessage> selector,
            ISagaPolicy<TSaga, TMessage> policy) where TMessage : class
        {
            if (context == null) throw new ArgumentNullException("context");
            if (selector == null) throw new ArgumentNullException("selector");
            if (policy == null) throw new ArgumentNullException("policy");

            var instance = Queryable.FirstOrDefault(x => x.CorrelationId == sagaId);

            if (instance == null)
            {
                if (policy.CanCreateInstance(context)) yield return CreateNewSagaAction(sagaId, selector, policy);
                else
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.DebugFormat(
                            "SAGA: {0} Ignoring Missing {1} for {2}",
                            typeof (TSaga).ToFriendlyName(),
                            sagaId,
                            typeof (TMessage).ToFriendlyName());
                    }
                }
            }
            else
            {
                if (policy.CanUseExistingInstance(context))
                    yield return UseExistingSagaAction(sagaId, selector, policy, instance);
                else
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.DebugFormat(
                            "SAGA: {0} Ignoring Existing {1} for {2}",
                            typeof (TSaga).ToFriendlyName(),
                            sagaId,
                            typeof (TMessage).ToFriendlyName());
                    }
                }
            }
        }

        /// <summary> Enumerates select in this collection. </summary>
        /// <typeparam name="TResult"> Type of the result. </typeparam>
        /// <param name="transformer"> The transformer. </param>
        /// <returns> An enumerator that allows foreach to be used to process select&lt; t result&gt; in this collection. </returns>
        public IEnumerable<TResult> Select<TResult>(Func<TSaga, TResult> transformer)
        {
            return Queryable.Select(transformer).ToList();
        }

        /// <summary> Enumerates where in this collection. </summary>
        /// <param name="filter"> A filter specifying the. </param>
        /// <returns> An enumerator that allows foreach to be used to process where in this collection. </returns>
        public IEnumerable<TSaga> Where(ISagaFilter<TSaga> filter)
        {
            return Queryable.Where(filter.FilterExpression).ToList();
        }

        /// <summary> Enumerates where in this collection. </summary>
        /// <typeparam name="TResult"> Type of the result. </typeparam>
        /// <param name="filter"> A filter specifying the. </param>
        /// <param name="transformer"> The transformer. </param>
        /// <returns> An enumerator that allows foreach to be used to process where&lt; t result&gt; in this collection. </returns>
        public IEnumerable<TResult> Where<TResult>(
            ISagaFilter<TSaga> filter,
            Func<TSaga, TResult> transformer)
        {
            return Queryable.Where(filter.FilterExpression).Select(transformer).ToList();
        }

        /// <summary>
        ///     Gets mongo query.
        /// </summary>
        /// <param name="queryable"> The queryable.</param>
        /// <returns>
        ///     The mongo query.
        /// </returns>
        protected IMongoQuery GetMongoQuery(IQueryable<TSaga> queryable)
        {
            if (queryable == null) throw new ArgumentNullException("queryable");

            var mongoQueryable = queryable as MongoQueryable<TSaga>;

            return mongoQueryable != null ? mongoQueryable.GetMongoQuery() : null;
        }

        /// <summary>
        ///     Creates new saga action.
        /// </summary>
        /// <exception cref="SagaException"> Thrown when a saga error condition occurs.</exception>
        /// <typeparam name="TMessage"> Type of the message.</typeparam>
        /// <param name="sagaId">   Identifier for the saga.</param>
        /// <param name="selector"> The selector.</param>
        /// <param name="policy">   The policy.</param>
        /// <returns>
        ///     The new new saga action&lt; t message&gt;
        /// </returns>
        private Action<IConsumeContext<TMessage>> CreateNewSagaAction<TMessage>(
            Guid sagaId,
            InstanceHandlerSelector<TSaga, TMessage> selector,
            ISagaPolicy<TSaga, TMessage> policy)
            where TMessage : class
        {
            return x =>
            {
                if (Log.IsDebugEnabled)
                {
                    Log.DebugFormat(
                        "SAGA: {0} Creating New {1} for {2}",
                        typeof (TSaga).ToFriendlyName(),
                        sagaId,
                        typeof (TMessage).ToFriendlyName());
                }

                try
                {
                    var instance = policy.CreateInstance(x, sagaId);

                    foreach (var callback in selector(instance, x))
                    {
                        callback(x);
                    }

                    if (!policy.CanRemoveInstance(instance)) Collection.Insert(instance, WriteConcern.Acknowledged);
                    else Collection.Save(instance, WriteConcern.Acknowledged);
                }
                catch (Exception ex)
                {
                    var sagaException = new SagaException(
                        "Create Saga Instance Exception",
                        typeof (TSaga),
                        typeof (TMessage),
                        sagaId,
                        ex);

                    if (Log.IsErrorEnabled) Log.Error(sagaException);

                    throw sagaException;
                }
            };
        }

        /// <summary>
        ///     Use existing saga action.
        /// </summary>
        /// <exception cref="SagaException"> Thrown when a saga error condition occurs.</exception>
        /// <typeparam name="TMessage"> Type of the message.</typeparam>
        /// <param name="sagaId">   Identifier for the saga.</param>
        /// <param name="selector"> The selector.</param>
        /// <param name="policy">   The policy.</param>
        /// <param name="instance"> The instance.</param>
        /// <returns>
        ///     .
        /// </returns>
        private Action<IConsumeContext<TMessage>> UseExistingSagaAction<TMessage>(
            Guid sagaId,
            InstanceHandlerSelector<TSaga, TMessage> selector,
            ISagaPolicy<TSaga, TMessage> policy,
            TSaga instance)
            where TMessage : class
        {
            return x =>
            {
                if (Log.IsDebugEnabled)
                {
                    Log.DebugFormat(
                        "SAGA: {0} Using Existing {1} for {2}",
                        typeof (TSaga).ToFriendlyName(),
                        sagaId,
                        typeof (TMessage).ToFriendlyName());
                }

                try
                {
                    foreach (var callback in selector(instance, x))
                    {
                        callback(x);
                    }

                    if (policy.CanRemoveInstance(instance))
                    {
                        Collection.Remove(
                            GetMongoQuery(Queryable.Where(q => q.CorrelationId == sagaId)),
                            RemoveFlags.Single,
                            WriteConcern.Acknowledged);
                    }
                }
                catch (Exception ex)
                {
                    var sagaException = new SagaException(
                        "Existing Saga Instance Exception",
                        typeof (TSaga),
                        typeof (TMessage),
                        sagaId,
                        ex);
                    if (Log.IsErrorEnabled) Log.Error(sagaException);

                    throw sagaException;
                }
            };
        }
    }
}