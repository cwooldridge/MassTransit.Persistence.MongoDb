namespace MassTransit.Persistence.MongoDb.Tests.Sagas
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    using log4net;

    using MassTransit.Saga;

    public class ConcurrentLegacySaga :
        ISaga,
        InitiatedBy<StartConcurrentSaga>,
        Orchestrates<ContinueConcurrentSaga>
    {
        static readonly ILog _log = LogManager.GetLogger(typeof(ConcurrentLegacySaga));


        public ConcurrentLegacySaga(Guid correlationId)
        {
            this.CorrelationId = correlationId;

            this.Value = -1;
        }

        protected ConcurrentLegacySaga()
        {
            this.Value = -1;
        }

        public virtual string Name { get; set; }
        public virtual int Value { get; set; }

        public virtual void Consume(StartConcurrentSaga message)
        {
            Trace.WriteLine("Consuming " + message.GetType());
            Thread.Sleep(3000);
            this.Name = message.Name;
            this.Value = message.Value;
            Trace.WriteLine("Completed " + message.GetType());
        }

        public virtual Guid CorrelationId { get; set; }
        public virtual IServiceBus Bus { get; set; }

        public virtual void Consume(ContinueConcurrentSaga message)
        {
            Trace.WriteLine("Consuming " + message.GetType());
            Thread.Sleep(1000);

            if (this.Value == -1)
                throw new InvalidOperationException("Should not be a -1 dude!!");

            this.Value = message.Value;
            Trace.WriteLine("Completed " + message.GetType());
        }
    }
}