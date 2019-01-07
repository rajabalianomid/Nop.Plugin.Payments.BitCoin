using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.BitCoin.Models
{
    public class BitCoinAddressModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public decimal Balance { get; set; }
        public string PrivateKey { get; set; }
    }
}
