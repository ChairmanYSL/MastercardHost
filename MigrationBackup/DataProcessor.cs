using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using TcpSharp;
using MastercardHost.MessageProtos;
using Google.Protobuf;
using System.Security.Cryptography;

namespace MastercardHost
{
    public class DataProcessor
    {
        private TcpCommunication tcpCommunication;

        public DataProcessor(TcpCommunication tcpComm) 
        {
            this.tcpCommunication = tcpComm;
        }

        public delegate void OutcomeDelegate(string message);
        public delegate void SendDataToTestTollDelegate(byte[] data, string id);
        public delegate void SendDataToPOSDelegate(byte[] data, string id);

        public event OutcomeDelegate outcomeNeeded;
        public event SendDataToPOSDelegate SendDataToPOS;
        public event SendDataToTestTollDelegate SendDataToTestToll;

        public static ByteString HexStringToByteString(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return ByteString.Empty;

            if (hex.Length % 2 != 0)
                throw new ArgumentException("无效的十六进制字符串长度。");

            int len = hex.Length / 2;
            byte[] bytes = new byte[len];
            for (int i = 0; i < len; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return ByteString.CopyFrom(bytes);
        }

        public static bool StringToBool(string val)
        {
            if (string.IsNullOrEmpty(val))
                return false;

            return val == "1" || val == "01";
        }


        public void ProcessFromPOS(byte[] receiveData)
        {
            try
            {
                SendDataToTestToll?.Invoke(receiveData);
                Signal signal = JsonConvert.DeserializeObject<Signal>(receiveData);

                switch (signal.signalType)
                {
                    case "ACT_ACK":
                        MyLogManager.Log("Received ACT_ACK signal");
                        var responseCodeData = signal.signalData.FirstOrDefault(sd => sd.id == "ResponseCode");
                        // 检查是否找到 ResponseCode 并输出其值
                        if (responseCodeData != null && responseCodeData.value != null)
                        {
                            MyLogManager.Log($"ResponseCode: {responseCodeData.value}");
                        }
                        else
                        {
                            MyLogManager.Log("ResponseCode: not found.");
                        }
                        var descript = signal.signalData.FirstOrDefault(sd => sd.id == "Description");
                        if (descript != null && descript.value != null)
                        {
                            MyLogManager.Log($"Description: {descript.value}");
                        }
                        else
                        {
                            MyLogManager.Log("Description: not found.");
                        }

                        break;

                    case "CONFIG_ACK":
                        MyLogManager.Log("Received CONFIG_ACK signal");
                        var resp = signal.signalData.FirstOrDefault(sd => sd.id == "ResponseCode");
                        // 检查是否找到 ResponseCode 并输出其值
                        if (resp != null && resp.value != null)
                        {
                            MyLogManager.Log($"ResponseCode: {resp.value}");
                        }
                        else
                        {
                            MyLogManager.Log("ResponseCode: not found.");
                        }
                        break;

                    case "DEK":
                        MyLogManager.Log("Received DEK signal");
                        var dek = signal.signalData.FirstOrDefault(sd => sd.id == "DEK");
                        if (dek != null && dek.value != null)
                        {
                            MyLogManager.Log($"DEK: {dek.value}");
                        }
                        else
                        {
                            MyLogManager.Log("DEK: not found.");
                        }
                        break;

                    case "MSG":
                        MyLogManager.Log("Received MSG signal");
                        var urid = signal.signalData.FirstOrDefault(sd => sd.id == "URID"); //uesr request interface display
                        if (urid != null && urid.value != null)
                        {
                            byte[] urid_hex = MyConverter.HexStringToByteArray(urid.value);
                            outcomeNeeded?.Invoke("User Interface Request Data");
                            ShowUIReq(urid_hex);
                        }
                        else
                        {
                            MyLogManager.Log("MSG: not found.");
                        }
                        break;

                    case "OUT":
                        MyLogManager.Log("Received OUT signal");
                        var ops = signal.signalData.FirstOrDefault(s => s.id == "OPS");
                        if (ops != null && ops.value != null)
                        {
                            byte[] ops_hex = MyConverter.HexStringToByteArray(ops.value);
                            outcomeNeeded?.Invoke("Transaction Outcome");
                            ShowOutcome(ops_hex);
                        }

                        var dataRecord = signal.signalData.FirstOrDefault(s => s.id == "DataRecord");
                        if (dataRecord != null && dataRecord.value != null)
                        {
                            outcomeNeeded?.Invoke("DataRecord:");
                            ShowDataRecord(dataRecord.value);
                        }

                        var discData = signal.signalData.FirstOrDefault(s => s.id == "DiscData");
                        if (discData != null && discData.value != null)
                        {
                            outcomeNeeded?.Invoke("Discretionary Data:");
                            ShowDiscretionaryData(discData.value);
                        }

                        var uird = signal.signalData.FirstOrDefault(s => s.id == "UIRD");
                        if (uird != null && uird.value != null)
                        {
                            outcomeNeeded?.Invoke("User Interface Request Data");
                            ShowUIReq(MyConverter.HexStringToByteArray(uird.value));
                        }
                        break;

                    case "TEST_DATA":
                        MyLogManager.Log("Received TEST_DATA signal");
                        outcomeNeeded?.Invoke("TEST_DATA:");
                        var rrpTime = signal.signalData.FirstOrDefault(s => s.id == "DF8306");
                        if (rrpTime != null && rrpTime.value != null)
                        {
                            outcomeNeeded?.Invoke($"RRP r Measured Time:  {rrpTime.value}");
                        }
                        break;

                    case "TEST_INFO_ACK":
                        MyLogManager.Log("Received TEST_INFO_ACK signal");
                        var respCode = signal.signalData.FirstOrDefault(s => s.id == "ResponseCode");
                        if (respCode != null && respCode.value != null)
                        {
                            MyLogManager.Log($"{respCode.id}:  {respCode.value}");
                        }
                        break;
                    case "RUNTEST":
                        MyLogManager.Log("Received RUNTEST signal");
                        var testId = signal.signalData.FirstOrDefault(s => s.id == "TestId");
                        if (testId != null && testId.value != null)
                        {
                            MyLogManager.Log($"TestId:  {testId.value}");
                        }
                        break;

                    case "CONFIG_TEST":
                        MyLogManager.Log("Received CONFIG_TEST signal");
                        var configName = signal.signalData.FirstOrDefault(sd => sd.id == "CONF_NAME");
                        if (configName != null && configName.value != null)
                        {
                            if (configName.value.Equals("Default"))
                            {
                                configName.value = "PPS_MChip1";
                            }
                            string fileName = configName.value + ".json";
                            string runDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                            string configDir = runDir + "Config\\Config\\";
                            MyLogManager.Log($"Config Dir:{configDir}");
                            if (Directory.Exists(configDir))
                            {
                                MyLogManager.Log($"Target Config:{configDir + fileName}");
                                if (!File.Exists(configDir + fileName))
                                {
                                    MessageBox.Show("Target Config doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                else
                                {
                                    
                                    StreamReader streamReader = File.OpenText(configDir + fileName);
                                    JObject src = (JObject)JToken.ReadFrom(new JsonTextReader(streamReader));
                                    JObject dest = new JObject()
                                    {
                                        ["signalType"] = "CONFIG",
                                        ["AIDParam"] = src["AIDParam"],
                                        ["TermParam"] = src["TermParam"]
                                    };

                                    string str = dest.ToString(Formatting.None);
                                    MyLogManager.Log($"Download Config str len:  {str.Length}");
                                    MyLogManager.Log($"Download Config:  {str}");
                                }
                            }
                            else
                            {
                                MessageBox.Show("No Config Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        break;

                    case "CAPK":
                        MyLogManager.Log("Received CAPK signal");
                        var capkName = signal.signalData.FirstOrDefault(sd => sd.id == "CONF_NAME");
                        if (capkName != null && capkName.value != null)
                        {
                            string fileName = "PAYPASS_CAPK.json";
                            string runDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                            string configDir = runDir + "Config\\CAPK\\";
                            MyLogManager.Log($"Config Dir:{configDir}");
                            if (Directory.Exists(configDir))
                            {
                                MyLogManager.Log($"Target CAPK:{configDir + fileName}");
                                if (!File.Exists(configDir + fileName))
                                {
                                    MessageBox.Show("Target CAPK doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                else
                                {
                                    StreamReader streamReader = File.OpenText(configDir + fileName);
                                    JObject src = (JObject)JToken.ReadFrom(new JsonTextReader(streamReader));
                                    JObject dest = new JObject()
                                    {
                                        ["signalType"] = "CAPK",
                                        ["CAPKParam"] = src["CAPKParam"],
                                    };

                                    string str = dest.ToString();
                                    MyLogManager.Log($"Download CAPK:  {str}");
                                }
                            }
                            else
                            {
                                MessageBox.Show("No Config Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        break;

                    case "REVOCATION_PK":
                        MyLogManager.Log("Received REVOCATION_PK signal");
                        var revopkName = signal.signalData.FirstOrDefault(sd => sd.id == "CONF_NAME");
                        if (revopkName != null && revopkName.value != null)
                        {
                            string fileName = "PAYPASS_CAPK.json";
                            string runDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                            string configDir = runDir + "Config\\CAPK\\";
                            MyLogManager.Log($"Config Dir:{configDir}");
                            if (Directory.Exists(configDir))
                            {
                                MyLogManager.Log($"Target CAPK:{configDir + fileName}");
                                if (!File.Exists(configDir + fileName))
                                {
                                    MessageBox.Show("Target CAPK doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                else
                                {
                                    StreamReader streamReader = File.OpenText(configDir + fileName);
                                    JObject src = (JObject)JToken.ReadFrom(new JsonTextReader(streamReader));
                                    JObject dest = new JObject()
                                    {
                                        ["signalType"] = "CAPK",
                                        ["CAPKParam"] = src["CAPKParam"],
                                    };

                                    string str = dest.ToString();
                                    MyLogManager.Log($"Download CAPK:  {str}");
                                }
                            }
                            else
                            {
                                MessageBox.Show("No Config Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        break;


                    default:
                        break;
                }
            }
            catch (Exception ex) 
            {
                MyLogManager.Log($"Exception:  {ex.Message}");
            }

        }

        public void ProcessFromTestTool_PCBMode(string receiveData)
        {
            try
            {
                Signal signal = JsonConvert.DeserializeObject<Signal>(receiveData);

                MyLogManager.Log($"Received {signal.signalType} signal");
                switch (signal.signalType)
                {
                    case "ACT":
                        foreach (var tag in signal.signalData)
                        {
                            MyLogManager.Log($"ID:{tag.id}, Value:{tag.value}");
                        }
                        break;

                    case "CONFIG":
                        var configName = signal.signalData.FirstOrDefault(sd => sd.id == "CONF_NAME");
                        if (configName != null && configName.value != null)
                        {
                            string fileName = configName.value + ".json";
                            string runDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                            string configDir = runDir + "Config\\Config\\";
                            MyLogManager.Log($"Config Dir:{configDir}");
                            if (Directory.Exists(configDir))
                            {
                                MyLogManager.Log($"Target Config:{configDir + fileName}");
                                if (!File.Exists(configDir + fileName))
                                {
                                    MessageBox.Show("Target Config doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                else
                                {
                                    StreamReader streamReader = File.OpenText(configDir + fileName);
                                    JObject src = (JObject)JToken.ReadFrom(new JsonTextReader(streamReader));
                                    JObject dest = new JObject()
                                    {
                                        ["signalType"] = "CONFIG",
                                        ["AIDParam"] = src["AIDParam"],
                                        ["TermParam"] = src["TermParam"]
                                    };

                                    string str = dest.ToString();
                                    MyLogManager.Log($"Download Config:  {str}");
                                }
                            }
                            else
                            {
                                MessageBox.Show("No Config Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        break;

                    case "CLEAN":
                        var date = signal.signalData.FirstOrDefault(s => s.id == "9A");
                        if (date != null && date.value != null)
                        {
                            MyLogManager.Log($"9A:  {date.value}");
                        }

                        var time = signal.signalData.FirstOrDefault(s => s.id == "9F21");
                        if (time != null && time.value != null)
                        {
                            MyLogManager.Log($"9F21:   {time.value}");
                        }
                        break;

                    case "DET":
                        var det = signal.signalData.FirstOrDefault(s => s.id == "DET");
                        if (det != null && det.value != null)
                        {
                            MyLogManager.Log($"DET:  {det.value}");
                        }

                        break;

                    case "RUNTEST_ RESULT":
                        var testResult = signal.signalData.FirstOrDefault(s => s.id == "TestResult");
                        if (testResult != null && testResult.value != null)
                        {
                            MyLogManager.Log($"TestResult:  {testResult.value}");
                        }
                        break;

                    case "TEST_INFO":
                        foreach (var id in signal.signalData)
                        {
                            MyLogManager.Log($"{id.id}:  {id.value}");
                        }

                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex) 
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }

        private void DownloadConfig(string jsonFilePath)
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            JObject jsonData = JObject.Parse(jsonContent);

            var aidFieldMapping = new Dictionary<string, Action<AID, string>>()
                                {
                                        { "9F06", (aidMsg, val) => aidMsg.Aid = HexStringToByteString(val) },
                                        { "9C", (aidMsg, val) => aidMsg.TransType = HexStringToByteString(val) },
                                        { "9F09", (aidMsg, val) => aidMsg.AppVer = HexStringToByteString(val) },
                                        { "9F1B", (aidMsg, val) => aidMsg.TermFloorLmt = HexStringToByteString(val) },
                                        { "9F1D", (aidMsg, val) => aidMsg.TermRiskManageData = HexStringToByteString(val) },
                                        { "9F35", (aidMsg, val) => aidMsg.TermType = HexStringToByteString(val) },
                                        { "DF11", (aidMsg, val) => aidMsg.TacDefault = HexStringToByteString(val) },
                                        { "DF12", (aidMsg, val) => aidMsg.TacOnline = HexStringToByteString(val) },
                                        { "DF13", (aidMsg, val) => aidMsg.TacDeny = HexStringToByteString(val) },
                                        { "DF19", (aidMsg, val) => aidMsg.ClFloorLmt = HexStringToByteString(val) },
                                        { "DF20", (aidMsg, val) => aidMsg.ClTransLmt = HexStringToByteString(val) },
                                        { "DF21", (aidMsg, val) => aidMsg.CvmLmt = HexStringToByteString(val) },
                                        { "DF32", (aidMsg, val) => aidMsg.SupStausCheck = StringToBool(val) },
                                        { "DF33", (aidMsg, val) => aidMsg.SupClTransLmtCheck = StringToBool(val) },
                                        { "DF34", (aidMsg, val) => aidMsg.SupClFloorLmtCheck = StringToBool(val) },
                                        { "DF35", (aidMsg, val) => aidMsg.SupTermFloorLmtCheck = StringToBool(val) },
                                        { "DF36", (aidMsg, val) => aidMsg.SupCVMCheck = StringToBool(val) },
                                        { "DF811B", (aidMsg, val) => aidMsg.KernelConf = HexStringToByteString(val) },
                                        { "DF811E", (aidMsg, val) => aidMsg.MsdCVMCapCVMReq = HexStringToByteString(val) },
                                        { "DF8124", (aidMsg, val) => aidMsg.RcTransLmtNoCDCVM = HexStringToByteString(val) },
                                        { "DF8125", (aidMsg, val) => aidMsg.RcTransLmtCDCVM = HexStringToByteString(val) },
                                        { "DF812C", (aidMsg, val) => aidMsg.MsdCVMCapNoCVMReq = HexStringToByteString(val) },
                                        { "9F7E", (aidMsg, val) => aidMsg.MobileSupID = HexStringToByteString(val) },
                                        { "DF811F", (aidMsg, val) => aidMsg.SecueCap = HexStringToByteString(val) },
                                        { "DF8118", (aidMsg, val) => aidMsg.CvmCapCVMReq = HexStringToByteString(val) },
                                        { "DF8119", (aidMsg, val) => aidMsg.CvmCapNoCVMReq = HexStringToByteString(val) },
                                        { "9F40", (aidMsg, val) => aidMsg.AddTermCap = HexStringToByteString(val) },
                                        { "9F33", (aidMsg, val) => aidMsg.TermCap = HexStringToByteString(val) },
                                    };

            var termParamFieldMapping = new Dictionary<string, Action<TermParam, string>>()
                                    {
                                        { "9F01", (termMsg, val) => termMsg.AcquirerID = HexStringToByteString(val) },
                                        { "9F1E", (termMsg, val) => termMsg.IfdSN = HexStringToByteString(val) },
                                        { "9F15", (termMsg, val) => termMsg.MerchanCateCode = HexStringToByteString(val) },
                                        { "9F16", (termMsg, val) => termMsg.MerchanID = HexStringToByteString(val) },
                                        { "9F4E", (termMsg, val) => termMsg.MerchanName = HexStringToByteString(val) },
                                        { "9F1A", (termMsg, val) => termMsg.TermCountryCode = HexStringToByteString(val) },
                                        { "9F1C", (termMsg, val) => termMsg.TermID = HexStringToByteString(val) },
                                        { "9F7C", (termMsg, val) => termMsg.MerchanCustData = HexStringToByteString(val) },
                                        { "5F2A", (termMsg, val) => termMsg.TransCurrCode = HexStringToByteString(val) },
                                        { "5F36", (termMsg, val) => termMsg.TransCurrExp = HexStringToByteString(val) },
                                        { "9F66", (termMsg, val) => termMsg.Ttq = HexStringToByteString(val) },
                                        { "9F53", (termMsg, val) => termMsg.TransCateCode = HexStringToByteString(val) },
                                        { "DF811A", (termMsg, val) => termMsg.DefualtUDOL = HexStringToByteString(val) },
                                        { "DF810C", (termMsg, val) => termMsg.KernelID = HexStringToByteString(val) },
                                        { "9F6D", (termMsg, val) => termMsg.MsdAppVer = HexStringToByteString(val) },
                                        { "DF811C", (termMsg, val) => termMsg.MaxLifeTornLog = HexStringToByteString(val) },
                                        { "DF811D", (termMsg, val) => termMsg.MaxNumberTornLog = HexStringToByteString(val) },
                                        { "DF811F", (termMsg, val) => termMsg.SecurCap = HexStringToByteString(val) },
                                        // 添加其他映射
                                    };

            ConfigProtocol configProtocol = new ConfigProtocol();

            JArray aidParamArray = (JArray)jsonData["AIDParam"];
            foreach (JObject aidItem in aidParamArray)
            {
                AID aidMsg = new AID();

                foreach (var property in aidItem.Properties())
                {
                    string key = property.Name;
                    string value = property.Value.ToString();

                    if (aidFieldMapping.ContainsKey(key))
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            aidFieldMapping[key](aidMsg, value);
                        }
                    }
                    else
                    {
                        //TODO: 处理其他可能的扩展字段
                    }
                }

                configProtocol.Aid.Add(aidMsg);
            }

            // 处理 TermParam 对象
            JObject termParamObj = (JObject)jsonData["TermParam"];
            TermParam termParamMsg = new TermParam();

            foreach (var property in termParamObj.Properties())
            {
                string key = property.Name;
                string value = property.Value.ToString();

                if (termParamFieldMapping.ContainsKey(key))
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        termParamFieldMapping[key](termParamMsg, value);
                    }
                }
                else
                {
                    //TODO: 处理其他可能的扩展字段
                }
            }

            configProtocol.Termpar = termParamMsg;

            Envelope envelope = new Envelope()
            {
                Config = configProtocol
            };

            byte[] serializedData = envelope.ToByteArray();

            MyLogManager.Log($"Download Config:  {serializedData}");
            SendDataToPOS?.Invoke(serializedData);
        }

        private void TransFormSiganl(Signal signal)
        {
            SignalProtocol signalProtocol = new SignalProtocol()
            {
                Type = signal.signalType,
            };

            foreach (var tag in signal.signalData)
            {
                MyLogManager.Log($"ID:{tag.id}, Value:{tag.value}");
                SignalDataProtocol dataProtocol = new SignalDataProtocol
                {
                    Id = tag.id,
                    Value = tag.value
                };
                signalProtocol.Data.Add(dataProtocol);
            }

            Envelope envelope = new Envelope()
            {
                Signal = signalProtocol
            };

            byte[] serializedData = envelope.ToByteArray();
            SendDataToPOS?.Invoke(serializedData);
        }

        private void ProcessFromTestTool_TEIMode(string receiveData)
        {
            try
            {
                Signal signal = JsonConvert.DeserializeObject<Signal>(receiveData);

                MyLogManager.Log($"Received {signal.signalType} signal");
                switch (signal.signalType)
                {
                    case "ACT":
                        TransFormSiganl(signal);
                        break;

                    case "CONFIG":
                        var configName = signal.signalData.FirstOrDefault(sd => sd.id == "CONF_NAME");
                        if (configName != null && configName.value != null)
                        {
                            string fileName = configName.value + ".json";
                            string runDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                            string configDir = runDir + "Config\\Config\\";
                            MyLogManager.Log($"Config Dir:{configDir}");

                            if (Directory.Exists(configDir))
                            {
                                MyLogManager.Log($"Target Config:{configDir + fileName}");
                                if (!File.Exists(configDir + fileName))
                                {
                                    MessageBox.Show("Target Config doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                else
                                {
                                    DownloadConfig(configDir + fileName);
                                }
                            }
                            else
                            {
                                MessageBox.Show("No Config Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        break;

                    case "CLEAN":
                        var date = signal.signalData.FirstOrDefault(s => s.id == "9A");
                        if (date != null && date.value != null)
                        {
                            MyLogManager.Log($"9A:  {date.value}");
                        }

                        var time = signal.signalData.FirstOrDefault(s => s.id == "9F21");
                        if (time != null && time.value != null)
                        {
                            MyLogManager.Log($"9F21:   {time.value}");
                        }
                        TransFormSiganl(signal);
                        break;

                    case "DET":
                        var det = signal.signalData.FirstOrDefault(s => s.id == "DET");
                        if (det != null && det.value != null)
                        {
                            MyLogManager.Log($"DET:  {det.value}");
                        }
                        TransFormSiganl(signal);
                        break;

                    case "RUNTEST_ RESULT":
                        var testResult = signal.signalData.FirstOrDefault(s => s.id == "TestResult");
                        if (testResult != null && testResult.value != null)
                        {
                            MyLogManager.Log($"TestResult:  {testResult.value}");
                        }
                        TransFormSiganl(signal);
                        break;

                    case "TEST_INFO":
                        foreach (var id in signal.signalData)
                        {
                            MyLogManager.Log($"{id.id}:  {id.value}");
                        }
                        TransFormSiganl(signal);
                        break;
                    default:
                        MyLogManager.Log("无法识别的Signal类型");
                        break;
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }


        private void ShowUIReq(byte[] data)     //refer to C-2 kernel book A.1.195 User Interface Request Data
        {
            byte messageId = data[0];
            switch(messageId)
            {
                case (byte)0x17:
                    outcomeNeeded?.Invoke("Message Identifier:  CARD READ OK");
                    break;
                case (byte)0x21:
                    outcomeNeeded?.Invoke("Message Identifier:  TRY AGAIN");
                    break;
                case (byte)0x03:
                    outcomeNeeded?.Invoke("Message Identifier:  APPROVED");
                    break;
                case (byte)0x1A:
                    outcomeNeeded?.Invoke("Message Identifier:  APPROVED – SIGN");
                    break;
                case (byte)0x07:
                    outcomeNeeded?.Invoke("Message Identifier:  DECLINED");
                    break;
                case (byte)0x1C:
                    outcomeNeeded?.Invoke("Message Identifier:  ERROR – OTHER CARD");
                    break;
                case (byte)0x1D:
                    outcomeNeeded?.Invoke("Message Identifier:  INSERT CARD");
                    break;
                case (byte)0x20:
                    outcomeNeeded?.Invoke("Message Identifier:  SEE PHONE");
                    break;
                case (byte)0x1B:
                    outcomeNeeded?.Invoke("Message Identifier:  AUTHORISING – PLEASE WAIT");
                    break;
                case (byte)0x1E:
                    outcomeNeeded?.Invoke("Message Identifier:  CLEAR DISPLAY");
                    break;
                default :
                    outcomeNeeded?.Invoke("Message Identifier:  N/A");
                    break;
            }

            byte status = data[1];
            switch(status)
            {
                case (byte)0x00:
                    outcomeNeeded?.Invoke("Status:  NOT READY");
                    break;
                case (byte)0x01:
                    outcomeNeeded?.Invoke("Status:  IDLE");
                    break;
                case (byte)0x02:
                    outcomeNeeded?.Invoke("Status:  READY TO READ");
                    break;
                case (byte)0x03:
                    outcomeNeeded?.Invoke("Status:  PROCESSING");
                    break;
                case (byte)0x04:
                    outcomeNeeded?.Invoke("Status:  CARD READ SUCCESSFULLY");
                    break;
                case (byte)0x05:
                    outcomeNeeded?.Invoke("Status:  PROCESSING ERROR");
                    break;
                default:
                    outcomeNeeded?.Invoke("Status:  N/A");
                    break;
            }

            string holdTime = MyConverter.ByteArrayToHexString(data, 2, 3);
            outcomeNeeded?.Invoke($"Hold Time:  {holdTime}");

            string languagePrefer = MyConverter.ByteArrayToHexString(data,5, 8);
            outcomeNeeded?.Invoke($"Language Preference:  {languagePrefer}");

            byte valueQualifier = data[13];
            switch (valueQualifier)  
            {
                case (byte)0x00:
                    outcomeNeeded?.Invoke("Value Qualifier:  NONE");
                    break;
                case (byte)0x01:
                    outcomeNeeded?.Invoke("Value Qualifier:  AMOUNT");
                    break;
                case (byte)0x02:
                    outcomeNeeded?.Invoke("Value Qualifier:  BALANCE");
                    break;
                default :
                    outcomeNeeded?.Invoke("Value Qualifier:  N/A");
                    break;
            }

            string currencyCode = MyConverter.ByteArrayToHexString(data, 20, 2);
            outcomeNeeded?.Invoke($"Currency Code:  {currencyCode}");
        }

        private void ShowOutcome(byte[] data)   //refer to C-2 kernel book A.1.117 Outcome Parameter Set
        {
            byte status = data[0];
            switch (status)
            {
                case (byte)0x10:
                    outcomeNeeded?.Invoke($"Status:  APPROVED");
                    break;
                case (byte)0x20:
                    outcomeNeeded?.Invoke($"Status:  DECLINED");
                    break;
                case (byte)0x30:
                    outcomeNeeded?.Invoke($"Status:  ONLINE REQUEST");
                    break;
                case (byte)0x40:
                    outcomeNeeded?.Invoke($"Status:  END APPLICATION");
                    break;
                case (byte)0x50:
                    outcomeNeeded?.Invoke($"Status:  SELECT NEXT");
                    break;
                case (byte)0x60:
                    outcomeNeeded?.Invoke($"Status:  TRY ANOTHER INTERFACE");
                    break;
                case (byte)0x70:
                    outcomeNeeded?.Invoke($"Status:  TRY AGAIN");
                    break;
                case (byte)0xF0:
                    outcomeNeeded?.Invoke($"Status:  N/A");
                    break;
                default:
                    outcomeNeeded?.Invoke($"Status:  Invalid Data");
                    break;
            }

            byte start = data[1];
            switch (start)
            {
                case (byte)0x00:
                    outcomeNeeded?.Invoke($"Start:  A");
                    break;
                case (byte)0x10:
                    outcomeNeeded?.Invoke($"Start:  B");
                    break;
                case (byte)0x20:
                    outcomeNeeded?.Invoke($"Start:  C");
                    break;
                case (byte)0x30:
                    outcomeNeeded?.Invoke($"Start:  D");
                    break;
                case (byte)0xF0:
                    outcomeNeeded?.Invoke($"Start:  N/A");
                    break;
                default:
                    outcomeNeeded?.Invoke($"Start:  Invalid Data");
                    break;
            }

            byte onlineRespData = data[2];
            switch (onlineRespData)
            {
                case (byte)0xF0:
                    outcomeNeeded?.Invoke($"Online Response Data:  N/A");
                    break;

                default:
                    outcomeNeeded?.Invoke($"Online Response Data:  Invalid Data");
                    break;
            }

            byte cvm = data[3];
            switch (cvm) 
            {
                case (byte)0x00:
                    outcomeNeeded?.Invoke($"CVM:  NO CVM");
                    break;
                case (byte)0x01:
                    outcomeNeeded?.Invoke($"CVM:  OBTAIN SIGNATURE");
                    break;
                case (byte)0x02:
                    outcomeNeeded?.Invoke($"CVM:  ONLINE PIN");
                    break;
                case (byte)0x03:
                    outcomeNeeded?.Invoke($"CVM:  CONFIRMATION CODE VERIFIED");
                    break;
                case (byte)0xF0:
                    outcomeNeeded?.Invoke($"CVM:  N/A");
                    break;
                default:
                    outcomeNeeded?.Invoke($"CVM:  Invalid Data");
                    break;
            }

            byte flag = data[4];
            if((flag & (byte)0x80) == (byte)0x80)
            {
                outcomeNeeded?.Invoke($"UI Request on Outcome Present:  yes");
            }
            else
            {
                outcomeNeeded?.Invoke($"UI Request on Outcome Present:  no");
            }
            if ((flag & (byte)0x40) == (byte)0x40)
            {
                outcomeNeeded?.Invoke($"UI Request on Restart Present:  yes");
            }
            else
            {
                outcomeNeeded?.Invoke($"UI Request on Restart Present:  no");
            }
            if ((flag & (byte)0x20) == (byte)0x20)
            {
                outcomeNeeded?.Invoke($"Data Record Present:  yes");
            }
            else
            {
                outcomeNeeded?.Invoke($"Data Record Present:  no");
            }
            if ((flag & (byte)0x10) == (byte)0x10)
            {
                outcomeNeeded?.Invoke($"Discretionary Data Present:  yes");
            }
            else
            {
                outcomeNeeded?.Invoke($"Discretionary Data Present:  no");
            }
            if ((flag & (byte)0x08) == (byte)0x08)
            {
                outcomeNeeded?.Invoke($"Discretionary Data Present:  yes");
            }
            else
            {
                outcomeNeeded?.Invoke($"Discretionary Data Present:  no");
            }

            byte aip = data[5];
            if((byte)0xF0 == (aip & (byte)0xF0))
            {
                outcomeNeeded?.Invoke($"Alternate Interface Preference:   N/A");
            }
            else
            {
                outcomeNeeded?.Invoke($"Alternate Interface Preference:  Invalid Data");
            }

            byte fieldOffReq = data[6];
            if ((byte)0xFF == fieldOffReq)
            {
                outcomeNeeded?.Invoke($"Field Off Request:   N/A");
            }
            else
            {
                outcomeNeeded?.Invoke($"Field Off Request:  {fieldOffReq}");
            }

            byte removalTimeout = data[7];
            outcomeNeeded?.Invoke($"Removal Timeout:  {removalTimeout}");
        }

        private void ShowDataRecord(string data)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }

            TLVObject tlvObject = new TLVObject();
            tlvObject.Parse(data);

            Dictionary<string, string> dict = tlvObject.TlvDic;
            foreach (KeyValuePair<string, string> kvp in dict)
            {
                outcomeNeeded?.Invoke($"{kvp.Key}:  {kvp.Value}");
            }
        }

        private void ShowDiscretionaryData(string data)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }

            TLVObject tlvObject = new TLVObject();
            tlvObject.Parse(data);

            Dictionary<string, string> dict = tlvObject.TlvDic;
            foreach (KeyValuePair<string, string> kvp in dict)
            {
                if(kvp.Key == "DF8115")
                {
                    outcomeNeeded?.Invoke($"Error Indication:");
                    byte [] errIndic = MyConverter.HexStringToByteArray(kvp.Value);
                    byte L1 = errIndic[0];
                    switch (L1) 
                    {
                        case (byte)0x00:
                            outcomeNeeded?.Invoke($"L1:  OK");
                            break;
                        case (byte)0x01:
                            outcomeNeeded?.Invoke($"L1:  TIME OUT ERROR");
                            break;
                        case (byte)0x02:
                            outcomeNeeded?.Invoke($"L1:  TRANSMISSION ERROR");
                            break;
                        case (byte)0x03:
                            outcomeNeeded?.Invoke($"L1:  PROTOCOL ERROR");
                            break;
                        default:
                            outcomeNeeded?.Invoke($"L1:  Invalid Data");
                            break;
                    }
                    byte L2 = errIndic[1];
                    switch (L2) 
                    {
                        case (byte)0x00:
                            outcomeNeeded?.Invoke($"L2:  OK");
                            break;
                        case (byte)0x01:
                            outcomeNeeded?.Invoke($"L2:  CARD DATA MISSING");
                            break;
                        case (byte)0x02:
                            outcomeNeeded?.Invoke($"L2:  CAM FAILED");
                            break;
                        case (byte)0x03:
                            outcomeNeeded?.Invoke($"L2:  STATUS BYTES");
                            break;
                        case (byte)0x04:
                            outcomeNeeded?.Invoke($"L2:  PARSING ERROR");
                            break;
                        case (byte)0x05:
                            outcomeNeeded?.Invoke($"L2:  MAX LIMIT EXCEEDED");
                            break;
                        case (byte)0x06:
                            outcomeNeeded?.Invoke($"L2:  CARD DATA ERROR");
                            break;
                        case (byte)0x07:
                            outcomeNeeded?.Invoke($"L2:  MAGSTRIPE NOT SUPPORTED");
                            break;
                        case (byte)0x08:
                            outcomeNeeded?.Invoke($"L2:  NO PPSE");
                            break;
                        case (byte)0x09:
                            outcomeNeeded?.Invoke($"L2:  PPSE FAULT");
                            break;
                        case (byte)0x0A:
                            outcomeNeeded?.Invoke($"L2:  EMPTY CANDIDATE LIST");
                            break;
                        case (byte)0x0B:
                            outcomeNeeded?.Invoke($"L2:  IDS READ ERROR");
                            break;
                        case (byte)0x0C:
                            outcomeNeeded?.Invoke($"L2:  IDS WRITE ERROR");
                            break;
                        case (byte)0x0D:
                            outcomeNeeded?.Invoke($"L2:  IDS DATA ERROR");
                            break;
                        case (byte)0x0E:
                            outcomeNeeded?.Invoke($"L2:  IDS NO MATCHING AC");
                            break;
                        case (byte)0x0F:
                            outcomeNeeded?.Invoke($"L2:  TERMINAL DATA ERROR");
                            break;
                        default:
                            outcomeNeeded?.Invoke($"L2:  Invalid Data");
                            break;
                    }

                    byte L3 = errIndic[2];
                    switch(L3)
                    {
                        case (byte)0x00:
                            outcomeNeeded?.Invoke($"L3:  OK");
                            break;
                        case (byte)0x01:
                            outcomeNeeded?.Invoke($"L3:  TIME OUT");
                            break;
                        case (byte)0x02:
                            outcomeNeeded?.Invoke($"L3:  STOP");
                            break;
                        case (byte)0x03:
                            outcomeNeeded?.Invoke($"L3:  AMOUNT NOT PRESENT");
                            break;
                        default:
                            outcomeNeeded?.Invoke($"L3:  Invalid Data");
                            break;
                    }

                    string SW12 = kvp.Value.Substring(3, 2);
                    outcomeNeeded?.Invoke($"SW12:  {SW12}");

                    byte msgOnError = errIndic[5];
                    switch (msgOnError) 
                    {
                        case (byte)0x17:
                            outcomeNeeded?.Invoke("Msg On Error:  CARD READ OK");
                            break;
                        case (byte)0x21:
                            outcomeNeeded?.Invoke("Msg On Error:  TRY AGAIN");
                            break;
                        case (byte)0x03:
                            outcomeNeeded?.Invoke("Msg On Error:  APPROVED");
                            break;
                        case (byte)0x1A:
                            outcomeNeeded?.Invoke("Msg On Error:  APPROVED – SIGN");
                            break;
                        case (byte)0x07:
                            outcomeNeeded?.Invoke("Msg On Error:  DECLINED");
                            break;
                        case (byte)0x1C:
                            outcomeNeeded?.Invoke("Msg On Error:  ERROR – OTHER CARD");
                            break;
                        case (byte)0x1D:
                            outcomeNeeded?.Invoke("Msg On Error:  INSERT CARD");
                            break;
                        case (byte)0x20:
                            outcomeNeeded?.Invoke("Msg On Error:  SEE PHONE");
                            break;
                        case (byte)0x1B:
                            outcomeNeeded?.Invoke("Msg On Error:  AUTHORISING – PLEASE WAIT");
                            break;
                        case (byte)0x1E:
                            outcomeNeeded?.Invoke("Msg On Error:  CLEAR DISPLAY");
                            break;
                        default:
                            outcomeNeeded?.Invoke("Msg On Error:  N/A");
                            break;
                    }
                }
                outcomeNeeded?.Invoke($"{kvp.Key}:  {kvp.Value}");
            }
        }

        public void Client_OnDataReceived(object sender, OnClientDataReceivedEventArgs e)
        {
            ProcessFromTestTool_PCBMode(Encoding.UTF8.GetString(e.Data));
        }

        public void Server_OnDataReceived(object sender, OnServerDataReceivedEventArgs e) 
        {
            ProcessFromTestTool_TEIMode(Encoding.UTF8.GetString(e.Data));
        }
    }
}
