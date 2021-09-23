using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FE3D.GovanifY;
using FE3D.IO;

namespace FE3D
{
    public class FEArc
    {
        private uint Filesize { get; set; } = 0;
        private uint DataRegionSize { get; set; } = 0;
        private uint PTR1Count {  get; set; } = 0;
        private uint PTR2Count {  get; set; } = 0;
        private byte[] DataRegion {  get; set; } = new byte[0];
        private uint count {  get; set; } = 0;
        private List<FileInfos> fileInfos { get; set; } = new List<FileInfos>();
        private List<uint> PTR1 {  get; set; } = new List<uint>();
        private List<PTR2Data> PTR2 {  get; set; } = new List<PTR2Data>();
        private byte[] StringTable {  get; set; } = new byte[0];
        public List<byte[]> Files {  get; set; } = new List<byte[]>();
        public List<string> FileNames {  get; set; } = new List<string>();
        public bool Padding { get; set; } = true;
        public uint Alignment { get; set; } = 128;

        /// <summary>
        /// Reads an FEArc.
        /// </summary>
        /// <param name="binaryStream"></param>
        public void Read(BinaryStream binaryStream)
        {
            var ShiftJIS = Encoding.GetEncoding(932);
            fileInfos = new List<FileInfos>();

            //Read binary data of arc file
            Filesize = binaryStream.ReadUInt32();
            DataRegionSize = binaryStream.ReadUInt32();
            PTR1Count = binaryStream.ReadUInt32();
            PTR2Count = binaryStream.ReadUInt32();

            binaryStream.Seek(0x20, SeekOrigin.Begin);

            //Skip Over padding
            if (binaryStream.ReadInt32() == 0)
            {
                binaryStream.Seek(0x80, SeekOrigin.Begin);
                Padding = true;
            }
            else
            {
                binaryStream.Seek(0x20, SeekOrigin.Begin);
                Padding = false;
            }

            DataRegion = binaryStream.ReadBytes(Convert.ToInt32(DataRegionSize - (4 + (PTR1Count * 16)) - (Padding ? 0x60 : 0)));
            count = binaryStream.ReadUInt32();

            for (int i = 0; i < count; i++)
            {
                FileInfos file = new FileInfos();
                file.Read(binaryStream);
                fileInfos.Add(file);
            }


            binaryStream.Seek((PTR1Count * 4) + (PTR2Count * 8), SeekOrigin.Current);

            StringTable = binaryStream.ReadBytes(Convert.ToInt32(Filesize - (DataRegionSize + (PTR1Count * 4) + (PTR2Count * 8))));

            //Convert data into FEArc object
            Files = new List<byte[]>();
            FileNames = new List<string>();
            for (int i = 0; i < count; i++)
            {
                byte[] file = FEIO.ReadBytesFromArray(DataRegion, fileInfos[i].FileSize, fileInfos[i].Offset);
                Files.Add(file);

                uint offset = fileInfos[i].NameOffset - (DataRegionSize + (PTR1Count * 4) + (PTR2Count * 8));
                string name = FEIO.ReadStringFromArray(StringTable, ShiftJIS, offset);
                FileNames.Add(name);
            }

            if (FileNames.Count != Files.Count)
                Console.WriteLine("Error Reading file, file count does not match name count");
        }

        /// <summary>Extracts an .arc archive file used in 3ds FE games.</summary>
        /// <param name="outdir"> Output folder where files will be extracted to</param>
        /// <param name="archive"> Input bytes to read</param>
        public static void ExtractArc(string ourdir, byte[] archive)
        {
            if (Directory.Exists(ourdir))
                Directory.Delete(ourdir, true);
            Directory.CreateDirectory(ourdir);

            MemoryStream ms = new MemoryStream(archive);
            BinaryStream bs = new BinaryStream(ms);

            FEArc arc = new FEArc();
            arc.Read(bs);

            for (int i = 0; i < arc.Files.Count; i++)
            {
                File.WriteAllBytes(ourdir + Path.DirectorySeparatorChar + arc.FileNames[i], arc.Files[i]);
            }
            ms.Close();
        }

