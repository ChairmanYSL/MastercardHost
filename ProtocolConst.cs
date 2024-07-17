using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MastercardHost
{
    public class ProtocolConst
    {
        public const byte BCTC_MNG_StartTrade_RECV = 0xC0;
        public const byte BCTC_MNG_TransResult_RECV = 0xC1;
        public const  byte BCTC_MNG_DownloadCAPK_RECV = 0xC2;
        public const  byte BCTC_MNG_DownloadAID_RECV = 0xC3;
        public const  byte BCTC_MNG_DownloadTermInfo_RECV = 0xC4;
        public const  byte BCTC_MNG_DownloadBlackList_RECV = 0xC5;
        public const  byte BCTC_MNG_DownloadRevocPK_RECV = 0xC6;
        public const  byte BCTC_MNG_UploadEcStrip_RECV = 0xC7;
        public const  byte BCTC_MNG_DownloadDRL_RECV = 0xC8;
        public const  byte BCTC_MNG_TermDispUI_RECV = 0xC9;

        public const  byte BCTC_MNG_StartTrade_SEND = 0x80;
        public const  byte BCTC_MNG_TransResult_SEND = 0x81;
        public const  byte BCTC_MNG_DownloadCAPK_SEND = 0x82;
        public const  byte BCTC_MNG_DownloadAID_SEND = 0x83;
        public const  byte BCTC_MNG_DownloadTermInfo_SEND = 0x84;
        public const  byte BCTC_MNG_DownloadBlackList_SEND = 0x85;
        public const  byte BCTC_MNG_DownloadRevocPK_SEND = 0x86;
        public const  byte BCTC_MNG_UploadEcStrip_SEND = 0x87;
        public const  byte BCTC_MNG_DownloadDRL_SEND = 0x88;
        public const  byte BCTC_MNG_TermDispUI_SEND = 0x89;

        public const  byte BCTC_TRS_FinReq_RECV = 0x41;
        public const  byte BCTC_TRS_AuthReq_RECV = 0x42;
        public const  byte BCTC_TRS_FinReqConfirm_RECV = 0x43;
        public const  byte BCTC_TRS_BatchUp_RECV = 0x44;
        public const  byte BCTC_TRS_Notify_RECV = 0x45;
        public const  byte BCTC_TRS_Reversal_RECV = 0x46;
        public const  byte BCTC_TRS_DEKSignal_RECV = 0x47;
        public const  byte BCTC_TRS_DETSignal_RECV = 0x48;

        public const  byte BCTC_TRS_FinReq_SEND = 0x01;
        public const  byte BCTC_TRS_AuthReq_SEND = 0x02;
        public const  byte BCTC_TRS_FinReqConfirm_SEND = 0x03;
        public const  byte BCTC_TRS_BatchUp_SEND = 0x04;
        public const  byte BCTC_TRS_Notify_SEND = 0x05;
        public const  byte BCTC_TRS_Reversal_SEND = 0x06;
        public const  byte BCTC_TRS_DEKSignal_SEND = 0x07;
        public const  byte BCTC_TRS_DETSignal_SEND = 0x08;
    }

}
