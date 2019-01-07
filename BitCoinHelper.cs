using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nop.Plugin.Payments.BitCoin.Models;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Nop.Core.Infrastructure;
using System.Security.Cryptography;
using System.IO;

namespace Nop.Plugin.Payments.BitCoin
{
    public class BitCoinHelper
    {
        Service.IBitCoinService _bcService;
        Services.Configuration.ISettingService _settingService;

        public BitCoinHelper()
        {
            _bcService = EngineContext.Current.Resolve<Service.IBitCoinService>();
            _settingService = EngineContext.Current.Resolve<Services.Configuration.ISettingService>();
        }

        public bool IsHash { get { return _settingService.LoadSetting<BitCoinPaymentSettings>().HashBCPrivateKey; } }

        public BitCoinAddressModel GenerateAddress()
        {
            Key privateKey = new Key();
            BitcoinSecret bitcoinSecret = privateKey.GetWif(Network.Main);
            Key samePrivateKey = bitcoinSecret.PrivateKey;
            PubKey publicKey = privateKey.PubKey;
            BitcoinPubKeyAddress bitcoinPublicKey = publicKey.GetAddress(Network.Main);
            return new BitCoinAddressModel
            {
                Id = bitcoinPublicKey.ToString(),
                PrivateKey = bitcoinSecret.ToString()
            };
        }
        public BitCoinAddressModel GenerateBitCoinAddressByPrivateKey(string privateKey)
        {
            var bitcoinPrivateKey = new BitcoinSecret(privateKey);
            var address = bitcoinPrivateKey.PubKey.GetAddress(Network.Main).ToString();
            BitCoinAddressModel result = new BitCoinAddressModel();
            if (address != null)
            {
                result = new BitCoinAddressModel
                {
                    Id = address,
                    PrivateKey = privateKey
                };
            }
            return result;
        }
        public Dictionary<Coin, bool> GetUnspentCoins(IEnumerable<Key> privatekeys)
        {
            var unspentCoins = new Dictionary<Coin, bool>();
            foreach (var privatekey in privatekeys)
            {
                var destination = privatekey.ScriptPubKey.GetDestinationAddress(Network.Main);

                var client = new QBitNinjaClient(Network.Main);
                var balanceModel = client.GetBalance(destination, unspentOnly: true).Result;
                foreach (var operation in balanceModel.Operations)
                {
                    foreach (var elem in operation.ReceivedCoins.Select(coin => coin as Coin))
                    {
                        unspentCoins.Add(elem, operation.Confirmations > 0);
                    }
                }
            }

            return unspentCoins;
        }
        private Money ParseBtcString(string value)
        {
            decimal amount;
            if (!decimal.TryParse(value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out amount))
            {

            }
            return new Money(amount, MoneyUnit.BTC);
        }
        public bool SelectCoins(ref HashSet<Coin> coinsToSpend, Money totalOutAmount, List<Coin> unspentCoins)
        {
            var haveEnough = false;
            foreach (var coin in unspentCoins.OrderByDescending(x => x.Amount))
            {
                coinsToSpend.Add(coin);
                // if doesn't reach amount, continue adding next coin
                if (coinsToSpend.Sum(x => x.Amount) < totalOutAmount) continue;
                else
                {
                    haveEnough = true;
                    break;
                }
            }

            return haveEnough;
        }
        public void TransferCoin(List<string> from, string to, double? feeBase = null, double? amount = null)
        {
            try
            {
                Money fee = feeBase == null ? GetFasterFee() : ParseBtcString(feeBase.ToString());
                Money availableAmount = Money.Zero;
                Dictionary<Coin, bool> unspentCoins = null;
                List<Key> secrets = new List<Key>();
                BitcoinAddress address = BitcoinAddress.Create(to, Network.Main);
                Script publickey = null;

                foreach (var item in from)
                {
                    var pvKey = _bcService.GetByPublickKey(item);
                    var foundPrivateKey = IsHash ? StringCipher.Encrypt(pvKey, item) : pvKey;
                    if (foundPrivateKey == null)
                    {
                        throw new Exception("خطا در انتقال مبلغ");
                    }
                    var bitcoinPrivateKey = new BitcoinSecret(foundPrivateKey);
                    publickey = bitcoinPrivateKey.ScriptPubKey;


                    secrets.Add(bitcoinPrivateKey.PrivateKey);
                    unspentCoins = GetUnspentCoins(secrets);
                    var operationsPerNotEmptyPrivateKeys = new Dictionary<BitcoinExtKey, List<BalanceOperation>>();

                    foreach (var elem in unspentCoins)
                    {
                        if (elem.Value)
                        {
                            availableAmount += elem.Key.Amount;
                        }
                    }
                }

                Money amountToSend = null;
                amountToSend = ParseBtcString(amount.ToString());
                if (amount == null)
                {
                    amountToSend = availableAmount - fee;
                }
                else if (availableAmount < amountToSend)
                {
                    throw new Exception("amount is not enough");
                }

                var totalOutAmount = amountToSend + fee;
                var coinsToSpend = new HashSet<Coin>();
                var unspentConfirmedCoins = new List<Coin>();
                var unspentUnconfirmedCoins = new List<Coin>();

                foreach (var elem in unspentCoins)
                    if (elem.Value) unspentConfirmedCoins.Add(elem.Key);
                    else unspentUnconfirmedCoins.Add(elem.Key);

                bool haveEnough = SelectCoins(ref coinsToSpend, totalOutAmount, unspentConfirmedCoins);

                if (!haveEnough)
                    haveEnough = SelectCoins(ref coinsToSpend, totalOutAmount, unspentUnconfirmedCoins);
                if (!haveEnough)
                    throw new Exception("مبلغ کافی نمی باشد");

                var builder = new TransactionBuilder();
                var tx = builder
                    .AddCoins(coinsToSpend)
                    .AddKeys(secrets.ToArray())
                    .Send(address, amountToSend)
                    //.SetChange(publickey)
                    .SendFees(fee)
                    .BuildTransaction(true);

                if (!builder.Verify(tx))
                    throw new Exception("خطا در تراکنش");

                var qBitClient = new QBitNinjaClient(Network.Main);

                BroadcastResponse broadcastResponse;
                var success = false;
                var tried = 0;
                var maxTry = 7;
                do
                {
                    tried++;
                    broadcastResponse = qBitClient.Broadcast(tx).Result;
                    var getTxResp = qBitClient.GetTransaction(tx.GetHash()).Result;
                    if (getTxResp == null)
                    {
                        Thread.Sleep(3000);
                        continue;
                    }
                    else
                    {
                        success = true;
                        break;
                    }
                } while (tried <= maxTry);

                if (!success)
                {
                    if (broadcastResponse.Error != null)
                    {
                        throw new Exception($"Error code: {broadcastResponse.Error.ErrorCode} Reason: {broadcastResponse.Error.Reason}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public decimal GetBalance(string privatekey)
        {
            var bitcoinPrivateKey = new BitcoinSecret(privatekey);
            List<Key> secrets = new List<Key>();
            secrets.Add(bitcoinPrivateKey.PrivateKey);
            var unspentCoins = GetUnspentCoins(secrets);
            return unspentCoins.Sum(s => decimal.Parse(s.Key.Amount.ToString()));
        }
        public decimal BitCoinPerDollor(double usd = 1)
        {
            //var client = new RestClient($"https://blockchain.info/tobtc?currency=USD&value={usd}");
            //var request = new RestRequest(Method.GET);
            //request.AddHeader("post-token", Guid.NewGuid().ToString());
            //request.AddHeader("cache-control", "no-cache");
            //request.AddHeader("content-type", "application/json");
            //IRestResponse response = client.Execute(request);
            //var amount = response.Content;

            var uri = String.Format("https://blockchain.info/tobtc?currency=USD&value={0}", usd);

            WebClient client = new WebClient();
            client.UseDefaultCredentials = true;
            var data = client.DownloadString(uri);
            double result = 0;
            try
            {
                result = Convert.ToDouble(data.Replace(".", "/"));
            }
            catch (FormatException)
            {
                result = Convert.ToDouble(data);
            }

            return (decimal)result;
        }
        public List<BitCoinTransactionDetail> GetPaidBalanceOperations(string privateKey)
        {
            if (privateKey != null)
            {
                List<BitCoinTransactionDetail> result = new List<BitCoinTransactionDetail>();
                var bitcoinPrivateKey = new BitcoinSecret(privateKey);
                List<Key> secrets = new List<Key>();
                secrets.Add(bitcoinPrivateKey.PrivateKey);
                secrets.ForEach(sprivatekey =>
                {
                    var destination = sprivatekey.ScriptPubKey.GetDestinationAddress(Network.Main);
                    var client = new QBitNinjaClient(Network.Main);
                    var balanceModel = client.GetBalance(destination, unspentOnly: true).Result;
                    foreach (var operation in balanceModel.Operations)
                    {
                        var transactionId = operation.TransactionId;
                        // Query the transaction
                        GetTransactionResponse transactionResponse = client.GetTransaction(transactionId).Result;

                        var inputs = transactionResponse.Transaction.Outputs;
                        foreach (var item in inputs)
                        {
                            var paymentScript = item.ScriptPubKey;
                            var address = paymentScript.GetDestinationAddress(Network.Main);
                        }
                        var inputs2 = transactionResponse.Transaction.Inputs;
                        foreach (var item in inputs2)
                        {
                            var paymentScript = item.ScriptSig.Hash.ScriptPubKey;
                            var address = paymentScript.GetDestinationAddress(Network.Main);
                        }


                        List<ICoin> receivedCoins = transactionResponse.SpentCoins;
                        var data = receivedCoins[0];
                        result.Add(new BitCoinTransactionDetail
                        {
                            Address = receivedCoins[0].TxOut.ScriptPubKey.GetDestinationAddress(Network.Main).ToString(),
                            Amount = decimal.Parse(operation.Amount.ToString().Replace(".", "/")),
                            TransactionId = transactionId.ToString()
                        });
                    }
                });
                return result;
            }
            return null;
        }
        public Money GetFasterFee()
        {
            Money fee;
            try
            {
                var txSizeInBytes = 250;
                using (var client = new HttpClient())
                {

                    const string request = @"https://bitcoinfees.21.co/api/v1/fees/recommended";
                    var result = client.GetAsync(request, HttpCompletionOption.ResponseContentRead).Result;
                    var json = JObject.Parse(result.Content.ReadAsStringAsync().Result);
                    var fastestSatoshiPerByteFee = json.Value<decimal>("fastestFee");
                    fee = new Money(fastestSatoshiPerByteFee * txSizeInBytes, MoneyUnit.Satoshi);
                }
            }
            catch
            {
                throw new Exception("Couldn't calculate transaction fee, try it again later.");
            }
            return fee;
        }

        public static class StringCipher
        {
            // This constant is used to determine the keysize of the encryption algorithm in bits.
            // We divide this by 8 within the code below to get the equivalent number of bytes.
            private const int Keysize = 256;

            // This constant determines the number of iterations for the password bytes generation function.
            private const int DerivationIterations = 1000;

            public static string Encrypt(string plainText, string passPhrase)
            {
                // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
                // so that the same Salt and IV values can be used when decrypting.  
                var saltStringBytes = Generate256BitsOfRandomEntropy();
                var ivStringBytes = Generate256BitsOfRandomEntropy();
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                {
                    var keyBytes = password.GetBytes(Keysize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                                {
                                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                    cryptoStream.FlushFinalBlock();
                                    // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                    var cipherTextBytes = saltStringBytes;
                                    cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                    cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Convert.ToBase64String(cipherTextBytes);
                                }
                            }
                        }
                    }
                }
            }

            public static string Decrypt(string cipherText, string passPhrase)
            {
                // Get the complete stream of bytes that represent:
                // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
                var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
                // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
                var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
                // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
                var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
                // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
                var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                {
                    var keyBytes = password.GetBytes(Keysize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream(cipherTextBytes))
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                                {
                                    var plainTextBytes = new byte[cipherTextBytes.Length];
                                    var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                                }
                            }
                        }
                    }
                }
            }

            private static byte[] Generate256BitsOfRandomEntropy()
            {
                var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
                using (var rngCsp = new RNGCryptoServiceProvider())
                {
                    // Fill the array with cryptographically secure random bytes.
                    rngCsp.GetBytes(randomBytes);
                }
                return randomBytes;
            }
        }
    }
}
