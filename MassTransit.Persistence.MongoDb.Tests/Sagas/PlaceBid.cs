using System;

namespace MassTransit.Persistence.MongoDb.Tests.Sagas
{

    public class PlaceBid
    {
        public Guid BidId { get; set; }

        public Guid AuctionId { get; set; }

        public decimal MaximumBid { get; set; }

        public string BidderEmail { get; set; }
    }
}