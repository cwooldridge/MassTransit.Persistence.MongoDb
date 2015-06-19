using System;

namespace MassTransit.Persistence.MongoDb.Tests.Sagas
{
    public class CreateAuction : CorrelatedBy<Guid>
    {
        public CreateAuction(Guid correlationId)
        {
            this.CorrelationId = correlationId;
        }

        public Guid CorrelationId { get; set; }

        public string Title { get; set; }

        public string OwnerEmail { get; set; }

        public decimal OpeningBid { get; set; }
    }
}