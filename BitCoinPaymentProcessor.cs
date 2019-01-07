using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.BitCoin.Controllers;
using Nop.Plugin.Payments.BitCoin.Data;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Payments.BitCoin
{
    /// <summary>
    /// CheckMoneyOrder payment processor
    /// </summary>
    public class BitCoinPaymentProcessor : BasePlugin, IPaymentMethod, IAdminMenuPlugin
    {
        #region Fields

        private readonly BitCoinPaymentSettings _checkMoneyOrderPaymentSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ISettingService _settingService;
        private readonly HttpContextBase _httpContext;
        private readonly BitCoinContext _objectContext;

        #endregion

        #region Ctor

        public BitCoinPaymentProcessor(BitCoinPaymentSettings checkMoneyOrderPaymentSettings,
            ILocalizationService localizationService,
            IOrderTotalCalculationService orderTotalCalculationService,
            ISettingService settingService,
            HttpContextBase httpContext,
            BitCoinContext objectContext)
        {
            this._checkMoneyOrderPaymentSettings = checkMoneyOrderPaymentSettings;
            this._localizationService = localizationService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._settingService = settingService;
            this._httpContext = httpContext;
            this._objectContext = objectContext;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;
            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            _httpContext.Response.Redirect($"/PaymentBitCoin/Index/{postProcessPaymentRequest.Order.Id}");
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country

            if (_checkMoneyOrderPaymentSettings.ShippableProductRequired && !cart.RequiresShipping())
                return true;

            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
                _checkMoneyOrderPaymentSettings.AdditionalFee, _checkMoneyOrderPaymentSettings.AdditionalFeePercentage);
            return result;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //it's not a redirection payment method. So we always return false
            return false;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentBitCoin";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.BitCoin.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentBitCoin";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.BitCoin.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Get the type of controller
        /// </summary>
        /// <returns>Type</returns>
        public Type GetControllerType()
        {
            return typeof(PaymentBitCoinController);
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //database objects
            _objectContext.Install();


            //settings
            var settings = new BitCoinPaymentSettings
            {
                DescriptionText = "<p>BitCoin Paymnet</p>"
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.Bitcoin.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.Bitcoin.AdditionalFee.Hint", "The additional fee.");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.Bitcoin.DescriptionText", "Description");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.Bitcoin.DescriptionText.Hint", "Enter info that will be shown to customers during checkout");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.Bitcoin.PaymentMethodDescription", "Pay by cheque or money order");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.Bitcoin.CheckoutBitcoinPayment", "BitCoin Gate Pay");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.Bitcoin.Inonepayment", "Pay at the following address in one payment");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.Bitcoin.To", "To address");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.Bitcoin.Openwallet", "Open wallet");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.Bitcoin.Inonepayment.Awaiting.Payment.From.You", "Awaiting for payment");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.Bitcoin.Total", "Total");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.BitCoin.HashBCPrivateKey", "Private Keys encrypt");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.BitCoin.HashBCPrivateKey.Hint", "Private Keys encrypt save on db");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.BitCoin.PublicKey", "public Key");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.BitCoin.PrivateKey", "Private Key");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.BitCoin.OrderId", "Order Id");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.BitCoin.PaymentStatus", "Payment Status");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.BitCoin.ToAddress", "To BC Address");
            this.AddOrUpdatePluginLocaleResource("plugin.payments.bitcoin.price", "Price");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.Bitcoin.PaidPrice", "cash");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.BitCoin.Transfer.Success", "Transfer successfully");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.BitCoin.PrivateKeyShow", "Show PrivateKey");
            this.AddOrUpdatePluginLocaleResource("plugin.payments.bitcoin.list", "Manage BitCoin");
            this.AddOrUpdatePluginLocaleResource("Plugin.Payments.Bitcoin.Transfer.Selected", "Transfer selected bitcoin to address");

            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings

            _objectContext.Uninstall();

            _settingService.DeleteSetting<BitCoinPaymentSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugin.Payments.Bitcoin.AdditionalFee");
            this.DeletePluginLocaleResource("Plugin.Payments.Bitcoin.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugin.Payments.Bitcoin.DescriptionText");
            this.DeletePluginLocaleResource("Plugin.Payments.Bitcoin.DescriptionText.Hint");
            this.DeletePluginLocaleResource("Plugin.Payments.Bitcoin.PaymentMethodDescription");
            this.DeletePluginLocaleResource("Plugin.Payments.Bitcoin.CheckoutBitcoinPayment");
            this.DeletePluginLocaleResource("Plugin.Payments.Bitcoin.Inonepayment");
            this.DeletePluginLocaleResource("Plugin.Payments.Bitcoin.To");
            this.DeletePluginLocaleResource("Plugin.Payments.Bitcoin.Openwallet");
            this.DeletePluginLocaleResource("Plugin.Payments.Bitcoin.Inonepayment.Awaiting.Payment.From.You");
            this.DeletePluginLocaleResource("Plugin.Payments.Bitcoin.Total");
            this.DeletePluginLocaleResource("Plugin.Payments.BitCoin.HashBCPrivateKey");
            this.DeletePluginLocaleResource("Plugin.Payments.BitCoin.HashBCPrivateKey.Hint");
            this.DeletePluginLocaleResource("Plugin.Payments.BitCoin.PublicKey");
            this.DeletePluginLocaleResource("Plugin.Payments.BitCoin.PrivateKey");
            this.DeletePluginLocaleResource("Plugin.Payments.BitCoin.OrderId");
            this.DeletePluginLocaleResource("Plugin.Payments.BitCoin.PaymentStatus");
            this.DeletePluginLocaleResource("Plugin.Payments.BitCoin.ToAddress");
            this.DeletePluginLocaleResource("plugin.payments.bitcoin.price");
            this.DeletePluginLocaleResource("Plugin.Payments.Bitcoin.PaidPrice");
            this.DeletePluginLocaleResource("Plugin.Payments.BitCoin.Transfer.Success");
            this.DeletePluginLocaleResource("Plugin.Payments.BitCoin.PrivateKeyShow");
            this.DeletePluginLocaleResource("plugin.payments.bitcoin.list");
            this.DeletePluginLocaleResource("Plugin.Payments.Bitcoin.Transfer.Selected");

            base.Uninstall();
        }

        public void ManageSiteMap(Web.Framework.Menu.SiteMapNode rootNode)
        {
            var menuItem = new Web.Framework.Menu.SiteMapNode()
            {
                SystemName = "BitCoin",
                Title = "Manage bitcoin",
                ControllerName = "AdminBitCoin",
                ActionName = "Index",
                Visible = true,
                IconClass = "fa-dot-circle-o",
                RouteValues = new RouteValueDictionary() { { "area", null } },
            };

            var pluginNode = rootNode.ChildNodes.FirstOrDefault(x => x.SystemName == "Sales");
            if (pluginNode != null)
                pluginNode.ChildNodes.Add(menuItem);
            else
                rootNode.ChildNodes.Add(menuItem);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.NotSupported; }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get { return PaymentMethodType.Redirection; }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            //return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
            //for example, for a redirection payment method, description may be like this: "You will be redirected to PayPal site to complete the payment"
            get { return _localizationService.GetResource("Plugins.Payment.CheckMoneyOrder.PaymentMethodDescription"); }
        }

        #endregion

    }
}
