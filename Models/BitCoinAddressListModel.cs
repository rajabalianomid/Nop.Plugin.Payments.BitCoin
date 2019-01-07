using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Nop.Plugin.Payments.BitCoin.Models
{
    public class BitCoinAddressListModel : BaseNopModel
    {
        public BitCoinAddressListModel()
        {
            AvailablePaymentStatuses = new List<SelectListItem>();
            PaymentStatusIds = new List<int>();
        }

        [NopResourceDisplayName("Plugin.Payments.BitCoin.ID")]
        public int Id { get; set; }

        [NopResourceDisplayName("Plugin.Payments.BitCoin.PublicKey")]
        public string PublicKey { get; set; }

        [NopResourceDisplayName("Plugin.Payments.BitCoin.PrivateKey")]
        public string PrivateKey { get; set; }

        [NopResourceDisplayName("Plugin.Payments.BitCoin.Price")]
        public decimal Price { get; set; }

        [NopResourceDisplayName("Plugin.Payments.BitCoin.PaidPrice")]
        public decimal PaidPrice { get; set; }

        [NopResourceDisplayName("Plugin.Payments.BitCoin.OrderId")]
        public int OrderId { get; set; }

        public int VendorId { get; set; }

        [NopResourceDisplayName("Plugin.Payments.BitCoin.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [NopResourceDisplayName("Plugin.Payments.BitCoin.PaymentStatus")]
        [UIHint("MultiSelect")]
        public List<int> PaymentStatusIds { get; set; }

        [NopResourceDisplayName("Plugin.Payments.BitCoin.ToAddress")]
        public string ToAddress { get; set; }

        public IList<SelectListItem> AvailablePaymentStatuses { get; set; }
    }
}
