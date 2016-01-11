using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderMatchingEngine.Messages
{
    public class PlaceOrder:ICommand
    {
        public long Id { get; set; }

        public int MarketId { get; set; }

        /// <summary>
        /// in milionseconds
        /// </summary>
        public long Date { get; set; }

        public decimal Price { get; set; }

        public ulong Quantity { get; set; }
    }
}
