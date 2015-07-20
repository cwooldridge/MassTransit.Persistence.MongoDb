using System;
using Automatonymous;
using MassTransit.Persistence.MongoDb.Automatonymous;
using MassTransit.Saga;
using MassTransit.Testing;
using MongoDB.Driver;
using MongoDB.Embedded;
using NUnit.Framework;

namespace MassTransit.Persistence.MongoDb.Tests
{
    //This test harness is based off of Chris Pattersons test harness for Nhibernate.
    [TestFixture, Explicit]
    public class WhenUsingAutomatonymousMongoDbRepository
    {
        private Guid _correlationId;
        private MongoDatabase _db;
        private SuperShopper _machine;
        private ISagaRepository<ShoppingChore> _repository;
        private EmbeddedMongoDbServer _server;
        private SagaTest<BusTestScenario, ShoppingChore> _test;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _server = new EmbeddedMongoDbServer();
            var mongoClient = _server.Client;

            //var mongoClient = new MongoClient("mongodb://localhost:27017");

            // requires an instance of mongodb running at localhost
            _db = mongoClient.GetServer().GetDatabase("MassTransit-SagaTest");

            _machine = new SuperShopper();

            _repository = new MongoDbAutomatonymousSagaRepository<ShoppingChore, SuperShopper>(_db, _machine);

            _correlationId = NewId.NextGuid();

            _test = TestFactory.ForSaga<ShoppingChore>().New(x =>
            {
                x.UseStateMachineBuilder(_machine);
                
                x.UseSagaRepository(_repository);

                x.Publish(new GirlfriendYelling
                {
                    CorrelationId = _correlationId
                });

                x.Publish(new GotHitByACar
                {
                    CorrelationId = _correlationId
                });
            });

            _test.Execute();
        }

        [TestFixtureTearDown]
        public void Teardown()
        {
            _test.Dispose();
            _server.Dispose();
        }

        [Test]
        public void Should_have_a_saga()
        {
            var shoppingChore = _test.Saga.Created.Contains(_correlationId);
            Assert.IsNotNull(shoppingChore);
        }

        [Test]
        public void Should_have_a_saga_in_the_proper_state()
        {
            var shoppingChore = _test.Saga.ContainsInState(_correlationId, _machine.Final, _machine);

            foreach (var result in _repository.Select(x => x))
                Console.WriteLine("{0} - {1} ({2})", result.CorrelationId, result.CurrentState, result.Screwed);

            Assert.IsNotNull(shoppingChore);
        }

        [Test]
        public void Should_have_heard_girlfriend_yelling()
        {
            Assert.IsTrue(_test.Received.Any<GirlfriendYelling>());
        }

        [Test]
        public void Should_have_heard_her_yelling_to_the_end_of_the_world()
        {
            var shoppingChore = _test.Saga.Created.Any(x => x.CorrelationId == _correlationId && x.Screwed);
            Assert.IsNotNull(shoppingChore);
        }

        [Test]
        public void Should_have_heard_the_impact()
        {
            Assert.IsTrue(_test.Received.Any<GotHitByACar>());
        }

        /// <summary>
        ///     Why to exit the door to go shopping
        /// </summary>
        private class GirlfriendYelling :
            CorrelatedBy<Guid>
        {
            public Guid CorrelationId { get; set; }
        }

        private class GotHitByACar :
            CorrelatedBy<Guid>
        {
            public Guid CorrelationId { get; set; }
        }

        private class ShoppingChore : SagaStateMachineInstance
        {
            [Obsolete("for serialization")]
            protected ShoppingChore()
            {
            }

            public ShoppingChore(Guid correlationId)
            {
                CorrelationId = correlationId;
            }

            public State CurrentState { get; set; }
            public CompositeEventStatus Everything { get; set; }
            public bool Screwed { get; set; }
            public Guid CorrelationId { get; set; }
            public IServiceBus Bus { get; set; }
        }

        private class SuperShopper :
            AutomatonymousStateMachine<ShoppingChore>
        {
            public SuperShopper()
            {
                InstanceState(x => x.CurrentState);

                State(() => OnTheWayToTheStore);

                Event(() => ExitFrontDoor);
                Event(() => GotHitByCar);

                Event(() => EndOfTheWorld, x => x.Everything, ExitFrontDoor, GotHitByCar);

                Initially(
                    When(ExitFrontDoor)
                        .Then(state => Console.Write("Leaving!"))
                        .TransitionTo(OnTheWayToTheStore));

                During(OnTheWayToTheStore,
                    When(GotHitByCar)
                        .Then(state => Console.WriteLine("Ouch!!"))
                        .Finalize());

                DuringAny(
                    When(EndOfTheWorld)
                        .Then(state => Console.WriteLine("Screwed!!"))
                        .Then(state => state.Screwed = true));
            }

            public Event<GirlfriendYelling> ExitFrontDoor { get; private set; }
            public Event<GotHitByACar> GotHitByCar { get; private set; }
            public Event EndOfTheWorld { get; private set; }
            public State OnTheWayToTheStore { get; private set; }
        }
    }
}