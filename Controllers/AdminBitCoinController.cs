using Nop.Admin.Controllers;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.BitCoin.Models;
using Nop.Plugin.Payments.BitCoin.Service;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Framework.Kendoui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Nop.Plugin.Payments.BitCoin.Controllers
{
    public class AdminBitCoinController : BaseAdminController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly ILanguageService _languageService;
        private readonly IBitCoinService _bitCoinService;
        private readonly IOrderService _orderService;
        private readonly IShoppingCartModelFactory _shoppingCartModelFactory;
        private readonly IPermissionService _permissionService;
        private readonly IDateTimeHelper _dateTimeHelper;

        public AdminBitCoinController(IWorkContext workContext,
            IStoreService storeService,
            ISettingService settingService,
            IStoreContext storeContext,
            ILocalizationService localizationService,
            ILanguageService languageService,
            IBitCoinService bitCoinService,
            IShoppingCartModelFactory shoppingCartModelFactory,
            IOrderService orderService,
            IPermissionService permissionService,
            IDateTimeHelper dateTimeHelper)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._storeContext = storeContext;
            this._localizationService = localizationService;
            this._languageService = languageService;
            this._bitCoinService = bitCoinService;
            this._shoppingCartModelFactory = shoppingCartModelFactory;
            this._orderService = orderService;
            this._permissionService = permissionService;
            this._dateTimeHelper = dateTimeHelper;
        }

        public virtual ActionResult Index()
        {
            return RedirectToAction("List");
        }

        public virtual ActionResult List()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            //order statuses
            var model = new BitCoinAddressListModel();
            //payment statuses
            model.AvailablePaymentStatuses = PaymentStatus.Pending.ToSelectList(false).ToList();
            model.AvailablePaymentStatuses.Insert(0, new SelectListItem
            { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0", Selected = true });
            return View("~/Plugins/Payments.BitCoin/Views/List.cshtml", model);
        }

        [HttpPost]
        public virtual ActionResult BitCoinList(DataSourceRequest command, BitCoinAddressListModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageOrders))
                return AccessDeniedKendoGridJson();

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null)
            {
                model.VendorId = _workContext.CurrentVendor.Id;
            }

            var paymentStatusIds = !model.PaymentStatusIds.Contains(0) ? model.PaymentStatusIds : null;

            //load orders
            var bitCoinAddresses = _bitCoinService.SearchBitCoinAddresses(
                vendorId: model.VendorId,
                publicKey: model.PublicKey,
                //privateKey: model.PrivateKey,
                orderId: model.OrderId,
                psIds: paymentStatusIds,
                pageIndex: command.Page - 1,
                pageSize: command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = bitCoinAddresses.Select(x =>
                {
                    return new BitCoinAddressListModel
                    {
                        Id = x.Id,
                        PublicKey = x.PublicKey,
                        PrivateKey = x.PrivateKey,
                        OrderId = x.OrderId ?? 0,
                        Price = x.Price,
                        PaidPrice = new BitCoinHelper().GetBalance(x.PrivateKey),
                        CreatedOn = _dateTimeHelper.ConvertToUserTime(x.Time, DateTimeKind.Utc)
                    };
                }),
                Total = bitCoinAddresses.TotalCount
            };

            return Json(gridModel);
        }

        public JsonResult Transfer(ICollection<int> selectedIds, string toAddress)
        {
            var foundAddress = _bitCoinService.GetPublickKeyByIdes(selectedIds.ToList());
            if (foundAddress != null && foundAddress.Any())
            {
                new BitCoinHelper().TransferCoin(foundAddress, toAddress);
            }
            return Json(new { Result = true });
        }
    }
}
