using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FE3D.GovanifY;
using SPICA.PICA.Converters;
using SPICA.PICA.Commands;
using SPICA.Formats.Common;
using System.IO;

namespace FE3D.Image
{
    public class CTPK
    {
        public string Magic { get; } = "CTPK";              //Always CTPK
        public ushort Version { get; set; } = 1;            //only seen 1 but other versions might exist
        public ushort TexNum { get; set; }                  //number of textures stored
        public uint TexDataOffset { get; set; }             //pointer to the raw texture data
        public uint TotalTexSize { get; set; }              //total size of all texture data
        public uint HashOffset { get; set; }                //Pointer to the hash data location
        public uint FormatInfoOffset { get; set; }          //Pointer to the Format Info location
        public List<CTPKData> TextureData { get; set; }     //Data for each texture split into 0x20 sized sections
        public List<uint> TextureSize { get; set; }         //Each image size stored as a uint
        public List<string> FileName { get; set; }          //Each file name stored null terminate Shift-JIS encoded strings
        public List<CTPKHash> NameHash { get; set; }        //Name hash info for each image split into 0x8 sized sections
        public List<CTPKInfo> FormatInfo { get; set; }      //Format info for each image split into 0x4 sized sections
        public List<Bitmap> Textures { get; set; }          //Raw bytes for each image based on image encoding

        public void Read(BinaryStream binaryStream)
        {
            TextureData = new List<CTPKData>();
            TextureSize = new List<uint>();
            FileName = new List<string>();
            NameHash = new List<CTPKHash>();
            FormatInfo = new List<CTPKInfo>();
            Textures = new List<Bitmap>();


            //Read header data
            if (Magic != Encoding.UTF8.GetString(binaryStream.ReadBytes(4)))
            {
                throw new InvalidOperationException("Input File not valid CTPK file");
            }
                
            Version = binaryStream.ReadUInt16();
            TexNum = binaryStream.ReadUInt16();
            TexDataOffset = binaryStream.ReadUInt32();
            TotalTexSize = binaryStream.ReadUInt32();
            HashOffset = binaryStream.ReadUInt32();
            FormatInfoOffset = binaryStream.ReadUInt32();

            //Skip header padding
            binaryStream.Seek(8, System.IO.SeekOrigin.Current);

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

            //Skip section padding
            binaryStream.Seek(TexDataOffset, System.IO.SeekOrigin.Begin);

            //Read Raw Texture bytes
            for (int i = 0; i < TexNum; i++)
            {
                Bitmap texture = TextureConverter.DecodeBitmap(binaryStream.ReadBytes((int)TextureSize[i]), TextureData[i].Width, TextureData[i].Height, (PICATextureFormat)TextureData[i].Format);
                Textures.Add(texture);
            }
        }

        public void Write(BinaryStream binaryStream, CTPKFormat imageformat = CTPKFormat.Rgba8)
        {
            if (imageformat != CTPKFormat.Rgba8)
                throw new NotImplementedException();
            var ShiftJIS = Encoding.GetEncoding(932);

            //Write data first pass
            binaryStream.Write(Encoding.UTF8.GetBytes("CTPK")); 
            binaryStream.Write(Version); 
            binaryStream.Write(TexNum); 
            binaryStream.Write(TexDataOffset); 
            binaryStream.Write(TotalTexSize); 
            binaryStream.Write(HashOffset);
            binaryStream.Write(FormatInfoOffset);
            binaryStream.Write((ulong)0);

            foreach (CTPKData data in TextureData)
            {
                data.Write(binaryStream);
            }

            foreach (uint size in TextureSize)
            {
                binaryStream.Write(size);
            }

            foreach (string name in FileName)
            {
                binaryStream.Write(ShiftJIS.GetBytes(name));
                binaryStream.Write((byte)0);
            }

            foreach (CTPKHash hash in NameHash)
            {
                hash.Write(binaryStream);
            }

            foreach (CTPKInfo info in FormatInfo)
            {
                info.Write(binaryStream);
            }

            while ((int)binaryStream.Tell() % 0x80 != 0)
            {
                binaryStream.Write((byte)0);
            }

            foreach (Bitmap texture in Textures)
            {
                binaryStream.Write(TextureConverter.Encode(texture, (PICATextureFormat)imageformat));
            }

            //Rewrite Data properly
            binaryStream.Seek(0, SeekOrigin.Begin);
        }

