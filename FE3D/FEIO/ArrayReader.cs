using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FE3D.IO
{
    public static class ArrayReader
    {
        /// <summary>
        /// Read a signed Short from a given byte array
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static short ReadInt16FromArray(byte[] array, uint index = 0, bool LittleEndian = true)
        {
            if (LittleEndian)
                return (short)(array[index + 0] | (array[index + 1] << 8));
            else
                return (short)(array[index + 0] | (array[index + 1]));
        }

        /// <summary>
        /// Read a signed Short from a given byte array
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static ushort ReadUInt16FromArray(byte[] array, uint index = 0, bool LittleEndian = true)
        {
            if (LittleEndian)
                return (ushort)(array[index + 0] | (array[index + 1] << 8));
            else
                return (ushort)(array[index + 0] | (array[index + 1]));
        }

        /// <summary>
        /// Reads a signed Int from a given byte array
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static int ReadInt32FromArray(byte[] array, uint index = 0, bool LittleEndian = true)
        {
            if (LittleEndian)
                return (int)(array[index + 0] | (array[index + 1] << 8) | (array[index + 2] << 16) | (array[index + 3] << 24));
            else
                return (int)(array[index + 0] | (array[index + 1]) | (array[index + 2]) | (array[index + 3]));
        }

        /// <summary>
        /// Reads an unsigned Int from a given byte array
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static uint ReadUInt32FromArray(byte[] array, uint index = 0, bool LittleEndian = true)
        {
            if (LittleEndian)
                return (uint)(array[index + 0] | (array[index + 1] << 8) | (array[index + 2] << 16) | (array[index + 3] << 24));
            else
                return (uint)(array[index + 0] | (array[index + 1]) | (array[index + 2]) | (array[index + 3]));
        }

        /// <summary>
        /// Reads a signed Long from a given byte array
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static long ReadInt64FromArray(byte[] array, uint index = 0, bool LittleEndian = true)
        {
            if (LittleEndian)
                return (long)(array[index + 0] | (array[index + 1] << 8) | (array[index + 2] << 16) | (array[index + 3] << 24) | (array[index + 4] << 32) | (array[index + 5] << 40) | (array[index + 6] << 48) | (array[index + 7] << 56));
            else
                return (long)(array[index + 0] | (array[index + 1]) | (array[index + 2]) | (array[index + 3]) | (array[index + 4]) | (array[index + 5]) | (array[index + 6]) | (array[index + 7]));
        }

        /// <summary>
        /// Reads a signed Long from a given byte array
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static ulong ReadUInt64FromArray(byte[] array, uint index = 0, bool LittleEndian = true)
        {
            if (LittleEndian)
                return (ulong)(array[index + 0] | (array[index + 1] << 8) | (array[index + 2] << 16) | (array[index + 3] << 24) | (array[index + 4] << 32) | (array[index + 5] << 40) | (array[index + 6] << 48) | (array[index + 7] << 56));
            else
                return (ulong)(array[index + 0] | (array[index + 1]) | (array[index + 2]) | (array[index + 3]) | (array[index + 4]) | (array[index + 5]) | (array[index + 6]) | (array[index + 7]));
        }
    }
}
