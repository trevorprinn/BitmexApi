using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitmexApi {
    public class WalletData {
        // NB There are a lot more fields available
        public int Account { get; set; }
        public string Currency { get; set; }
        public decimal Deposited { get; set; }
        public decimal Withdrawn { get; set; }
        public decimal Amount { get; set; }
    }

    public class MarginData {
        // NB There are a lot more fields available
        public int Account { get; set; }
        public string Currency { get; set; }
    }
}
