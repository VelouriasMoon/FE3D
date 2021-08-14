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
        public static uint ReadUint32FromLEArray(byte[] array)
        {
            return (uint)(array[0] | (array[1] << 8) | (array[2] << 16) | (array[3] << 24));
        }
        public static uint ReadUint32FromLEArray(byte[] array, uint index)
        {
            return (uint)(array[index + 0] | (array[index + 1] << 8) | (array[index + 2] << 16) | (array[index + 3] << 24));
        }
        public static byte[] HexStringToByteArray(string hexString)
        {
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
    }
}
