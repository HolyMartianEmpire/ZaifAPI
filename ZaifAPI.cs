using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zaif
{
    public class ZaifAPI
    {
        private string _key;
        private string _secret;
        private string _entry = "https://api.zaif.jp/tapi";

        public ZaifAPI(string key, string secret)
        {
            _key = key;
            _secret = secret;
        }

        protected async Task<string> postSign(string method, NameValueCollection values)
        {
            string param = "";
            string hash = "";

            values.Add("method", method);
            values.Add("nonce", (DateTime.Now.Ticks / 1000000000.0).ToString());

            foreach (var key in values.AllKeys)
            {
                param += key + "=" + WebUtility.UrlEncode(values[key]) + "&";
            }
            param = param.Trim('&');
            byte[] data = Encoding.ASCII.GetBytes(param);
            Debug.WriteLine("{0}", param);

            var hma = new HMACSHA512(Encoding.ASCII.GetBytes(_secret));
            foreach (var b in hma.ComputeHash(data))
            {
                hash += b.ToString("x02");
            }

            WebClient wc = new WebClient();
            wc.Headers.Add("Key", _key);
            wc.Headers.Add("Sign", hash);

            byte[] res = null;
            bool success = false;
            int wait = 0;

            while (!success)
            {
                try
                {
                    res = await wc.UploadDataTaskAsync(new Uri(_entry), data);
                    success = true;
                }
                catch(WebException wex)
                {
                    if( wex.Status == WebExceptionStatus.ProtocolError)
                    {
                        var code = ((HttpWebResponse)wex.Response).StatusCode;
                        switch(code)
                        {
                            case HttpStatusCode.GatewayTimeout:
                            case HttpStatusCode.ServiceUnavailable:
                                wait++;
                                Thread.Sleep(TimeSpan.FromSeconds(5 * wait));
                                Debug.WriteLine("{0}", wait);
                                break;
                        }
                    }
                }
            }            
            return Encoding.UTF8.GetString(res);
        }

        public async Task<string> getInfo()
        {
            var res = await postSign("get_info", new NameValueCollection());
            Debug.WriteLine("{0}", res);

            return res;
        }

        public async Task<string> getLastPrice(string currency)
        {
            WebClient wc = new WebClient();

            string res = await wc.DownloadStringTaskAsync(string.Format("https://api.zaif.jp/api/1/last_price/{0}", currency));
            Debug.WriteLine("{0}", res);

            return res;
        }

        public async Task<string> trade(string currency, string action, decimal price, decimal amount)
        {
            NameValueCollection values = new NameValueCollection();
            values.Add("currency_pair", currency);
            values.Add("action", action);
            values.Add("price", price.ToString());
            values.Add("amount", amount.ToString("0.####"));

            string res = await postSign("trade", values);
            Debug.WriteLine("{0}", res);

            return res;
        }

        public async Task<string> calcelOrder(decimal id)
        {
            NameValueCollection values = new NameValueCollection();
            values.Add("order_id", id.ToString());

            string res = await postSign("cancel_order", values);
            Debug.WriteLine("{0}", res);

            return res;
        }

        public async Task<string> activeOrders(string currency)
        {
            NameValueCollection values = new NameValueCollection();
            values.Add("currency_pair", currency);

            string res = await postSign("active_orders", values);
            Debug.WriteLine("{0}", res);

            return res;
        }

        public async Task<string> tradeHistory(int start)
        {
            NameValueCollection values = new NameValueCollection();
            values.Add("from_id", start.ToString());

            string res = await postSign("trade_history", values);
            Debug.WriteLine("{0}", res);

            return res;
        }

        public async Task<string> getTicker(string currency)
        {
            string uri = string.Format("https://api.zaif.jp/api/1/ticker/{0}", currency);

            HttpClient hc = new HttpClient();

            string res = await hc.GetStringAsync(uri);

            Debug.WriteLine("{0}", res);

            return res;
        }
    }

    public class ZaifApiUwp
    {
        private string _key;
        private string _secret;

        public ZaifApiUwp(string key = "", string secret = "")
        {
            _key = key;
            _secret = secret;
        }

        public async Task<string> getTicker(string currency)
        {
            string uri = string.Format("https://api.zaif.jp/api/1/ticker/{0}", currency);

            HttpClient hc = new HttpClient();

            string res = await hc.GetStringAsync(uri);

            Debug.WriteLine("{0}", res);

            return res;
        }
    }

    public class CurrencyPair
    {
        public const string btc_jpy = "btc_jpy";
        public const string bch_jpy = "bch_jpy";   //ビットキャッシュ
        public const string eth_jpy = "eth_btc";   //イーサリアム
    }

    public class Trade
    {
        public const string sale = "ask";
        public const string buy = "bid";
    }

    [DataContract]
    public class ResultPrice
    {
        [DataMember]
        public decimal last_price;

        public static ResultPrice Parse(string str)
        {
            var ser = new DataContractJsonSerializer(typeof(ResultPrice));
            var res = (ResultPrice)ser.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(str)));

            return res;
        }
    }

    [DataContract]
    public class ResultInfo
    {
        [DataMember]
        public int success;

        [DataMember(Name = "return")]
        public Infomation result;

        public static ResultInfo Parse(string str)
        {
            var ser = new DataContractJsonSerializer(typeof(ResultInfo));
            var res = (ResultInfo)ser.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(str)));

            return res;
        }

        [DataContract]
        public class Infomation
        {
            [DataMember]
            public Balance funds;

            [DataMember]
            public Balance deposit;

            [DataMember]
            public Balance rights;

            [DataMember]
            public int trade_count;

            [DataMember]
            public int open_orders;

            [DataMember]
            public long server_time;
        }

        [DataContract]
        public class Balance
        {
            [DataMember]
            public decimal jpy;

            [DataMember]
            public decimal btc;

            [DataMember]
            public decimal xem;

            [DataMember]
            public decimal mona;
        }
    }

    [DataContract]
    public class ResultHistory
    {

        [DataMember]
        public int success;

        [DataMember(Name = "return")]
        public Dictionary<string, Entry> result;

        public static ResultHistory Parse(string str)
        {
            var settings = new DataContractJsonSerializerSettings() { UseSimpleDictionaryFormat = true };
            var ser = new DataContractJsonSerializer(typeof(ResultHistory), settings);
            var res = (ResultHistory)ser.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(str)));

            return res;
        }

        [DataContract]
        public class Entry
        {
            [DataMember]
            public string currency_pair;

            [DataMember]
            public string action;

            [DataMember]
            public decimal amount;

            [DataMember]
            public decimal price;

            [DataMember]
            public int fee;

            [DataMember]
            public decimal fee_amount;

            [DataMember]
            public string your_action;

            [DataMember]
            public decimal? bonus;

            [DataMember]
            public long timestamp;

            [DataMember]
            public string comment;
        }
    }

    [DataContract]
    public class ResultOrder
    {
        [DataMember]
        public string success;

        [DataMember(Name = "return")]
        public Dictionary<string, Entry> result;

        [DataContract]
        public class Entry
        {
            [DataMember]
            public string currency_pair { get; set; }

            [DataMember]
            public string action { get; set; }

            [DataMember]
            public decimal amount { get; set; }

            [DataMember]
            public decimal price { get; set; }

            [DataMember]
            public long timestamp { get; set; }

            [DataMember]
            public string comment { get; set; }
        }

        public static ResultOrder Parse(string str)
        {
            var settings = new DataContractJsonSerializerSettings() { UseSimpleDictionaryFormat = true };
            var ser = new DataContractJsonSerializer(typeof(ResultOrder), settings);
            var res = (ResultOrder)ser.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(str)));

            return res;
        }

    }

    [DataContract]
    public class ResultTicker
    {
        [DataMember]
        public Decimal last;

        [DataMember]
        public Decimal high;

        [DataMember]
        public Decimal low;

        [DataMember]
        public Decimal vwap;

        [DataMember]
        public Decimal volume;

        [DataMember]
        public Decimal bid;

        [DataMember]
        public Decimal ask;

        public static ResultTicker Parse(string str)
        {
            var settings = new DataContractJsonSerializerSettings() { UseSimpleDictionaryFormat = true };
            var ser = new DataContractJsonSerializer(typeof(ResultTicker), settings);
            var res = (ResultTicker)ser.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(str)));

            return res;
        }
    }

}
