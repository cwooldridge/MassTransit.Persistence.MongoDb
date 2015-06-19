// MassTransit.Persistence.MongoDb.Tests - Copyright (c) 2015 CaptiveAire

using System;

namespace MassTransit.Persistence.MongoDb.Tests.Sagas
{
    public class Outbid
    {
        public Outbid(Guid bidId)
        {
            this.BidId = bidId;
        }

        public Guid BidId { get; set; }
    }
}