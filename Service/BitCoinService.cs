using NBitcoin;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.BitCoin.Domain;
using Nop.Plugin.Payments.BitCoin.Models;
using Nop.Plugin.Payments.BitCoin.Repository;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Services.Payments;
using QBitNinja.Client;
using QBitNinja.Client.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.BitCoin.Service
{
    public interface IBitCoinService
    {
        decimal CheckPayment(string publicKey);
        string GenerateAddress(int orderId, decimal price);
        string GetByPublickKey(string publicKey);
        BitCoinAddresses GetAddressByOrderId(int id);
        BitCoinAddresses GetAddressByPublicKey(string key);
        string UpdateAmount(int orderId, decimal price);
        IPagedList<BitCoinAddresses> SearchBitCoinAddresses(int vendorId = 0, string publicKey = null, int orderId = 0, List<int> psIds = null, int pageIndex = 0, int pageSize = int.MaxValue);
        List<string> GetPublickKeyByIdes(List<int> ides);
        void HashAll();
        void DHashAll();
    }
    public class BitCoinService : IBitCoinService
    {
        private readonly ISettingService _settingService;
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<BitCoinAddresses> _bcAdressesRepository;
        private readonly IBitCoinAddressRepository _bcRepository;

        public bool IsHash { get { return _settingService.LoadSetting<BitCoinPaymentSettings>().HashBCPrivateKey; } }

        public BitCoinService(ISettingService settingService, IRepository<Order> orderRepository, IRepository<BitCoinAddresses> bcAdressesRepository, IBitCoinAddressRepository bcRepository)
        {
            this._settingService = settingService;
            this._orderRepository = orderRepository;
            this._bcAdressesRepository = bcAdressesRepository;
            this._bcRepository = bcRepository;
        }

        public string GetByPublickKey(string publicKey)
        {
            var found = _bcAdressesRepository.Table.FirstOrDefault(w => w.PublicKey == publicKey);
            if (found != null)
            {
                return IsHash ? BitCoinHelper.StringCipher.Encrypt(found.PrivateKey, found.PublicKey) : found.PrivateKey;
            }
            return null;
        }

        public decimal CheckPayment(string publicKey)
        {
            var paidBalanceOperations = new BitCoinHelper().GetPaidBalanceOperations(GetByPublickKey(publicKey));
            var transactions = paidBalanceOperations.ToList();
            var result = transactions.Select(s => s.Amount).DefaultIfEmpty(0).Sum(s => s);
            var foundBCAddresses = _bcAdressesRepository.Table.FirstOrDefault(w => w.PublicKey == publicKey);
            if (foundBCAddresses != null && result >= foundBCAddresses.Price)
            {
                var foundOrder = _orderRepository.Table.FirstOrDefault(f => f.Id == foundBCAddresses.OrderId);
                if (foundOrder != null)
                {
                    foundOrder.PaymentStatus = Core.Domain.Payments.PaymentStatus.Paid;
                    _orderRepository.Update(foundOrder);
                }
                return foundBCAddresses.Price;
            }
            else if (result > 0 && result < foundBCAddresses.Price)
            {
                return result - foundBCAddresses.Price;
            }
            return 0;
        }

        public string GenerateAddress(int orderId, decimal price)
        {
            var bcAddresses = new BitCoinHelper().GenerateAddress();

            _bcAdressesRepository.Insert(new BitCoinAddresses
            {
                PrivateKey = IsHash ? BitCoinHelper.StringCipher.Encrypt(bcAddresses.PrivateKey, bcAddresses.Id) : bcAddresses.PrivateKey,
                PublicKey = bcAddresses.Id,
                OrderId = orderId,
                Price = price,
                Time = DateTime.Now
            });
            return bcAddresses.Id;
        }

        public BitCoinAddresses GetAddressByOrderId(int id)
        {
            var result = _bcAdressesRepository.Table.FirstOrDefault(w => w.OrderId == id);
            if (result != null)
            {
                result.PrivateKey = IsHash ? BitCoinHelper.StringCipher.Decrypt(result.PrivateKey, result.PublicKey) : result.PrivateKey;
            }

            return result;
        }

        public BitCoinAddresses GetAddressByPublicKey(string key)
        {
            var result = _bcAdressesRepository.Table.FirstOrDefault(w => w.PublicKey == key);
            result.PrivateKey = IsHash ? BitCoinHelper.StringCipher.Decrypt(result.PrivateKey, result.PublicKey) : result.PrivateKey;
            return result;
        }

        public string UpdateAmount(int orderId, decimal price)
        {
            var bcAddresses = _bcAdressesRepository.Table.FirstOrDefault(f => f.OrderId == orderId);
            if (bcAddresses != null)
            {
                bcAddresses.Price = price;
            }
            _bcAdressesRepository.Update(bcAddresses);
            return bcAddresses.PublicKey;
        }

        public virtual IPagedList<BitCoinAddresses> SearchBitCoinAddresses(int vendorId = 0, string publicKey = null, int orderId = 0, List<int> psIds = null, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var result = _bcRepository.SearchBitCoinAddresses(vendorId, publicKey, orderId, psIds, pageIndex, pageSize);
            foreach (var item in result)
            {
                item.PrivateKey = (IsHash ? BitCoinHelper.StringCipher.Decrypt(item.PrivateKey, item.PublicKey) : item.PrivateKey);
            }
            return result;
        }

        public List<string> GetPublickKeyByIdes(List<int> ides)
        {
            return _bcAdressesRepository.Table.Where(w => ides.Any(a => a == w.Id)).Select(s => s.PublicKey).ToList();
        }

        public void HashAll()
        {
            var all = _bcAdressesRepository.Table.ToList();
            all.ForEach(s =>
            {
                s.PrivateKey = (IsHash ? BitCoinHelper.StringCipher.Encrypt(s.PrivateKey, s.PublicKey) : s.PrivateKey);
            });
            _bcAdressesRepository.Update(all);
        }

        public void DHashAll()
        {
            var all = _bcAdressesRepository.Table.ToList();
            all.ForEach(s => s.PrivateKey = BitCoinHelper.StringCipher.Decrypt(s.PrivateKey, s.PublicKey));
            _bcAdressesRepository.Update(all);
        }
    }
}
