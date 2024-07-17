using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpSharp;

namespace MastercardHost
{
    internal class DataProcessor
    {
        private readonly MainViewModel _viewModel;

        public DataProcessor(MainViewModel viewModel) 
        {
            _viewModel = viewModel;
        }

        public void Server_OnDataReceived(object sender, OnClientDataReceivedEventArgs e)
        {
            if (!IsServerProtocolValid(e.Data))
            {
                _viewModel.UpdateLogText("[From POS]Invalid protocol received.");
                return;
            }

            string data = TransformProtocolBin2Json(e.Data);

            Signal signal = JsonConvert.DeserializeObject<Signal>(e.Data.ToString());

        }

        private bool IsServerProtocolValid(byte[] data) 
        {
            if(data == null || data.Length == 0) 
            { 
                return false; 
            }

            if (data[0] != 0x02)
            {
                return false;
            }

            return true;
        }

        private string TransformProtocolBin2Json(byte[] protocol)
        {
            Signal signal = new Signal();
            byte [] tlvData = new byte[protocol.Length - 4];
            TLVObject tlv = new TLVObject();

            try
            {
                protocol.CopyTo(tlvData, 3);
            }
            catch (Exception ex) 
            {
                MyLogManager.Log($"Copy Array Error:{ex.Message}");
                return null;
            }

            if(!tlv.Parse(tlvData, tlvData.Length))
            {
                MyLogManager.Log("Parse TLV Data Error, Can not Transform Protocol");
                return null;
            }

            switch (protocol[1])
            {
                case ProtocolConst.BCTC_MNG_DownloadAID_RECV:
                    signal.signalType = "OUT";
                    SignalData signalData = new SignalData();
                    signalData.id = "OPS";
                    signalData.value = tlv.GetTagData(Tag.OutcomeParameterSet);
                    signal.signalData.Add(signalData);

                    signalData.id = "DataRecord";
                    signalData.value = tlv.GetTagData(Tag.DataRecord);
                    signal.signalData.Add(signalData);

                    signalData.id = "DiscData";
                    signalData.value = tlv.GetTagData(Tag.DiscretionaryData);
                    signal.signalData.Add(signalData);

                    signalData.id = " UIRD";
                    signalData.value = tlv.GetTagData(Tag.UserInterfaceRequestData);
                    signal.signalData.Add(signalData);
                    
                    _viewModel

                    break;

                default:
                    break;

            }

            string temp = JsonConvert.SerializeObject(signal);
            return temp;
        }

        public void Client_OnDataReceived(object sender, OnServerDataReceivedEventArgs e)
        {

        }

    }
}
