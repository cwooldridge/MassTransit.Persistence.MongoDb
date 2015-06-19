// MassTransit.Persistence.MongoDb.Tests - Copyright (c) 2015 CaptiveAire

using System;
using System.Collections.Generic;
using System.Linq;

using MassTransit.Saga;

namespace MassTransit.Persistence.MongoDb.Tests
{
    public static class RepositoryHelpers
    {
        public static IEnumerable<TSaga> ByCorrelationId<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId)
            where TSaga : class, ISaga
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            return repository.Where(s => s.CorrelationId == correlationId);
        }

        public static IEnumerable<TSaga> ByCorrelationId<TSaga>(
            this ISagaRepository<TSaga> repository,
            CorrelatedBy<Guid> correlationImplementation) where TSaga : class, ISaga
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            return repository.Where(s => s.CorrelationId == correlationImplementation.CorrelationId);
        }
    }
}