using DSDecmp.Formats.Nitro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FE3D.IO
{
    public static class FEIO
    {
        /// <summary>
        /// Reads a hex formated string and returns a byte array
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static byte[] HexStringToByteArray(string hexString)
        {
            if (hexString.StartsWith("0x"))
                hexString = hexString.Substring(2);

            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException("String cannot be an odd number in length");
            }

            byte[] data = new byte[hexString.Length / 2];
            for (int index = 0; index < data.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                data[index] = Convert.ToByte(byteValue, 16);
            }

            return data;
        }

        /// <summary>
        /// Converts a byte array into a hex string
        /// </summary>
        /// <param name="array">Input Array</param>
        /// <param name="LittleEndian">Byte Order for string</param>
        /// <param name="prefix">Addes "0x" to the start of the string</param>
        /// <returns></returns>
        public static string ByteArrayToHexString(byte[] array, bool LittleEndian = true, bool prefix = true)
        {
            if (LittleEndian)
                Array.Reverse(array);

            string result = BitConverter.ToString(array, 0, array.Length).Replace("-","");

            if (prefix)
                return $"0x{result}";
            else
                return result;
        }

        /// <summary>
        /// Reads the first 4 bytes of the byte array returns a string if present
        /// </summary>
        /// <param name="inbytes"></param>
        /// <returns></returns>
        public static string GetMagic(byte[] inbytes)
        {
            byte[] header = new byte[4];
            Array.Copy(inbytes, 0, header, 0, 4);
            string magic = Encoding.UTF8.GetString(header).Replace("\0", "");
            return magic;
        }

        public static byte[] ReadAllBytesFromStream(Stream instream)
        {
            if (instream is MemoryStream)
                return ((MemoryStream)instream).ToArray();

            using (var memoryStream = new MemoryStream())
            {
                instream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public static byte[] LZ10Decompress(byte[] compressed)
        {
            using (MemoryStream cstream = new MemoryStream(compressed))
            {
                using (MemoryStream dstream = new MemoryStream())
                {
                    (new LZ10()).Decompress(cstream, compressed.Length, dstream);
                    return dstream.ToArray();
                }
            }
        }
        public static byte[] LZ10Compress(byte[] decompressed)
        {
            using (MemoryStream dstream = new MemoryStream(decompressed))
            {
                using (MemoryStream cstream = new MemoryStream())
                {
                    (new LZ10()).Compress(dstream, decompressed.Length, cstream);
                    return cstream.ToArray();
                }
            }
        }

        public static byte[] LZ11Decompress(byte[] compressed)
        {
            using (MemoryStream cstream = new MemoryStream(compressed))
            {
                using (MemoryStream dstream = new MemoryStream())
                {
                    (new LZ11()).Decompress(cstream, compressed.Length, dstream);
                    return dstream.ToArray();
                }
            }
        }
        public static byte[] LZ11Compress(byte[] decompressed)
        {
            using (MemoryStream dstream = new MemoryStream(decompressed))
            {
                using (MemoryStream cstream = new MemoryStream())
                {
                    (new LZ11()).Compress(dstream, decompressed.Length, cstream);
                    return cstream.ToArray();
                }
            }
        }

        public static byte[] LZ13Decompress(byte[] compressed)
        {
            using (MemoryStream cstream = new MemoryStream(compressed))
            {
                using (MemoryStream dstream = new MemoryStream())
                {
                    (new LZ13()).Decompress(cstream, compressed.Length, dstream);
                    return dstream.ToArray();
                }
            }
        }
        public static byte[] LZ13Compress(byte[] decompressed)
        {
            using (MemoryStream dstream = new MemoryStream(decompressed))
            {
                using (MemoryStream cstream = new MemoryStream())
                {
                    (new LZ13()).Compress(dstream, decompressed.Length, cstream);
                    return cstream.ToArray();
                }
            }
        }

        /// <summary>
        /// Reads Bytes from an Array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="length"></param>
        /// <param name="startindex"></param>
        /// <returns>Array of the bytes read</returns>
        /// <exception cref="ArgumentNullException"><c>array</c> is null</exception>
        /// <exception cref="ArgumentOutOfRangeException"><c>length</c> or <c>start</c> is greated than array size</exception>
        public static byte[] ReadBytesFromArray(byte[] array, uint length, uint start = 0)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Length < length)
                throw new ArgumentOutOfRangeException("array");
            if (start < 0 || start > array.Length)
                throw new ArgumentOutOfRangeException("start");

            byte[] result = new byte[length];
            Array.Copy(array, start, result, 0, length);
            return result;
        }

        /// <summary>
        /// Reads a NULL-terminated string from a given byte array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public static string ReadStringFromArray(byte[] array, Encoding encoding, uint start = 0)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Length < start)
                throw new ArgumentOutOfRangeException("encoding");
            if (encoding == null) encoding = Encoding.UTF8;
            
            List<byte> result = new List<byte>();

            while (array[start] != 0)
            {
                result.Add(array[start]);
                start++;
            }

            return encoding.GetString(result.ToArray());
        }
    }
}
