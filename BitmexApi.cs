using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BitmexApi
{
    public class BitMEXApi
    {
        private const string _domain = "https://www.bitmex.com";
        private string _apiKey;
        private string _apiSecret;

        public BitMEXApi(string bitmexKey = "", string bitmexSecret = "") {
            _apiKey = bitmexKey;
            _apiSecret = bitmexSecret;
        }

        private string buildQueryData(Dictionary<string, string> param) {
            if (param == null) return "";
            return string.Join("&", param.Select(p => $"{p.Key}={WebUtility.UrlEncode(p.Value)}"));
        }

        private string buildJSON(Dictionary<string, string> param) {
            if (param == null) return "";
            return "{" + string.Join(",", param.Select(p => $"{p.Key}:{p.Value}")) + "}";
        }

        private long getNonce() {
            DateTime yearBegin = new DateTime(1990, 1, 1);
            return DateTime.UtcNow.Ticks - yearBegin.Ticks;
        }

        private string bytesToHexString(IEnumerable<byte> bs) {
            return string.Join("", bs.Select(b => b.ToString("x2")));
        }

        private async Task<string> queryAsync(string method, string function, Dictionary<string, string> param = null, bool auth = false, bool json = false) {
            string paramData = json ? buildJSON(param) : buildQueryData(param);
            string url = "/api/v1" + function + ((method == "GET" && paramData != "") ? "?" + paramData : "");
            string postData = (method != "GET") ? paramData : "";

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_domain + url);
            webRequest.Method = method;

            if (auth) {
                string nonce = getNonce().ToString();
                string message = method + url + nonce + postData;
                byte[] signatureBytes = hmacsha256(Encoding.UTF8.GetBytes(_apiSecret), Encoding.UTF8.GetBytes(message));
                string signatureString = bytesToHexString(signatureBytes);

                webRequest.Headers.Add("api-nonce", nonce);
                webRequest.Headers.Add("api-key", _apiKey);
                webRequest.Headers.Add("api-signature", signatureString);
            }

            try {
                if (postData != "") {
                    webRequest.ContentType = json ? "application/json" : "application/x-www-form-urlencoded";
                    var data = Encoding.UTF8.GetBytes(postData);
                    using (var stream = webRequest.GetRequestStream()) {
                        stream.Write(data, 0, data.Length);
                    }
                }

                using (WebResponse webResponse = await webRequest.GetResponseAsync())
                using (Stream str = webResponse.GetResponseStream())
                using (StreamReader sr = new StreamReader(str)) {
                    return sr.ReadToEnd();
                }
            } catch (WebException wex) {
                throw new BitmexWebException(wex);
            }
        }

        public async Task<IEnumerable<MarginData>> GetMarginsAsync(string currency = "all") {
            var param = new Dictionary<string, string> {
                {"currency", currency}
            };
            return JsonConvert.DeserializeObject<IEnumerable<MarginData>>(await queryAsync("GET", "/user/margin", param, true));
        }

        public async Task<WalletData> GetWalletAsync(string currency = "XBt") {
            var param = new Dictionary<string, string> {
                {"currency", currency}
            };
            return JsonConvert.DeserializeObject<WalletData>(await queryAsync("GET", "/user/wallet", param, true));
        }

        private byte[] hmacsha256(byte[] keyByte, byte[] messageBytes) {
            using (var hash = new HMACSHA256(keyByte)) {
                return hash.ComputeHash(messageBytes);
            }
        }
    }

    public class BitmexWebException : Exception {
        public string Response { get; private set; }

        public BitmexWebException(WebException wex) : base(getMessage(wex), wex) { }

        private static string getMessage(WebException wex) {
            try {
                using (HttpWebResponse response = (HttpWebResponse)wex.Response) {
                    if (response == null) {
                        return $"{wex.Message} - No response data received";
                    }

                    using (Stream str = response.GetResponseStream())
                    using (StreamReader sr = new StreamReader(str)) {
                        return $"{wex.Message}\r\n{sr.ReadToEnd()}";
                    }
                }
            } catch (Exception ex) {
                return $"Problem processing exception when getting response for exception message\r\n{ex.Message}";
            }
        }
    }
}
