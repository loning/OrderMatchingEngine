using System;

namespace OrderMatchingEngine.OrderBook
{
    public class EquityOrder : Order
    {
        public EquityOrder(int instrument, OrderTypes orderType, BuyOrSell buySell, ulong price,
                           UInt64 quantity)
            : base(instrument, orderType, buySell, price, quantity)
        {
        }
    }
}
