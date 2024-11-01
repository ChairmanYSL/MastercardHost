using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MastercardHost
{
    public static class MyConverter
    {
        /// <summary>
        /// Converts a hex string into a byte array.
        /// </summary>
        /// <param name="hex">The hexadecimal string to convert.</param>
        /// <returns>A byte array representing the bytes of the hexadecimal string.</returns>
        /// <exception cref="ArgumentException">Thrown if the hex string has an odd length or contains invalid characters.</exception>
        public static byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hexadecimal string must have an even length", nameof(hex));

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                string byteValue = hex.Substring(i * 2, 2);
                bytes[i] = Convert.ToByte(byteValue, 16); // 16 表示转换的基数为十六进制
            }
            return bytes;
        }

        /// <summary>
        /// Converts a segment of a byte array into a hex string.
        /// </summary>
        /// <param name="bytes">The byte array to convert.</param>
        /// <param name="start">The starting index of the segment.</param>
        /// <param name="length">The number of bytes to convert.</param>
        /// <returns>A string representing the hex value of the byte array segment.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if start or length are out of the array's bounds.</exception>
        public static string ByteArrayToHexString(byte[] bytes, int start, int length)
        {
            if (start < 0 || length < 0 || start + length > bytes.Length)
                throw new ArgumentOutOfRangeException("Start or length are out of range.");

            char[] c = new char[length * 2];
            int b;
            for (int i = start; i < start + length; i++)
            {
                b = bytes[i] >> 4;
                c[(i - start) * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                b = bytes[i] & 0xF;
                c[(i - start) * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
            }
            return new string(c);
        }


    }
}
