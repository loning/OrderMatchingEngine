using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderMatchingEngine.Messages
{
    public class Trade
    {
        public long Id { get; set; }

        public int MarketId { get; set; }

        public decimal Price { get; set; }

        public ulong Quantity { get; set; }

        public int AskOrderId { get; set; }

        public int BidOrderId { get; set; }

    }
}
