﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace OrderMatchingEngine.OrderBook
{
    public class OrderBook
    {
        private OrderProcessor m_OrderProcessingStrategy;
        private Object m_Locker = new Object();

        public Instrument Instrument { get; private set; }
        public BuyOrders BuyOrders { get; private set; }
        public SellOrders SellOrders { get; private set; }
        public Trades Trades { get; private set; }

        public OrderProcessor OrderProcessingStrategy
        {
            get { return m_OrderProcessingStrategy; }
            set
            {

                lock (m_Locker)
                {
                    DedicatedThreadOrderProcessor dedicatedThreadOrderProcessor = m_OrderProcessingStrategy as DedicatedThreadOrderProcessor;

                    if (dedicatedThreadOrderProcessor != null)
                        dedicatedThreadOrderProcessor.Stop();

                    m_OrderProcessingStrategy = value;
                }
            }
        }

        public OrderBook(Instrument instrument, BuyOrders buyOrders, SellOrders sellOrders, Trades trades,
                         OrderProcessor orderProcessingStrategy)
        {
            if (instrument == null) throw new ArgumentNullException("instrument");
            if (buyOrders == null) throw new ArgumentNullException("buyOrders");
            if (sellOrders == null) throw new ArgumentNullException("sellOrders");
            if (trades == null) throw new ArgumentNullException("trades");
            if (orderProcessingStrategy == null) throw new ArgumentNullException("orderProcessingStrategy");
            if (!(instrument == buyOrders.Instrument && instrument == sellOrders.Instrument))
                throw new ArgumentException("instrument does not match buyOrders and sellOrders instrument");

            Instrument = instrument;
            BuyOrders = buyOrders;
            SellOrders = sellOrders;
            Trades = trades;
            OrderProcessingStrategy = orderProcessingStrategy;
        }

        public OrderBook(Instrument instrument)
            : this(instrument, new BuyOrders(instrument), new SellOrders(instrument), new Trades(instrument))
        {
        }

        public OrderBook(Instrument instrument, BuyOrders buyOrders, SellOrders sellOrders, Trades trades)
            : this(
                instrument, buyOrders, sellOrders, trades, new SynchronousOrderProcessor(buyOrders, sellOrders, trades))
        {
        }

        public void InsertOrder(Order order)
        {
            if (order == null) throw new ArgumentNullException("order");
            if (order.Instrument != this.Instrument)
                throw new OrderIsNotForThisBookException();

            //the strategy can change at runtime so lock here and in OrderProcessingStrategy property
            lock (m_Locker)
                this.OrderProcessingStrategy.InsertOrder(order);
        }

        public class OrderIsNotForThisBookException : Exception
        {
        }

        public abstract class OrderProcessor
        {
            public delegate bool OrderMatcher(Order order, Orders orders, out Trade createdTrade);

            public static bool TryMatchOrder(Order order, Orders orders, out Trade createdTrade)
            {
                createdTrade = null;
                List<Order> candidateOrders = order.BuySell == Order.BuyOrSell.Buy
                                                         ? new List<Order>(orders.FindAll(o => o.Price <= order.Price))
                                                         : new List<Order>(orders.FindAll(o => o.Price >= order.Price));
                if (candidateOrders.Count == 0)
                    return false;

                ulong total = 0;

                foreach (var candidateOrder in candidateOrders)
                {
                        var quantity = candidateOrder.Quantity;

                        candidateOrder.Quantity -= order.Quantity;
                        order.Quantity -= quantity;
                        
                        total += quantity;

                        if(candidateOrder.Quantity == 0)
                            orders.Remove(candidateOrder);

                        if(order.Quantity == 0)
                        {
                            createdTrade = new Trade(order.Instrument, total, candidateOrder.Price);
                            break;
                        }
                }
                return true;
            }

            protected BuyOrders m_BuyOrders;
            protected SellOrders m_SellOrders;
            protected Trades m_Trades;

            public OrderMatcher TryMatchBuyOrder { get; set; }
            public OrderMatcher TryMatchSellOrder { get; set; }



            public OrderProcessor(BuyOrders buyOrders, SellOrders sellOrders, Trades trades, OrderMatcher tryMatchBuyOrder, OrderMatcher tryMatchSellOrder)
            {
                m_BuyOrders = buyOrders;
                m_SellOrders = sellOrders;
                m_Trades = trades;
                TryMatchBuyOrder = tryMatchBuyOrder;
                TryMatchSellOrder = tryMatchSellOrder;
            }

            public OrderProcessor(BuyOrders buyOrders, SellOrders sellOrders, Trades trades)
                : this(buyOrders, sellOrders, trades,
                    TryMatchOrder,
                    TryMatchOrder)
            {

            }


            public abstract void InsertOrder(Order order);

            protected void ProcessOrder(Order order)
            {
                Trade trade = null;

                switch (order.BuySell)
                {
                    case Order.BuyOrSell.Buy:
                        if (!TryMatchBuyOrder(order, this.m_SellOrders, out trade))
                            m_BuyOrders.Insert(order);
                        break;
                    case Order.BuyOrSell.Sell:
                        if (!TryMatchSellOrder(order, this.m_BuyOrders, out trade))
                            m_SellOrders.Insert(order);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (trade != null)
                    this.m_Trades.AddTrade(trade);
            }

        }

        public class SynchronousOrderProcessor : OrderBook.OrderProcessor
        {
            public SynchronousOrderProcessor(BuyOrders buyOrders, SellOrders sellOrders, Trades trades)
                : base(buyOrders, sellOrders, trades)
            {
            }

            public override void InsertOrder(Order order)
            {
                ProcessOrder(order);
            }
        }

        public class ThreadPooledOrderProcessor : OrderBook.OrderProcessor
        {
            public ThreadPooledOrderProcessor(BuyOrders buyOrders, SellOrders sellOrders, Trades trades)
                : base(buyOrders, sellOrders, trades)
            {
            }

            public override void InsertOrder(Order order)
            {
                ThreadPool.QueueUserWorkItem((o) => ProcessOrder(order));
            }
        }

        public class DedicatedThreadOrderProcessor : OrderBook.OrderProcessor
        {
            private readonly Thread m_Thread;
            private readonly BlockingCollection<Order> m_PendingOrders = new BlockingCollection<Order>();

            public DedicatedThreadOrderProcessor(BuyOrders buyOrders, SellOrders sellOrders, Trades trades)
                : base(buyOrders, sellOrders, trades)
            {
                m_Thread = new Thread(ProcessOrders);
                m_Thread.Start();
            }


            private void ProcessOrders()
            {
                foreach (var order in m_PendingOrders.GetConsumingEnumerable())
                {
                    ProcessOrder(order);
                }
            }

            public void Stop()
            {
                this.m_PendingOrders.CompleteAdding();
            }

            public override void InsertOrder(Order order)
            {
                m_PendingOrders.Add(order);
            }

        }
    }


}