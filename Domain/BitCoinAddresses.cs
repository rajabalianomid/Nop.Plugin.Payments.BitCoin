using Nop.Core;
using Nop.Core.Domain.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.BitCoin.Domain
{
    public class BitCoinAddresses : BaseEntity
    {
        public BitCoinAddresses()
        {
            //Order = new Order();
        }
        public string PublicKey { get; set; }

        public string PrivateKey { get; set; }

        public decimal Price { get; set; }

        public DateTime Time { get; set; }

        public int? OrderId { get; set; }
    }
}
