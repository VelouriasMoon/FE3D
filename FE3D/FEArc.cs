using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FE3D.GovanifY;

namespace FE3D
{
    public static class FEArc
    {
        /// <summary>Creates an .arc archive file used in 3ds FE games</summary>
        /// <param name="inpath"> Input folder the containts the files to pack</param>
        /// <param name="Alignment"> Padding used to align files</param>
        /// <param name="Padding"> Start of file padding</param>
        public static void CreateArc(string inpath, int Alignment = 128, bool Padding = true)
        {
            string outfile = $"{Path.GetDirectoryName(inpath)}//{Path.GetFileNameWithoutExtension(inpath)}.arc";
            Console.WriteLine($"Creating archive {Path.GetFileName(outfile)}");
            FileStream newfilestream = File.Create(outfile);
            string[] files = Directory.GetFiles(inpath, "*", SearchOption.AllDirectories);

            uint FileCount = (uint)files.Length;
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
            foreach (string fileName in files)
            {
                Console.WriteLine("Adding file {0}...", fileName.Replace(inpath + Path.DirectorySeparatorChar, "").Replace("\\", "/"));
                byte[] filetoadd = File.ReadAllBytes(fileName);
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
            while ((int)newFile.Tell() % 4 != 0)
            {
                newFile.Write(nil); //align bytes after files are added
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
                newFile.Write(ShiftJIS.GetBytes(fileName.Replace(inpath + Path.DirectorySeparatorChar, "").Replace("\\", "/")));
                newFile.Write(nil);
                long NameEnd = newfilestream.Position;
                long pointerpos = (ptr2region + 4 + (y * 8));
                newFile.Seek(pointerpos, SeekOrigin.Begin);
                newFile.Write(nameloc);
                newFile.Seek(NameEnd, SeekOrigin.Begin);
                byte[] onlyfilename = ShiftJIS.GetBytes(fileName.Replace(inpath + Path.DirectorySeparatorChar, "").Replace("\\", "/"));
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
        }

        public static byte[] CreatArcFromMemory(List<byte[]> infiles, string[] files, int Alignment = 128, bool Padding = true)
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
            while ((int)newFile.Tell() % 4 != 0)
            {
                newFile.Write(nil); //align bytes after files are added
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
