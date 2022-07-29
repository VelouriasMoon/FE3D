using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FE3D.Compression
{
    public class LZ13
    {
        public static byte[] Compress(string infile)
        {
            return CompressFile(File.ReadAllBytes(infile));
        }
        public static byte[] Compress(byte[] indata)
        {
            return CompressFile(indata);
        }

        public static byte[] CalculateLZ13Header(byte[] data)
        {
            if (data.Length > 0xffffff)
                throw new Exception("Decompressed file is too large");

            byte[] _buffer = new byte[4];
            int maxlead = 0, sp = 0, BufferLength = 9, fc = 0, bp = 0;

            while (sp < data.Length)
            {
                int length = 1, refer = 0, x = sp < 4096 ? sp : 4096;
                while (x >= 2)
                {
                    int y = sp;
                    while (y < data.Length && data[y] == data[y - x])
                        y++;
                    y -= sp;
                    if (y >= 3 && y > length)
                    {
                        length = y;
                        refer = x;
                    }
                    x--;
                }

                if (length == 1)
                {
                    BufferLength++;
                    sp++;
                }
                else
                {
                    refer--;
                    sp += length;

                    if (length <= 2)
                        throw new Exception("Some type of error just labeled \"big oops\", very helpful cow");
                    else if (length <= 0x10)
                        BufferLength++;
                    else if (length <= 0x110)
                    {
                        BufferLength += 2;
                    }
                    else
                    {
                        BufferLength += 3;
                    }
                    BufferLength++;
                }

                int lead = sp - BufferLength;
                if (maxlead < lead)
                    maxlead = lead;

                fc++;
                if ((fc & 7) == 0)
                    bp = BufferLength++;
            }

            if (BufferLength < data.Length + 4)
            {
                int blen = maxlead + BufferLength;
                _buffer[0] = 0x13;
                _buffer[1] = (byte)(blen);
                _buffer[2] = (byte)(blen >> 8);
                _buffer[3] = (byte)(blen >> 16);
            }
            else
            {
                _buffer[0] = 0x0;
                _buffer[1] = (byte)(data.Length);
                _buffer[2] = (byte)(data.Length >> 8);
                _buffer[3] = (byte)(data.Length >> 16);
            }

            byte[] final = new byte[4];
            Array.Copy(_buffer, 0, final, 0, 4);
            return final;
        }

        private static byte[] CompressFile(byte[] data)
        {
            if (data.Length > 0xffffff)
                throw new Exception("Decompressed file is too large");

            byte[] _buffer = new byte[9 + data.Length + (data.Length -1 >> 3)];
            int maxlead = 0, sp = 0, BufferLength = 9, fc = 0, bp = 0;

            while (sp < data.Length)
            {
                int length = 1, refer = 0, x = sp < 4096 ? sp : 4096;
                while (x >= 2)
                {
                    int y = sp;
                    while (y < data.Length && data[y] == data[y - x])
                        y++;
                    y -= sp;
                    if (y >= 3 && y > length)
                    {
                        length = y;
                        refer = x;
                    }
                    x--;
                }

                if (length == 1)
                    _buffer[BufferLength++] = data[sp++];
                else
                {
                    refer--;
                    sp += length;

                    _buffer[bp] |= (byte)(128 >> (fc & 7));        
                    if (length <= 2)
                        throw new Exception("Some type of error just labeled \"big oops\", very helpful cow");
                    else if (length <= 0x10)
                        _buffer[BufferLength++] = (byte)(refer >> 8 | length - 1 << 4);
                    else if (length <= 0x110)
                    {
                        length -= 0x11;
                        _buffer[BufferLength++] = (byte)(length >> 4);
                        _buffer[BufferLength++] = (byte)(refer >> 8 | length << 4);
                    }
                    else
                    {
                        length -= 0x111;
                        _buffer[BufferLength++] = (byte)(length >> 12 | 16);
                        _buffer[BufferLength++] = (byte)(length >> 4);
                        _buffer[BufferLength++] = (byte)(length << 4 | refer >> 8);
                    }
                    _buffer[BufferLength++] = (byte)(refer);
                }

                int lead = sp - BufferLength;
                if (maxlead < lead)
                    maxlead = lead;

                fc++;
                if ((fc & 7) == 0)
                    bp = BufferLength++;
            }

            if (BufferLength < data.Length + 4)
            {
                int blen = maxlead + BufferLength;
                _buffer[0] = 0x13;
                _buffer[1] = (byte)(blen);
                _buffer[2] = (byte)(blen >> 8);
                _buffer[3] = (byte)(blen >> 16);
                _buffer[4] = 0x11;
                _buffer[5] = (byte)(data.Length);
                _buffer[6] = (byte)(data.Length >> 8);
                _buffer[7] = (byte)(data.Length >> 16);
            }
            else
            {
                BufferLength = data.Length + 4;
                _buffer[1] = (byte)(data.Length);
                _buffer[2] = (byte)(data.Length >> 8);
                _buffer[3] = (byte)(data.Length >> 16);
                Array.Copy(data, 0, _buffer, 4, data.Length);
            }

            byte[] final = new byte[BufferLength];
            Array.Copy(_buffer, 0, final, 0, BufferLength);
            return final;
        }
    }
}
