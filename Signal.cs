using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MastercardHost
{
    public class Signal
    {
        public string signalType {  get; set; }
        public List<SignalData> signalData { get; set; } = new List<SignalData>();

    }
}
