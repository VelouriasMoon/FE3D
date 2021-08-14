using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FE3D
{
    public static class FEMessage
    {
        public static byte[] MakeMessage(string infile)
        {
            return MakeFireEmblemMessageArchive(File.ReadAllLines(infile));
        }
        public static byte[] MakeMessage(string[] infile)
        {
            return MakeFireEmblemMessageArchive(infile);
        }
        public static string ExtractMessage(string outname, string infile)
        {
            return ExtractFireEmblemMessageArchive(outname, File.ReadAllBytes(infile));
        }
        public static string ExtractMessage(string outname, byte[] infile)
        {
            return ExtractFireEmblemMessageArchive(outname, infile);
        }

        private static byte[] MakeFireEmblemMessageArchive(string[] lines)
        {
            var ShiftJIS = Encoding.GetEncoding(932);
            int StringCount = lines.Length - 6;
            string[] Messages = new string[StringCount];
            string[] Names = new string[StringCount];
            uint[] MPos = new uint[StringCount];
            uint[] NPos = new uint[StringCount];
            for (int i = 6; i < lines.Length; i++)
            {
                int ind = lines[i].IndexOf(": ", StringComparison.Ordinal);
                Names[i - 6] = lines[i].Substring(0, ind);
                Messages[i - 6] = lines[i].Substring(ind + 2, lines[i].Length - (ind + 2)).Replace("\\n", "\n").Replace("\\r", "\r");
            }
            byte[] Header = new byte[0x20];
            byte[] StringTable;
            byte[] MetaTable = new byte[StringCount * 8];
            byte[] NamesTable;
            using (MemoryStream st = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(st))
                {
                    bw.Write(ShiftJIS.GetBytes(lines[0]));
                    bw.Write((byte)0);
                    while (bw.BaseStream.Position % 4 != 0)
                        bw.Write((byte)0);
                    for (int i = 0; i < StringCount; i++)
                    {
                        MPos[i] = (uint)bw.BaseStream.Position;
                        bw.Write(Encoding.Unicode.GetBytes(Messages[i]));
                        bw.Write((ushort)0);
                        while (bw.BaseStream.Position % 4 != 0)
                            bw.Write((byte)0);
                    }
                }
                StringTable = st.ToArray();
            }
            using (MemoryStream nt = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(nt))
                {
                    for (int i = 0; i < StringCount; i++)
                    {
                        NPos[i] = (uint)bw.BaseStream.Position;
                        bw.Write(ShiftJIS.GetBytes(Names[i]));
                        bw.Write((byte)0);
                    }
                }
                NamesTable = nt.ToArray();
            }
            for (int i = 0; i < StringCount; i++)
            {
                Array.Copy(BitConverter.GetBytes(MPos[i]), 0, MetaTable, (i * 8), 4);
                Array.Copy(BitConverter.GetBytes(NPos[i]), 0, MetaTable, (i * 8) + 4, 4);
            }
            byte[] Archive = new byte[Header.Length + StringTable.Length + MetaTable.Length + NamesTable.Length];
            Array.Copy(BitConverter.GetBytes(Archive.Length), Header, 4);
            Array.Copy(BitConverter.GetBytes(StringTable.Length), 0, Header, 4, 4);
            Array.Copy(BitConverter.GetBytes(StringCount), 0, Header, 0xC, 4);
            Array.Copy(Header, Archive, Header.Length);
            Array.Copy(StringTable, 0, Archive, Header.Length, StringTable.Length);
            Array.Copy(MetaTable, 0, Archive, Header.Length + StringTable.Length, MetaTable.Length);
            Array.Copy(NamesTable, 0, Archive, Header.Length + StringTable.Length + MetaTable.Length, NamesTable.Length);
            return Archive;
        }

        private static string ExtractFireEmblemMessageArchive(string outname, byte[] archive)
        {
            var ShiftJIS = Encoding.GetEncoding(932);
            string ArchiveName = ShiftJIS.GetString(archive.Skip(0x20).TakeWhile(b => b != 0).ToArray()); // Archive Name.
            uint TextPartitionLen = BitConverter.ToUInt32(archive, 4);
            uint StringCount = BitConverter.ToUInt32(archive, 0xC);
            string[] MessageNames = new string[StringCount];
            string[] Messages = new string[StringCount];

            uint StringMetaOffset = 0x20 + TextPartitionLen;
            uint NamesOffset = StringMetaOffset + 0x8 * StringCount;

            for (int i = 0; i < StringCount; i++)
            {
                int MessageOffset = 0x20 + BitConverter.ToInt32(archive, (int)StringMetaOffset + 0x8 * i);
                int MessageLen = 0;
                while (BitConverter.ToUInt16(archive, MessageOffset + MessageLen) != 0)
                    MessageLen += 2;
                Messages[i] = Encoding.Unicode.GetString(archive.Skip(MessageOffset).Take(MessageLen).ToArray()).Replace("\n", "\\n").Replace("\r", "\\r");
                int NameOffset = (int)NamesOffset + BitConverter.ToInt32(archive, (int)StringMetaOffset + (0x8 * i) + 4);
                MessageNames[i] = ShiftJIS.GetString(archive.Skip(NameOffset).TakeWhile(b => b != 0).ToArray());
            }

            List<string> Lines = new List<string>
            {
                ArchiveName,
                Environment.NewLine,
                "Message Name: Message",
                Environment.NewLine
            };
            for (int i = 0; i < StringCount; i++)
                Lines.Add(string.Format("{0}: {1}", MessageNames[i], Messages[i]));
            File.WriteAllLines(outname, Lines);

            return ArchiveName;
        }
    }
}
