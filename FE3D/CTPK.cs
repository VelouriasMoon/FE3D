using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FE3D.GovanifY;

namespace FE3D.Image
{
    public class CTPK
    {
        public string Magic { get; set; }
        public ushort Version { get; set; }
        public ushort TexNum { get; set; }
        public uint TexDataOffset { get; set; }
        public uint TotalTexSize { get; set; }
        public uint HashOffset { get; set; }
        public uint FormatInfoOffset { get; set; }
        public ulong HeaderPadding { get; set; }
        public List<CTPKData> TextureData { get; set; }
        public List<uint> TextureSize { get; set; }
        public List<string> FileName { get; set; }
        public List<CTPKHash> NameHash { get; set; }
        public List<CTPKInfo> FormatInfo { get; set; }
        public List<byte[]> Textures { get; set; }

        public void Read(BinaryStream binaryStream)
        {
            TextureData = new List<CTPKData>();
            TextureSize = new List<uint>();
            FileName = new List<string>();
            NameHash = new List<CTPKHash>();
            FormatInfo = new List<CTPKInfo>();
            Textures = new List<byte[]>();


            //Read header data
            Magic = Encoding.UTF8.GetString(binaryStream.ReadBytes(4));
            Version = binaryStream.ReadUInt16();
            TexNum = binaryStream.ReadUInt16();
            TexDataOffset = binaryStream.ReadUInt32();
            TotalTexSize = binaryStream.ReadUInt32();
            HashOffset = binaryStream.ReadUInt32();
            FormatInfoOffset = binaryStream.ReadUInt32();
            HeaderPadding = binaryStream.ReadUInt64();

            //Read texture data for each texture
            for (int i = 0; i < TexNum; i++)
            {
                CTPKData data = new CTPKData();
                data.Read(binaryStream);
                TextureData.Add(data);
            }

            //Read texture size for each texture
            for (int i = 0; i < TexNum; i++)
            {
                TextureSize.Add(binaryStream.ReadUInt32());
            }

            //Read each filename string 
            for (int i = 0; i < TexNum; i++)
            {
                FileName.Add(binaryStream.ReadShiftJISString());
            }

            //Align reader after strings incase strings are odd in length
            while ((int)binaryStream.Tell() % 4 != 0)
            {
                binaryStream.ReadByte();
            }

            //Read Hash info for each texture
            for (int i = 0; i < TexNum; i++)
            {
                CTPKHash hash = new CTPKHash();
                hash.Read(binaryStream);
                NameHash.Add(hash);
            }

            //Read Texture info for each file
            for (int i = 0; i < TexNum; i++)
            {
                CTPKInfo info = new CTPKInfo();
                info.Read(binaryStream);
                FormatInfo.Add(info);
            }

            binaryStream.Seek(TexDataOffset, System.IO.SeekOrigin.Begin);

            //Read Raw Texture bytes
            for (int i = 0; i < TexNum; i++)
            {
                Textures.Add(binaryStream.ReadBytes((int)TextureSize[i]));
            }
        }

    }

    public class CTPKData
    {
        public uint NameOffset { get; set; } //real location in ctpk
        public uint ImageSize { get; set; }
        public uint TexDataOffset { get; set; } //relative to texture data start
        public uint Format { get; set; }
        public ushort Width { get; set; }
        public ushort Hight { get; set; }
        public byte Mipmap { get; set; }
        public byte Type { get; set; }
        public ushort CubeMap { get; set; }
        public uint BitmapSizeOffset { get; set; }
        public uint TimeStamp { get; set; }

        public void Read(BinaryStream binaryStream)
        {
            NameOffset = binaryStream.ReadUInt32();
            ImageSize = binaryStream.ReadUInt32();
            TexDataOffset = binaryStream.ReadUInt32();
            Format = binaryStream.ReadUInt32();
            Width = binaryStream.ReadUInt16();
            Hight = binaryStream.ReadUInt16();
            Mipmap = binaryStream.ReadByte();
            Type = binaryStream.ReadByte();
            CubeMap = binaryStream.ReadUInt16();
            BitmapSizeOffset = binaryStream.ReadUInt32();
            TimeStamp = binaryStream.ReadUInt32();
        }
    }

    public class CTPKHash
    {
        public uint crc32Hash { get; set; }
        public uint Index { get; set; }

        public void Read(BinaryStream binaryStream)
        {
            crc32Hash = binaryStream.ReadUInt32();
            Index = binaryStream.ReadUInt32();
        }
    }

    public class CTPKInfo
    {
        public byte TextureFormat { get; set; }
        public byte Unk { get; set; }
        public byte Compression { get; set; }
        public byte ETCEncoding { get; set; }

        public void Read(BinaryStream binaryStream)
        {
            TextureFormat = binaryStream.ReadByte();
            Unk = binaryStream.ReadByte();
            Compression = binaryStream.ReadByte();
            ETCEncoding = binaryStream.ReadByte();
        }
    }

    public enum CTPKFormat
    {
        Null = -1,
        Rgba8 = 0,
        Rgb8 = 1,
        Rgb5551 = 2,
        Rgb565 = 3,
        Rgba4 = 4,
        La8 = 5,
        Hilo8 = 6,
        L8 = 7,
        A8 = 8,
        La4 = 9,
        L4 = 10,
        A4 = 11,
        Etc1 = 12,
        Etc1A4 = 13
    }
}
