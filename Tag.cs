using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace MastercardHost
{
    public class Tag
    {
        public const string ApplicationLabel = "50";
        public const string Track1Data = "56";
        public const string Track2EquivalentData = "57";
        public const string ApplicationPAN = "5A";
        public const string ApplicationExpirationDate = "5F24";
        public const string ApplicationEffectiveDate = "5F25";
        public const string IssuerCountryCode = "5F28";
        public const string TransactionCurrencyCode = "5F2A";
        public const string LanguagePreference = "5F2D";
        public const string ServiceCode = "5F30";
        public const string ApplicationPANSequenceNumber = "5F34";
        public const string TransactionCurrencyExponent = "5F36";


        public const string AccountType = "5F57";
        public const string FileControlInformationTemplate = "6F";
        public const string ReadRecordTemplate = "70";
        public const string ResponseMessageTemplateFormat2 = "77";
        public const string ResponseMessageTemplateFormat1 = "80";


        public const string UserInterfaceRequestData = "DF8116";
        public const string OutcomeParameterSet = "DF8129";
        public const string MeasuredRelayResistanceProcessingTime = "DF8306";

        public const string DiscretionaryData = "FF8106";
        public const string DataRecord = "FF8105";

    }
}
