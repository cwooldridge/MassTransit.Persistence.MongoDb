using System;

using Magnum.StateMachine;

using MassTransit.Saga;

using Serilog;

namespace MassTransit.Persistence.MongoDb.Tests.Sagas
{
    public class AuctionSaga : SagaStateMachine<AuctionSaga>, ISaga
    {
        public static readonly ILogger Logger = Log.Logger.ForContext<AuctionSaga>();

        static AuctionSaga()
        {
            Define(
                () =>
                    {
                        Correlate(Bid).By((saga, message) => saga.CorrelationId == message.AuctionId);
                        Initially(
                            When(Create).Then(
                                (saga, message) =>
                                    {
                                        saga.OpeningBid = message.OpeningBid;
                                        saga.OwnerEmail = message.OwnerEmail;
                                        saga.Title = message.Title;
                                    }).TransitionTo(Open));
                        During(Open, When(Bid).Call((saga, message) => saga.Handle(message)));
                    });
        }

        public AuctionSaga(Guid correlationId)
        {
            this.CorrelationId = correlationId;
        }

        public decimal? CurrentBid { get; set; }

        public string HighBidder { get; set; }

        public Guid HighBidId { get; set; }

        public decimal OpeningBid { get; set; }

        public string OwnerEmail { get; set; }

        public string Title { get; set; }

        public static State Initial { get; set; }

        public static State Completed { get; set; }

        public static State Open { get; set; }

        public static State Closed { get; set; }

        public static Event<CreateAuction> Create { get; set; }

        public static Event<PlaceBid> Bid { get; set; }

        public Guid CorrelationId { get; set; }

        public IServiceBus Bus { get; set; }

        private void Handle(PlaceBid bid)
        {
            if (!this.CurrentBid.HasValue || bid.MaximumBid > this.CurrentBid)
            {
                if (this.HighBidder != null)
                {
                    this.Bus.Publish(new Outbid(this.HighBidId));
                }
                this.CurrentBid = bid.MaximumBid;
                this.HighBidder = bid.BidderEmail;
                this.HighBidId = bid.BidId;
            }
            else
            {
                // already outbid
                this.Bus.Publish(new Outbid(bid.BidId));
            }
        }
    }
}