        /// <summary>
        /// Writes an FEArc
        /// </summary>
        /// <param name="binaryStream"></param>
        public void Write(BinaryStream binaryStream)
        {
            var ShiftJIS = Encoding.GetEncoding(932);
            MemoryStream ms = new MemoryStream();
            BinaryWriter Strings = new BinaryWriter(ms);
            PTR2 = new List<PTR2Data>();

            //Write Base Strings table
            var datcount = new byte[] { 0x44, 0x61, 0x74, 0x61, 0x00, 0x43, 0x6F, 0x75, 0x6E, 0x74, 0x00, 0x49, 0x6E, 0x66, 0x6F, 0x00 };
            Strings.Write(datcount);

            //Write all file data into the data table
            List<byte> Data = new List<byte>();
            fileInfos = new List<FileInfos>();
            for (int i = 0; i < Files.Count; i++)
            {
                FileInfos infos = new FileInfos() { NameOffset = ((uint)ms.Position), Index = (uint)i, FileSize = (uint)Files[i].Length, Offset = (uint)Data.Count};
                foreach (var file in Files[i])
                {
                    Data.Add(file);
                }
                if (Alignment != 0)
                {
                    while (Data.Count % Alignment != 0)
                        Data.Add(0);
                }

                //Align final file by 4 if no alignment is used
                if (i == Files.Count - 1 && Alignment == 0)
                {
                    while (Data.Count % 4 != 0)
                        Data.Add(0);
                }
                
                Strings.Write(ShiftJIS.GetBytes(FileNames[i]));
                Strings.Write((byte)0);
                fileInfos.Add(infos);
            }
            //Set data
            DataRegion = Data.ToArray();
            DataRegionSize = (uint)((Padding ? 0x60 : 0) + DataRegion.Length + (Files.Count * 16) + 4);
            PTR1Count = (uint)Files.Count;
            PTR2Count = (uint)Files.Count + 3;
            count = (uint)Files.Count;

            //Write Data
            binaryStream.Write(0);
            binaryStream.Write(DataRegionSize);
            binaryStream.Write(PTR1Count);
            binaryStream.Write(PTR2Count);
            binaryStream.Write(new byte[Padding ? 0x70 : 0x10]);

            PTR2.Add(new PTR2Data { Location = (uint)binaryStream.Tell() - 0x20, Destination = 0});
            binaryStream.Write(DataRegion);
            PTR2.Add(new PTR2Data { Location = (uint)binaryStream.Tell() - 0x20, Destination = 5 });
            binaryStream.Write(count);
            PTR2.Add(new PTR2Data { Location = (uint)binaryStream.Tell() - 0x20, Destination = 11 });

            //Write file info
            foreach (FileInfos infos in fileInfos)
            {
                PTR2.Add(new PTR2Data { Location = (uint)binaryStream.Tell() - 0x20, Destination = infos.NameOffset });
                //recalculates name offset
                infos.NameOffset = (uint)(infos.NameOffset + DataRegionSize + (PTR1Count * 4) + (PTR2Count * 8));
                
                PTR1.Add((uint)binaryStream.Tell() - 0x20);
                infos.Write(binaryStream);
            }

            //Write Pointer 1 region
            foreach (uint ptr1 in PTR1)
            {
                binaryStream.Write(ptr1);
            }

            //Write Pointer 2 region
            foreach (PTR2Data ptr2 in PTR2)
            {
                ptr2.Write(binaryStream);
            }

            //Write String Table
            binaryStream.Write(ms.ToArray());
            long end = binaryStream.Tell();

            binaryStream.Seek(0, SeekOrigin.Begin);
            binaryStream.Write((uint)end);

            ms.Close();
        }

        /// <summary>
        /// Packs files into an Arc
        /// </summary>
        /// <param name="inpath"></param>
        public static void PackArc(string inpath, uint Alignment = 128, bool Padding = true)
        {
            FEArc arc = new FEArc();
            string[] files = Directory.GetFiles(inpath, "*", SearchOption.AllDirectories);

            arc.Files = new List<byte[]>();
            arc.FileNames = new List<string>();
            arc.Alignment = Alignment;
            arc.Padding = Padding;

            foreach (string file in files)
            {
                arc.Files.Add(File.ReadAllBytes(file));
                arc.FileNames.Add(file.Replace(inpath + Path.DirectorySeparatorChar, "").Replace("\\", "/"));
            }

            MemoryStream memoryStream = new MemoryStream();
            BinaryStream newFile = new BinaryStream(memoryStream);

            arc.Write(newFile);

            string outpath = inpath + ".arc";
            File.WriteAllBytes(outpath, memoryStream.ToArray());
            memoryStream.Close();
        }

        public class FileInfos
        {
            public uint NameOffset {  get; set; }
            public uint Index {  get; set; }
            public uint FileSize {  get; set; } 
            public uint Offset {  get; set; }

            public void Read(BinaryStream binaryStream)
            {
                NameOffset = binaryStream.ReadUInt32();
                Index = binaryStream.ReadUInt32();
                FileSize = binaryStream.ReadUInt32();
                Offset = binaryStream.ReadUInt32();
            }

