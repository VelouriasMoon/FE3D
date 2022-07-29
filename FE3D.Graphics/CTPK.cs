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
        public ushort TexNum { get; set; } = 0;                  //number of textures stored
        public uint TexDataOffset { get; set; } = 0;             //pointer to the raw texture data
        public uint TotalTexSize { get; set; } = 0;              //total size of all texture data
        public uint HashOffset { get; set; } = 0;                //Pointer to the hash data location
        public uint FormatInfoOffset { get; set; } = 0;          //Pointer to the Format Info location
        public List<CTPKData> TextureData { get; set; } = new List<CTPKData>();     //Data for each texture split into 0x20 sized sections
        public List<uint> TextureSize { get; set; } = new List<uint>();         //Each image size stored as a uint
        public List<string> FileName { get; set; } = new List<string>();          //Each file name stored null terminate Shift-JIS encoded strings
        public List<CTPKHash> NameHash { get; set; } = new List<CTPKHash>();        //Name hash info for each image split into 0x8 sized sections
        public List<CTPKInfo> FormatInfo { get; set; } = new List<CTPKInfo>();      //Format info for each image split into 0x4 sized sections
        public List<Bitmap> Textures { get; set; } = new List<Bitmap>();          //Raw bytes for each image based on image encoding

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

            for (int i = 0; i < TextureSize.Count; i++)
            {
                TextureData[i].BitmapSizeOffset = (uint)(binaryStream.Tell() / 4);
                binaryStream.Write(TextureSize[i]);
            }

            for (int i = 0; i < FileName.Count; i++)
            {
                TextureData[i].NameOffset = (uint)binaryStream.Tell();
                binaryStream.Write(ShiftJIS.GetBytes(FileName[i]));
                binaryStream.Write((byte)0);
            }

            long hashoffset = binaryStream.Tell();
            foreach (CTPKHash hash in NameHash)
            {
                hash.Write(binaryStream);
            }

            long infooffset = binaryStream.Tell();
            foreach (CTPKInfo info in FormatInfo)
            {
                info.Write(binaryStream);
            }

            while ((int)binaryStream.Tell() % 0x80 != 0)
            {
                binaryStream.Write((byte)0);
            }

            long textureoffset = binaryStream.Tell();
            for (int i = 0;  i < Textures.Count; i++)
            {
                TextureData[i].TexDataOffset = (uint)(binaryStream.Tell() - textureoffset);
                binaryStream.Write(TextureConverter.Encode(Textures[i], (PICATextureFormat)imageformat));
            }

            //Rewrite Data properly
            binaryStream.Seek(8, SeekOrigin.Begin);
            binaryStream.Write((uint)textureoffset);
            binaryStream.Seek(4, SeekOrigin.Current);
            binaryStream.Write((uint)hashoffset);
            binaryStream.Write((uint)infooffset);
            binaryStream.Seek(8, SeekOrigin.Current);

            foreach (CTPKData data in TextureData)
            {
                data.Write(binaryStream);
            }
        }

        public static void MakeCTPK(string inpath, CTPKFormat imageformat = CTPKFormat.Rgba8)
        {
            if (imageformat != CTPKFormat.Rgba8)
                throw new NotImplementedException();
            CTPK ctpk = new CTPK();
            var ShiftJIS = Encoding.GetEncoding(932);
            string[] files = Directory.GetFiles(inpath, "*", SearchOption.AllDirectories);

            int i = 0;
            foreach (string file in files)
            {
                Bitmap texture;
                try
                {
                    texture = (Bitmap)Bitmap.FromFile(file);
                }
                catch (Exception e)
                {
                    i++;
                    continue;
                }

                ctpk.TextureData.Add(new CTPKData() { Width = (ushort)texture.Width, Height = (ushort)texture.Height, Format = imageformat });
                ctpk.TextureSize.Add(Convert.ToUInt32(TextureConverter.CalculateLength(texture.Width, texture.Height, (PICATextureFormat)imageformat)));
                ctpk.NameHash.Add(new CTPKHash() { crc32Hash = CRC32Hash.Hash(ShiftJIS.GetBytes(Path.GetFileName(file).Replace(Path.GetExtension(file), ""))), Index = (uint)i });
                ctpk.FormatInfo.Add(new CTPKInfo() { TextureFormat = imageformat, Compression = false, ETCEncoding = 8 });

                ctpk.FileName.Add(Path.GetFileName(file.Replace(Path.GetExtension(file),"")));
                ctpk.Textures.Add(texture);
                ctpk.TexNum += Convert.ToUInt16(i + 1);
                ctpk.TotalTexSize += Convert.ToUInt32(TextureConverter.CalculateLength(texture.Width, texture.Height, (PICATextureFormat)imageformat));
                i++;
            }

            MemoryStream ms = new MemoryStream();
            BinaryStream bs = new BinaryStream(ms);
            ctpk.Write(bs);
            string outpath = inpath + ".ctpk";
            File.WriteAllBytes(outpath, ms.ToArray());
            ms.Close();
        }

        public static void ExtractCTPK(string infile)
        {
            string outpath = infile.Replace(Path.GetExtension(infile), "");
            if (Directory.Exists(outpath))
                Directory.Delete(outpath, true);
            Directory.CreateDirectory(outpath);
            using (FileStream fs = new FileStream(infile, FileMode.Open))
            {
                BinaryStream bs = new BinaryStream(fs);
                CTPK ctpk = new CTPK();
                ctpk.Read(bs);

                foreach(Bitmap image in ctpk.Textures)
                {
                    string filename = ctpk.FileName[ctpk.Textures.IndexOf(image)];
                    if (filename.Contains("R:/"))
                        filename = Path.GetFileNameWithoutExtension(filename.Replace("/","\\"));
                    image.Save($"{outpath}\\{Path.GetFileNameWithoutExtension(filename)}.png");
                }
            }
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
            public ushort CubeMap { get; set; } = 0; //used for cubemaps
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
                CubeMap = binaryStream.ReadUInt16();
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