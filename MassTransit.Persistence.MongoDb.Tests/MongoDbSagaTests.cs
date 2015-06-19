// MassTransit.Persistence.MongoDb.Tests - Copyright (c) 2015 CaptiveAire

using System;
using System.Collections.Generic;
using System.Linq;

using Magnum.Extensions;

using MassTransit.Context;
using MassTransit.Persistence.MongoDb.Tests.Sagas;
using MassTransit.Saga;

using MongoDB.Driver;

using NUnit.Framework;

namespace MassTransit.Persistence.MongoDb.Tests
{
    [TestFixture]
    [Category("Integration")]
    public class MongoDbSagaTests
    {
        private MongoDatabase _db;

        private Guid _sagaId;

        [SetUp]
        public void Setup()
        {
            var mongoClient = new MongoClient();

            // requires an instance of mongodb running at localhost
            this._db = mongoClient.GetServer().GetDatabase("MassTransit-SagaTest");
            this._sagaId = NewId.NextGuid();
        }

        [TearDown]
        public void Teardown()
        {
        }

        private IEnumerable<Action<IConsumeContext<InitiateSimpleSaga>>> GetHandlers(
            TestSaga instance,
            IConsumeContext<InitiateSimpleSaga> context)
        {
            yield return x => instance.RaiseEvent(TestSaga.Initiate, x.Message);
        }

        [Test]
        public void CorrelatedMessageShouldFindTheCorrectSaga()
        {
            var repository = new MongoDbStateMachineSagaRepository<TestSaga>(this._db);
            var initiatePolicy = new InitiatingSagaPolicy<TestSaga, InitiateSimpleSaga>(x => x.CorrelationId, x => false);

            var message = new InitiateSimpleSaga(this._sagaId);
            var context = new ConsumeContext<InitiateSimpleSaga>(ReceiveContext.Empty(), message);

            repository.GetSaga(context, message.CorrelationId, this.GetHandlers, initiatePolicy).Each(x => x(context));

            List<TestSaga> sagas = repository.ByCorrelationId(this._sagaId).ToList();

            Assert.AreEqual(1, sagas.Count);
            Assert.IsNotNull(sagas[0]);
            Assert.AreEqual(this._sagaId, sagas[0].CorrelationId);
        }

        //[Test]
        //public void ConcurrentSagaShouldTransition()
        //{
        //    var repository = new MongoDbStateMachineSagaRepository<AuctionSaga>(this._db);
        //    var initiatePolicy = new InitiatingSagaPolicy<AuctionSaga, CreateAuction>(x => x.CorrelationId, x => false);

        //    Bus.Initialize(sbc =>
        //    {
        //        sbc.ReceiveFrom("loopback://localhost/auction_saga_test_bus");
        //        sbc.Subscribe(
        //            subs =>
        //                {
        //                    subs.Saga(repository).Permanent();
        //                });
        //    });

        //    var correlationId = Magnum.CombGuid.Generate();

        //    var newAuction = new CreateAuction(correlationId) { OpeningBid = 1, OwnerEmail = "mr@test.com", Title = "Test Auction" };

        //    var context = new ConsumeContext<CreateAuction>(ReceiveContext.Empty(), newAuction);

        //    var auction = initiatePolicy.CreateInstance(context, correlationId);

        //    Bus.Instance.Publish(auction);
        //    Bus.Instance.Publish(newAuction);

        //    Assert.AreEqual("Test Auction", auction.Title);

        //    Bus.Instance.Dispose();
        //}
    }
}