            public void Write(BinaryStream binaryStream)
            {
                binaryStream.Write(NameOffset);
                binaryStream.Write(Index);
                binaryStream.Write(FileSize);
                binaryStream.Write(Offset);
            }
        }

        public class PTR2Data
        {
            public uint Location { get; set; }
            public uint Destination { get; set; }

            public void Read(BinaryStream binaryStream)
            {
                Location = binaryStream.ReadUInt32();
                Destination = binaryStream.ReadUInt32();
            }

            public void Write(BinaryStream binaryStream)
            {
                binaryStream.Write(Location);
                binaryStream.Write(Destination);
            }
        }
    }
    public static class FEArcOld
    {
        /// <summary>Creates an .arc archive file used in 3ds FE games</summary>
        /// <param name="inpath"> Input folder the containts the files to pack</param>
        /// <param name="Alignment"> Padding used to align files</param>
        /// <param name="Padding"> Start of file padding</param>
        public static byte[] CreateArc(string inpath, int Alignment = 128, bool Padding = true)
        {
            string[] files = Directory.GetFiles(inpath, "*", SearchOption.AllDirectories);

            List<byte[]> newfiles = new List<byte[]>();
            List<string> names = new List<string>();
            
            foreach (string file in files)
            {
                newfiles.Add(File.ReadAllBytes(file));
                names.Add(file.Replace(inpath + Path.DirectorySeparatorChar, "").Replace("\\", "/"));
            }

            return MakeArc(newfiles, names.ToArray(), Alignment, Padding);
        }

        /// <summary>Creates an .arc archive file used in 3ds FE games</summary>
        /// <param name="infiles"> Bytes of files to add</param>
        /// <param name="innames"> Names of files to add</param>
        /// <param name="Alignment"> Padding used to align files</param>
        /// <param name="Padding"> Start of file padding</param>
        public static byte[] CreateArc(List<byte[]> infiles, string[] innames, int Alignment = 128, bool Padding = true)
        {
            return MakeArc(infiles, innames, Alignment, Padding);
        }


