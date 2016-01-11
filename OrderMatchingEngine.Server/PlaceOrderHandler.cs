using NServiceBus;
using OrderMatchingEngine.Exchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderMatchingEngine.Server
{
    public class PlaceOrderHandler:IHandleMessages<Messages.PlaceOrder>
    {
    
        public void Handle(Messages.PlaceOrder message)
        {

        }
    }
}