        public CTPK Makectpk(string[] infiles, CTPKFormat imageformat = CTPKFormat.Rgba8)
        {
            if (imageformat != CTPKFormat.Rgba8)
                throw new NotImplementedException();
            CTPK ctpk = new CTPK();
            var ShiftJIS = Encoding.GetEncoding(932);

            ctpk.TexNum = (ushort)infiles.Length;

            for (int i = 0; i < infiles.Length; i++)
            {
                Bitmap texture = (Bitmap)Bitmap.FromFile(infiles[i]);

                ctpk.TextureData.Add(new CTPKData() { Width = (ushort)texture.Width, Height = (ushort)texture.Height, Format = imageformat });
                ctpk.TextureSize.Add(0);
                ctpk.NameHash.Add(new CTPKHash() { crc32Hash = CRC32Hash.Hash(ShiftJIS.GetBytes(Path.GetFileName(infiles[i]))),  Index = (uint)i});
                ctpk.FormatInfo.Add(new CTPKInfo() { TextureFormat = imageformat, Compression = false, ETCEncoding = 8});

                ctpk.FileName.Add(Path.GetFileName(infiles[i]));
                ctpk.Textures.Add(texture);
            }

            return ctpk;
        }

        public class CTPKData
        {
            public uint NameOffset { get; set; } //real location in ctpk
            public uint ImageSize { get; set; } //size of image data after encoding
            public uint TexDataOffset { get; set; } //relative to texture data start
            public CTPKFormat Format { get; set; } //encoding format
            public ushort Width { get; set; } 
            public ushort Height { get; set; }
            public byte Mipmap { get; set; } //number of mipmap
            public byte Type { get; set; } = 2; //only seen type 2 but there might be more
            public ushort CubeMap { get; } = 0; //used for cubemaps
            public uint BitmapSizeOffset { get; set; } //real location of size in Texture size list / 4
            public uint TimeStamp { get; set; } //timestamp when file was created, can be disable

            public void Read(BinaryStream binaryStream)
            {
                NameOffset = binaryStream.ReadUInt32();
                ImageSize = binaryStream.ReadUInt32();
                TexDataOffset = binaryStream.ReadUInt32();
                Format = (CTPKFormat)binaryStream.ReadUInt32();
                Width = binaryStream.ReadUInt16();
                Height = binaryStream.ReadUInt16();
                Mipmap = binaryStream.ReadByte();
                Type = binaryStream.ReadByte();
                //CubeMap = binaryStream.ReadUInt16();
                BitmapSizeOffset = binaryStream.ReadUInt32();
                TimeStamp = binaryStream.ReadUInt32();
            }

            public void Write(BinaryStream binaryStream)
            {
                binaryStream.Write(NameOffset);
                binaryStream.Write(ImageSize);
                binaryStream.Write(TexDataOffset);
                binaryStream.Write((uint)Format);
                binaryStream.Write(Width);
                binaryStream.Write(Height);
                binaryStream.Write(Mipmap);
                binaryStream.Write(Type);
                binaryStream.Write(CubeMap);
                binaryStream.Write(BitmapSizeOffset);
                binaryStream.Write(TimeStamp);
            }
        }

        public class CTPKHash
        {
            public uint crc32Hash { get; set; } //crc32 Hash of the file name
            public uint Index { get; set; } //Index of the texture hashed

            public void Read(BinaryStream binaryStream)
            {
                crc32Hash = binaryStream.ReadUInt32();
                Index = binaryStream.ReadUInt32();
            }

            public void Write(BinaryStream binaryStream)
            {
                binaryStream.Write(crc32Hash);
                binaryStream.Write(Index);
            }
        }

        public class CTPKInfo
        {
            public CTPKFormat TextureFormat { get; set; } //image encoding format
            public byte Unk { get; set; } = 1;
            public bool Compression { get; set; }
            public byte ETCEncoding { get; set; } //type of ETC encoding

            public void Read(BinaryStream binaryStream)
            {
                TextureFormat = (CTPKFormat)binaryStream.ReadByte();
                Unk = binaryStream.ReadByte();
                Compression = Convert.ToBoolean(binaryStream.ReadByte());
                ETCEncoding = binaryStream.ReadByte();
            }

            public void Write(BinaryStream binaryStream)
            {
                binaryStream.Write((byte)TextureFormat);
                binaryStream.Write(Unk);
                binaryStream.Write(Convert.ToByte(Compression));
                binaryStream.Write(ETCEncoding);
            }
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