        private static byte[] MakeArc(List<byte[]> infiles, string[] files, int Alignment, bool Padding)
        {
            MemoryStream newfilestream = new MemoryStream();

            uint FileCount = (uint)infiles.Count;
            Console.WriteLine($"{FileCount} files detected!");

            var ShiftJIS = Encoding.GetEncoding(932);

            BinaryStream newFile = new BinaryStream(newfilestream);

            MemoryStream infos = new MemoryStream();
            BinaryWriter FileInfos = new BinaryWriter(infos);


            Console.WriteLine("Creating dummy header...");
            newFile.Write(0);

            newFile.Write(0);
            newFile.Write(FileCount);
            newFile.Write(FileCount + 3);

            byte nil = 0;
            if (Padding)
            {
                for (int i = 0; i < 0x70; i++)
                {
                    newFile.Write(nil);
                }
            }
            else
            {
                for (int i = 0; i < 0x10; i++)
                {
                    newFile.Write(nil);
                }
            }
            int z = 0;
            foreach (byte[] filetoadd in infiles)
            {
                uint fileoff = (uint)newFile.Tell();
                newFile.Write(filetoadd); //File is written to arc
                if (Alignment != 0)
                {
                    while ((int)newFile.Tell() % Alignment != 0)
                    {
                        newFile.Write(nil); //writes between file padding
                    }
                }
                FileInfos.Write(0);
                FileInfos.Write(z);
                FileInfos.Write(filetoadd.Length);
                if (Padding)
                {
                    FileInfos.Write(fileoff - 0x80);
                }
                else
                {
                    FileInfos.Write(fileoff - 0x20);
                }
                z++;
            }

            if (Alignment == 0)
            {
                while ((int)newFile.Tell() % 4 != 0)
                {
                    newFile.Write(nil); //align bytes after files are added if no aliment is being used
                }
            }

            long countinfo = newFile.Tell();
            newFile.Write(FileCount);
            long infopointer = newFile.Tell();
            Console.WriteLine("Adding dummy FileInfos...");

            infos.Seek(0, SeekOrigin.Begin);
            var infopos = newFile.Tell();
            newFile.Write(infos.ToArray());

            Console.WriteLine("Rewriting header...");
            long metapos = newFile.Tell();
            newFile.Seek(4, SeekOrigin.Begin);
            newFile.Write((uint)metapos - 0x20);

            newFile.Seek(metapos, SeekOrigin.Begin);

            Console.WriteLine("Adding FileInfos pointer...");
            for (int i = 0; i < FileCount; i++)
            {
                newFile.Write((uint)((infopointer + i * 16) - 0x20));
            }

            Console.WriteLine("Adding Dummy Pointer 2 Region...");

            if (Padding)
            {
                newFile.Write((uint)0x60);
            }
            else
            {
                newFile.Write((uint)0x0);
            }
            newFile.Write(0);
            newFile.Write((uint)(countinfo - 0x20));
            newFile.Write((uint)5);
            newFile.Write((uint)(countinfo + 4 - 0x20));
            newFile.Write((uint)0xB);
            long ptr2region = newfilestream.Position;
            for (int i = 0; i < FileCount; i++)
            {
                newFile.Write((uint)((countinfo + 4) + i * 16) - 0x20);
                if (i == 0)
                {
                    newFile.Write((uint)0x10);
                }
                else
                {
                    if (i == 1)
                    {
                        newFile.Write((uint)0x1C);
                    }
                    else
                    {
                        newFile.Write((uint)(0x1C + (10 * (i - 1))));
                    }
                }
            }

            Console.WriteLine("Adding Filenames and Rewriting Pointer 2 Region...");
            var datcount = new byte[] { 0x44, 0x61, 0x74, 0x61, 0x00, 0x43, 0x6F, 0x75, 0x6E, 0x74, 0x00, 0x49, 0x6E, 0x66, 0x6F, 0x00 };
            newFile.Write(datcount);
            int y = 0;
            int nameloc = 16;

            foreach (string fileName in files)
            {
                FileInfos.Seek(y * 16, SeekOrigin.Begin);
                long namepos = newFile.Tell();
                FileInfos.Write((uint)namepos - 0x20);
                newFile.Write(ShiftJIS.GetBytes(fileName.Replace("\\", "/")));
                newFile.Write(nil);
                long NameEnd = newfilestream.Position;
                long pointerpos = (ptr2region + 4 + (y * 8));
                newFile.Seek(pointerpos, SeekOrigin.Begin);
                newFile.Write(nameloc);
                newFile.Seek(NameEnd, SeekOrigin.Begin);
                byte[] onlyfilename = ShiftJIS.GetBytes(fileName.Replace("\\", "/"));
                nameloc = nameloc + onlyfilename.Length + 1;
                y++;
            }
            Console.WriteLine("Rewriting FileInfos...");
            newFile.Seek(infopos, SeekOrigin.Begin);

            infos.Seek(0, SeekOrigin.Begin);
            newFile.Write(infos.ToArray());

            Console.WriteLine("Finishing the job...");
            newFile.Seek(0, SeekOrigin.Begin);
            UInt32 newlength = (UInt32)newFile.BaseStream.Length;
            newFile.Write(newlength);

            Console.WriteLine("Done!");
            newFile.Close();

            return newfilestream.ToArray();
        }


