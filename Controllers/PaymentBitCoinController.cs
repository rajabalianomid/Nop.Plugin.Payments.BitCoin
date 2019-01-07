using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.BitCoin.Models;
using Nop.Plugin.Payments.BitCoin.Service;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Framework.Controllers;
using Nop.Services.Security;
using Nop.Web.Framework.Kendoui;

namespace Nop.Plugin.Payments.BitCoin.Controllers
{
    public class PaymentBitCoinController : BasePaymentController
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

        public PaymentBitCoinController(IWorkContext workContext,
            IStoreService storeService,
            ISettingService settingService,
            IStoreContext storeContext,
            ILocalizationService localizationService,
            ILanguageService languageService,
            IBitCoinService bitCoinService,
            IShoppingCartModelFactory shoppingCartModelFactory,
            IOrderService orderService,
            IPermissionService permissionService)
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
        }

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var bcPaymentSettings = _settingService.LoadSetting<BitCoinPaymentSettings>(storeScope);

            var model = new ConfigurationModel();
            model.DescriptionText = bcPaymentSettings.DescriptionText;
            //locales
            AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.DescriptionText = bcPaymentSettings.GetLocalizedSetting(x => x.DescriptionText, languageId, 0, false, false);
            });
            model.AdditionalFee = bcPaymentSettings.AdditionalFee;

            model.HashBCPrivateKey = bcPaymentSettings.HashBCPrivateKey;

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.DescriptionText_OverrideForStore = _settingService.SettingExists(bcPaymentSettings, x => x.DescriptionText, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(bcPaymentSettings, x => x.AdditionalFee, storeScope);
            }

            return View("~/Plugins/Payments.BitCoin/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var bcPaymentSettings = _settingService.LoadSetting<BitCoinPaymentSettings>(storeScope);

            //save settings
            bcPaymentSettings.DescriptionText = model.DescriptionText;
            bcPaymentSettings.AdditionalFee = model.AdditionalFee;
            bcPaymentSettings.HashBCPrivateKey = model.HashBCPrivateKey;



            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(bcPaymentSettings, x => x.DescriptionText, model.DescriptionText_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(bcPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(bcPaymentSettings, x => x.HashBCPrivateKey, false, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            if (model.HashBCPrivateKey)
            {
                _bitCoinService.HashAll();
            }
            else
            {
                _bitCoinService.DHashAll();
            }

            //localization. no multi-store support for localization yet.
            foreach (var localized in model.Locales)
            {
                bcPaymentSettings.SaveLocalizedSetting(x => x.DescriptionText,
                    localized.LanguageId,
                    localized.DescriptionText);
            }

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var bcPaymentSettings = _settingService.LoadSetting<BitCoinPaymentSettings>(_storeContext.CurrentStore.Id);
            var model = new PaymentInfoModel
            {
                DescriptionText = bcPaymentSettings.GetLocalizedSetting(x => x.DescriptionText, _workContext.WorkingLanguage.Id, _storeContext.CurrentStore.Id)
            };

            return View("~/Plugins/Payments.BitCoin/Views/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        public ActionResult Index(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.EnableShoppingCart))
                return RedirectToRoute("HomePage");

            var model = new PaymentInfoModel();
            string address = null;
            var foundOrder = _orderService.GetOrderById(id);
            if (foundOrder != null)
            {
                var foundAddress = _bitCoinService.GetAddressByOrderId(foundOrder.Id);
                model.Amount = new BitCoinHelper().BitCoinPerDollor((double)foundOrder.OrderTotal);
                if (foundAddress == null)
                {
                    address = _bitCoinService.GenerateAddress(id, model.Amount);
                }
                else if (foundOrder.PaymentStatus == Core.Domain.Payments.PaymentStatus.Pending)
                {
                    var checkPartially = _bitCoinService.CheckPayment(foundAddress.PublicKey);
                    if (checkPartially < 0)
                    {
                        model.Amount = checkPartially * -1;
                        address = foundAddress.PublicKey;
                    }
                    else
                        address = _bitCoinService.UpdateAmount(id, model.Amount);
                }
                model.Address = address;
                model.Id = id;
                if (string.IsNullOrEmpty(address))
                {
                    return Content("Redirect...");
                }
                return View("~/Plugins/Payments.BitCoin/Views/BitCoinPayment.cshtml", model);
            }
            return Content("Redirect...");
        }

        public ActionResult CallBack(string toaddress)
        {
            string result = "error";
            if (_permissionService.Authorize(StandardPermissionProvider.EnableShoppingCart))
            {
                decimal price = _bitCoinService.CheckPayment(toaddress);
                result = price != 0 ? "payment_received" : "Ok";
                if (price < 0)
                {
                    result = "less";
                }
            }
            return Json(new { status = result }, JsonRequestBehavior.AllowGet);
        }
    }
}