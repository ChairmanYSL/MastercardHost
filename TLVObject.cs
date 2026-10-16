using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MastercardHost
{
    /// <summary>
    /// BER-TLV 解析器。只负责解析，不负责构造。
    /// </summary>
    public class TLVObject
    {
        private Dictionary<string, string> tlvDic = new Dictionary<string, string>();

        public TLVObject() 
        {
            
        }

        public Dictionary<string, string> TlvDic 
        {
            get => tlvDic; 
            private set => tlvDic = value;
        }

        /// <summary>
        /// 解析十六进制字符串形式的 BER-TLV 数据。
        /// </summary>
        public bool Parse(string tlv)
        {
            if (string.IsNullOrEmpty(tlv))
            {
                MyLogManager.Log("TLV string is null or empty");
                return false;
            }

            // check whether tlv string length is even
            if (tlv.Length % 2 != 0)
            {
                MyLogManager.Log("TLV string length is not even");
                return false;
            }

            try
            {
                int i = 0;
                while (i < tlv.Length)
                {
                    string tag;
                    byte firstByte = Convert.ToByte(tlv.Substring(i, 2), 16);

                    // Determine tag length
                    if ((firstByte & 0x1F) == 0x1F)
                    {
                        // 先做边界检查，再访问后续字节（原实现先 Substring 后判断，靠异常兜底）
                        if (i + 4 > tlv.Length)
                        {
                            MyLogManager.Log("TLV string ends after tag");
                            return false; // Bounds check for 2 bytes tag
                        }

                        if ((Convert.ToByte(tlv.Substring(i + 2, 2), 16) & 0x80) == 0x80)
                        {
                            // Tag is 3 bytes
                            if (i + 6 > tlv.Length)
                            {
                                MyLogManager.Log("TLV string ends after tag");
                                return false; // Bounds check for 3 bytes tag
                            }
                            tag = tlv.Substring(i, 6);
                            i += 6; // Move past the 3-byte tag
                        }
                        else
                        {
                            // Tag is 2 bytes
                            tag = tlv.Substring(i, 4);
                            i += 4; // Move past the 2-byte tag
                        }
                    }
                    else
                    {
                        // Tag is 1 byte
                        tag = tlv.Substring(i, 2);
                        i += 2; // Move past the 1-byte tag
                    }

                    if (i >= tlv.Length)
                    {
                        MyLogManager.Log("TLV string ends after tag");
                        return false; // Bounds check
                    }

                    // Determine length
                    int len;
                    firstByte = Convert.ToByte(tlv.Substring(i, 2), 16);
                    if ((firstByte & 0x80) == 0x80) // If b8 is 1
                    {
                        int numberOfLengthBytes = firstByte & 0x7F; // b7~b1 gives the number of bytes for length
                        len = 0;
                        for (int j = 1; j <= numberOfLengthBytes; j++)
                        {
                            if (i + j * 2 >= tlv.Length)
                            {
                                MyLogManager.Log("TLV string ends after length");
                                return false; // Bounds check
                            }
                            len = (len << 8) + Convert.ToByte(tlv.Substring(i + j * 2, 2), 16); // Construct the length from the subsequent bytes
                        }
                        i += (numberOfLengthBytes + 1) * 2; // Move past all length bytes
                    }
                    else
                    {
                        // b8 is 0, so length is given directly
                        len = firstByte & 0x7F;
                        i += 2; // Move past the length byte
                    }

                    if (i + len * 2 > tlv.Length)
                    {
                        MyLogManager.Log("TLV string ends after value");
                        return false; // Bounds check
                    }

                    // Extract the value
                    string value = tlv.Substring(i, len * 2);

                    // Save the tag-value pair
                    tlvDic[tag] = value;

                    i += len * 2; // Move past the value to the next tag, if exists
                }
                return true;
            }
            catch (Exception ex)
            {
                // If there's any error in the process
                MyLogManager.Log($"Error parsing TLV string: {ex}");
                return false;
            }
        }

        /// <summary>
        /// 解析字节数组形式的 BER-TLV 数据。
        /// </summary>
        public bool Parse(byte[] tlv, int tlv_len)
        {
            if (tlv == null || tlv_len <= 0 || tlv_len > tlv.Length)
            {
                MyLogManager.Log($"TLV data is null or invalid length (tlv_len={tlv_len})");
                return false;
            }

            int i = 0;
            try
            {
                while (i < tlv_len)
                {
                    string tag;
                    byte firstByte = tlv[i];

                    // Determine tag length
                    if ((firstByte & 0x1F) == 0x1F)
                    {
                        // 先做边界检查，再访问后续字节（原实现先访问 tlv[i+1] 后判断，靠异常兜底）
                        if (i + 1 >= tlv_len)
                        {
                            MyLogManager.Log("TLV data ends after tag");
                            return false; // Bounds check for 2 bytes tag
                        }

                        if ((tlv[i + 1] & 0x80) == 0x80)
                        {
                            // Tag is 3 bytes
                            if (i + 2 >= tlv_len) return false; // Bounds check for 3 bytes tag
                            tag = firstByte.ToString("X2") + tlv[i + 1].ToString("X2") + tlv[i + 2].ToString("X2");
                            i += 3; // Move past the 3-byte tag
                        }
                        else
                        {
                            // Tag is 2 bytes
                            tag = firstByte.ToString("X2") + tlv[i + 1].ToString("X2");
                            i += 2; // Move past the 2-byte tag
                        }
                    }
                    else
                    {
                        // Tag is 1 byte
                        tag = firstByte.ToString("X2");
                        i += 1; // Move past the 1-byte tag
                    }

                    if (i >= tlv_len) return false; // Bounds check

                    // Determine length
                    int len;
                    firstByte = tlv[i];
                    if ((firstByte & 0x80) == 0x80) // If b8 is 1
                    {
                        int numberOfLengthBytes = firstByte & 0x7F; // b7~b1 gives the number of bytes for length
                        len = 0;
                        for (int j = 1; j <= numberOfLengthBytes; j++)
                        {
                            if (i + j >= tlv_len) return false; // Bounds check
                            len = (len << 8) + tlv[i + j]; // Construct the length from the subsequent bytes
                        }
                        i += numberOfLengthBytes + 1; // Move past all length bytes
                    }
                    else
                    {
                        // b8 is 0, so length is given directly
                        len = firstByte & 0x7F;
                        i += 1; // Move past the length byte
                    }

                    if (i + len > tlv_len) return false; // Bounds check

                    // Extract the value
                    byte[] valueBytes = new byte[len];
                    Array.Copy(tlv, i, valueBytes, 0, len);
                    string value = BitConverter.ToString(valueBytes).Replace("-", "");

                    // Save the tag-value pair
                    tlvDic[tag] = value;

                    i += len; // Move past the value to the next tag, if exists
                }
                return true;
            }
            catch (Exception ex)
            {
                // 越界或格式错误都会走到这里，记录后返回失败
                MyLogManager.Log($"Error parsing TLV data (len={tlv_len}): {ex}");
                return false;
            }
        }
    }
}
