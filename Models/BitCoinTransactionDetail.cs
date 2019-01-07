using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.BitCoin.Models
{
    public class BitCoinTransactionDetail
    {
        public decimal Amount { get; set; }
        public string TransactionId { get; set; }
        public string Address { get; set; }
    }
}
