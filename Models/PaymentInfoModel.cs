using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.BitCoin.Models
{
    public class PaymentInfoModel : BaseNopModel
    {
        public string DescriptionText { get; set; }

        public string Address { get; set; }

        public decimal Amount { get; set; }

        public int Id { get; set; }
    }
}