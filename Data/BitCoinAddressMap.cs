using Nop.Core.Domain.Orders;
using Nop.Data.Mapping;
using Nop.Plugin.Payments.BitCoin.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.BitCoin.Data
{
    public class BitCoinAddressMap : NopEntityTypeConfiguration<BitCoinAddresses>
    {
        public BitCoinAddressMap()
        {
            this.ToTable("BitCoinAddresses");
            this.HasKey(tr => tr.Id);
            this.Property(p => p.Price).HasPrecision(18,18);
            this.Property(p => p.Time);
        }
    }
}
