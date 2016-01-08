using System;
using System.Text;
using System.Threading;

namespace OrderMatchingEngine.OrderBook
{
    public class Trade
    {
        private static Int64 GlobalTradeId;

        public Trade(int instrument, UInt64 quantity, Decimal price, long askorderid, long bidorderid)
            : this()
        {
            if (instrument == null) throw new ArgumentNullException("instrument");
            if (quantity <= 0) throw new ArgumentException("a trade cannot be created with a quantity cannot less than or equal to 0", "quantity");
            if (price <= 0) throw new ArgumentException("a trade cannot be created with a price cannot less than or equal to 0", "price");

            Instrument = instrument;
            Quantity = quantity;
            Price = price;
            this.AskOrderId = askorderid;
            this.BidOrderId = bidorderid;
        }

        private Trade()
        {
            Id = Interlocked.Increment(ref GlobalTradeId);
            CreationTime = DateTime.Now;
        }

        public int Instrument { get; private set; }
        public UInt64 Quantity { get; private set; }
        public Decimal Price { get; private set; }
        public Int64 Id { get; private set; }
        public DateTime CreationTime { get; private set; }

        public override string ToString()
        {
            var s = new StringBuilder(Instrument.ToString());
            s.AppendFormat(" {0} {1} ", Quantity, Price);
            return s.ToString();
        }

        public long BidOrderId { get; set; }

        public long AskOrderId { get; set; }
    }
}
