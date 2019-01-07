using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Plugin.Payments.BitCoin.Data;
using Nop.Plugin.Payments.BitCoin.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.BitCoin.Repository
{
    public class BitCoinAddressRepository : IBitCoinAddressRepository
    {
        IDbContext _context;
        public BitCoinAddressRepository(IDbContext context)
        {
            _context = EngineContext.Current.Resolve<BitCoinContext>();
        }

        public virtual IPagedList<BitCoinAddresses> SearchBitCoinAddresses(int vendorId = 0, string publicKey = null, int orderId = 0, List<int> psIds = null, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = _context.Set<BitCoinAddresses>().AsQueryable();
            var order = _context.Set<Order>().AsQueryable();

            if (vendorId > 0)
            {
                query = query.Join(order, bca => bca.OrderId, o => o.Id, (bca, o) => new { bca, o }).Where(w => w.o.OrderItems.Any(orderItem => orderItem.Product.VendorId == vendorId)).Select(s => s.bca);
            }
            if (publicKey != null)
            {
                query = query.Where(o => o.PublicKey == publicKey);
            }
            //if (privateKey != null)
            //{
            //    query = query.Where(o => o.PrivateKey == privateKey);
            //}
            if (orderId > 0)
            {
                query = query.Where(o => o.OrderId == orderId);
            }
            if (psIds != null && psIds.Any())
            {
                query = query.Join(order, bca => bca.OrderId, o => o.Id, (bca, o) => new { bca, o }).Where(w => psIds.Contains(w.o.PaymentStatusId)).Select(s => s.bca);
            }

            query = query.OrderByDescending(o => o.Time);

            //database layer paging
            return new PagedList<BitCoinAddresses>(query, pageIndex, pageSize);
        }
    }
}
