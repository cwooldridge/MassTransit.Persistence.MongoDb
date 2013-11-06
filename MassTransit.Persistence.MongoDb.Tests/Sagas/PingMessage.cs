namespace MassTransit.Persistence.MongoDb.Tests.Sagas
{
    using System;

    [Serializable]
    public class PingMessage :
        IEquatable<PingMessage>,
        CorrelatedBy<Guid>
    {
        private Guid _id = new Guid("D62C9B1C-8E31-4D54-ADD7-C624D56085A4");

        public PingMessage()
        {
        }

        public PingMessage(Guid id)
        {
            this._id = id;
        }

        public Guid CorrelationId
        {
            get { return this._id; }
            set { this._id = value; }
        }

        public bool Equals(PingMessage obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj._id.Equals(this._id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(PingMessage)) return false;
            return this.Equals((PingMessage)obj);
        }

        public override int GetHashCode()
        {
            return this._id.GetHashCode();
        }
    }
}