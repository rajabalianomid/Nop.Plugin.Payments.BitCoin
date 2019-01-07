using Nop.Core;
using Nop.Core.Data;
using Nop.Data;
using Nop.Plugin.Payments.BitCoin.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.BitCoin.Repository
{
    public interface IBitCoinAddressRepository
    {
        IPagedList<BitCoinAddresses> SearchBitCoinAddresses(int vendorId = 0, string publicKey = null, int orderId = 0, List<int> psIds = null, int pageIndex = 0, int pageSize = int.MaxValue);
    }
}
