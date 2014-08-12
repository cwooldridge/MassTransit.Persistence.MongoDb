namespace MassTransit.Persistence.MongoDb.Tests.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Magnum.Extensions;

    using MassTransit.Context;
    using MassTransit.Saga;

    using MongoDB.Driver;

    using NUnit.Framework;

    [TestFixture]
    [Category("Integration")]
    public class When_using_the_saga_locator_with_MongoDb
    {
        [SetUp]
        public void Setup()
        {
            var mongoClient = new MongoClient();

            // requires an instance of mongodb running at localhost
            _db = mongoClient.GetServer().GetDatabase("MassTransit-SagaTest");
            _sagaId = NewId.NextGuid();
        }

        [TearDown]
        public void teardown()
        {
        }

        MongoDatabase _db;

        Guid _sagaId;

        IEnumerable<Action<IConsumeContext<InitiateSimpleSaga>>> GetHandlers(
            TestSaga instance,
            IConsumeContext<InitiateSimpleSaga> context)
        {
            yield return x => instance.RaiseEvent(TestSaga.Initiate, x.Message);
        }

        [Test]
        public void A_correlated_message_should_find_the_correct_saga()
        {
            var repository = new MongoDbSagaRepository<TestSaga>(_db);
            var initiatePolicy =
                new InitiatingSagaPolicy<TestSaga, InitiateSimpleSaga>(
                    x => x.CorrelationId,
                    x => false);

            var message = new InitiateSimpleSaga(_sagaId);
            IConsumeContext<InitiateSimpleSaga> context =
                new ConsumeContext<InitiateSimpleSaga>(ReceiveContext.Empty(), message);

            repository.GetSaga(context, message.CorrelationId, GetHandlers, initiatePolicy)
                .Each(x => x(context));

            List<TestSaga> sagas = repository.Where(x => x.CorrelationId == _sagaId).ToList();

            Assert.AreEqual(1, sagas.Count);
            Assert.IsNotNull(sagas[0]);
            Assert.AreEqual(_sagaId, sagas[0].CorrelationId);
        }
    }
}