using Nop.Web.Framework.Mvc.Routes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;

namespace Nop.Plugin.Payments.BitCoin
{
    public class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("PaymentBitCoin_Root",
                 "BitCoin",
                 new { controller = "AdminBitCoin", action = "Index", area = "Admin" },
                 new[] { "Nop.Plugin.Payments.BitCoin.Controllers" }
            );
            routes.MapRoute("PaymentBitCoin_Index",
                 "BitCoin/index",
                 new { controller = "AdminBitCoin", action = "Index", area = "Admin" },
                 new[] { "Nop.Plugin.Payments.BitCoin.Controllers" }
            );
            routes.MapRoute("PaymentBitCoin_List",
                 "BitCoin/list",
                 new { controller = "AdminBitCoin", action = "List", area = "Admin" },
                 new[] { "Nop.Plugin.Payments.BitCoin.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