        /// <summary>Extracts an .arc archive file used in 3ds FE games</summary>
        /// <param name="outdir"> Output folder where files will be extracted to</param>
        /// <param name="archive"> Input bytes to read</param>
        public static void ExtractArc(string outdir, byte[] archive)
        {
            if (Directory.Exists(outdir))
                Directory.Delete(outdir, true);
            Directory.CreateDirectory(outdir);

            var ShiftJIS = Encoding.GetEncoding(932);

            uint MetaOffset = BitConverter.ToUInt32(archive, 4) + 0x20;
            uint FileCount = BitConverter.ToUInt32(archive, 0x8);

            bool awakening = (BitConverter.ToUInt32(archive, 0x20) != 0);

            for (int i = 0; i < FileCount; i++)
            {
                int FileMetaOffset = 0x20 + BitConverter.ToInt32(archive, (int)MetaOffset + 4 * i);
                int FileNameOffset = BitConverter.ToInt32(archive, FileMetaOffset) + 0x20;
                // int FileIndex = BitConverter.ToInt32(archive, FileMetaOffset + 4);
                uint FileDataLength = BitConverter.ToUInt32(archive, FileMetaOffset + 8);
                int FileDataOffset = BitConverter.ToInt32(archive, FileMetaOffset + 0xC) + (awakening ? 0x20 : 0x80);
                byte[] file = new byte[FileDataLength];
                Array.Copy(archive, FileDataOffset, file, 0, FileDataLength);
                string outpath = outdir + ShiftJIS.GetString(archive.Skip(FileNameOffset).TakeWhile(b => b != 0).ToArray());
                if (!Directory.Exists(Path.GetDirectoryName(outpath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(outpath));
                File.WriteAllBytes(outpath, file);
            }
        }

        /// <summary>Extracts an .arc archive file used in 3ds FE games</summary>
        /// <param name="outdir"> Output folder where files will be extracted to</param>
        /// <param name="archive"> Input path to arc file</param>
        public static void ExtractArc(string outdir, string archive)
        {
            if (Directory.Exists(outdir))
                Directory.Delete(outdir, true);
            Directory.CreateDirectory(outdir);

            byte[] arc = File.ReadAllBytes(archive);
            var ShiftJIS = Encoding.GetEncoding(932);

            uint MetaOffset = BitConverter.ToUInt32(arc, 4) + 0x20;
            uint FileCount = BitConverter.ToUInt32(arc, 0x8);

            bool awakening = (BitConverter.ToUInt32(arc, 0x20) != 0);

            for (int i = 0; i < FileCount; i++)
            {
                int FileMetaOffset = 0x20 + BitConverter.ToInt32(arc, (int)MetaOffset + 4 * i);
                int FileNameOffset = BitConverter.ToInt32(arc, FileMetaOffset) + 0x20;
                // int FileIndex = BitConverter.ToInt32(archive, FileMetaOffset + 4);
                uint FileDataLength = BitConverter.ToUInt32(arc, FileMetaOffset + 8);
                int FileDataOffset = BitConverter.ToInt32(arc, FileMetaOffset + 0xC) + (awakening ? 0x20 : 0x80);
                byte[] file = new byte[FileDataLength];
                Array.Copy(arc, FileDataOffset, file, 0, FileDataLength);
                string outpath = outdir + ShiftJIS.GetString(arc.Skip(FileNameOffset).TakeWhile(b => b != 0).ToArray());
                if (!Directory.Exists(Path.GetDirectoryName(outpath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(outpath));
                File.WriteAllBytes(outpath, file);
            }
        }

        public static List<byte[]> ExtractArcToMemory(byte[] archive)
        {
            List<byte[]> files = new List<byte[]>();
            var ShiftJIS = Encoding.GetEncoding(932);

            uint MetaOffset = BitConverter.ToUInt32(archive, 4) + 0x20;
            uint FileCount = BitConverter.ToUInt32(archive, 0x8);

            bool awakening = (BitConverter.ToUInt32(archive, 0x20) != 0);

            for (int i = 0; i < FileCount; i++)
            {
                int FileMetaOffset = 0x20 + BitConverter.ToInt32(archive, (int)MetaOffset + 4 * i);
                int FileNameOffset = BitConverter.ToInt32(archive, FileMetaOffset) + 0x20;
                // int FileIndex = BitConverter.ToInt32(archive, FileMetaOffset + 4);
                uint FileDataLength = BitConverter.ToUInt32(archive, FileMetaOffset + 8);
                int FileDataOffset = BitConverter.ToInt32(archive, FileMetaOffset + 0xC) + (awakening ? 0x20 : 0x80);
                byte[] file = new byte[FileDataLength];
                Array.Copy(archive, FileDataOffset, file, 0, FileDataLength);
                files.Add(file);
            }

            return files;
        }

        public static string[] ExtractArcNames(byte[] archive)
        {
            List<string> names = new List<string>();

            var ShiftJIS = Encoding.GetEncoding(932);

            uint MetaOffset = BitConverter.ToUInt32(archive, 4) + 0x20;
            uint FileCount = BitConverter.ToUInt32(archive, 0x8);

            bool awakening = (BitConverter.ToUInt32(archive, 0x20) != 0);

            for (int i = 0; i < FileCount; i++)
            {
                int FileMetaOffset = 0x20 + BitConverter.ToInt32(archive, (int)MetaOffset + 4 * i);
                int FileNameOffset = BitConverter.ToInt32(archive, FileMetaOffset) + 0x20;
                // int FileIndex = BitConverter.ToInt32(archive, FileMetaOffset + 4);
                uint FileDataLength = BitConverter.ToUInt32(archive, FileMetaOffset + 8);
                int FileDataOffset = BitConverter.ToInt32(archive, FileMetaOffset + 0xC) + (awakening ? 0x20 : 0x80);
                byte[] file = new byte[FileDataLength];
                Array.Copy(archive, FileDataOffset, file, 0, FileDataLength);
                string outpath = ShiftJIS.GetString(archive.Skip(FileNameOffset).TakeWhile(b => b != 0).ToArray());

                names.Add(outpath);
            }

            return names.ToArray();
        }
    }
}
