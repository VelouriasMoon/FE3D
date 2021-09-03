using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FE3D.GovanifY;
using FE3D.IO;

namespace FE3D
{
    public static class FEBin
    {
        //location of the ptr1 should never the same but they are able to point the same place
        //on the other side the destination of the ptr2 is unique because of their lack  
        //of data in the main section they can occupy the same location as other pointers
        private static Dictionary<uint, uint> PTR1; //location key, destination value
        private static Dictionary<uint, uint> PTR1_des; //destination key, id value
        private static Dictionary<uint, uint> PTR2; //destination key, location value
        private static Dictionary<string, uint> PTR2_Data;
        private static Dictionary<uint, string> PTR1_Data;
        private static Dictionary<string, uint> StringSection;

        /// <summary>Decompiles a 3ds FE bin file into a readable txt file</summary>
        /// <param name="inpath"> Input bin file to decompile</param> 
        public static void ExtractBin(string inpath)
        {
            PTR1 = new Dictionary<uint, uint>();
            PTR1_des = new Dictionary<uint, uint>();
            PTR2 = new Dictionary<uint, uint>();

            //open file stream and then open a binary stream
            FileStream fileStream = new FileStream(inpath, FileMode.Open);
            BinaryStream bin = new BinaryStream(fileStream, true, false);
            var ShiftJIS = Encoding.GetEncoding(932);

            //read header info
            uint filesize = bin.ReadUInt32();
            uint datasize = bin.ReadUInt32();
            uint ptr1count = bin.ReadUInt32();
            uint ptr2count = bin.ReadUInt32();
            bin.Seek(0x10, SeekOrigin.Current);
            byte[] datasection = bin.ReadBytes((int)datasize);

            //read pointer region 1 info
            for (uint i = 0; i < ptr1count; i++)
            {
                uint loc = bin.ReadUInt32();
                uint des = LittleEndian.ReadUInt32FromArray(datasection, loc);

                try
                {
                    PTR1.Add(loc, des);
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("Key Already Exist in PTR1");
                }
                try
                {
                    PTR1_des.Add(des, i);
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("Key Already Exist in PTR1_des");
                }
            }

            //read pointer region 2 locations and destinations then store them for later
            for (int i = 0; i < ptr2count; i++)
            {
                uint loc = bin.ReadUInt32();
                uint des = bin.ReadUInt32();

                try
                {
                    PTR2.Add(des, loc);
                }
                catch
                {
                    Console.WriteLine("Key Already Exist in PTR1");
                }
            }

            //log the location after the pointer regions and get the raw bytes for the string array
            uint stringtablepos = (uint)bin.Tell();
            byte[] rawstrings = bin.ReadBytes(Convert.ToInt32(filesize - stringtablepos));


            //return to the start of the data section to begin processing it
            bin.Seek(0x20, SeekOrigin.Begin);

            //Create a txt file to store the bin data
            string outfile = Path.GetDirectoryName(inpath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(inpath) + ".txt";
            if (File.Exists(outfile))
                File.Delete(outfile);

            //open a stream to write the output file
            using (StreamWriter outstream = new StreamWriter(File.Create(outfile)))
            {
                for (int i = 0; i < datasize / 4; i++)
                {
                    uint currentpos = Convert.ToUInt32(bin.Tell() - 0x20);

                    //Check for ptr1 destinations and write them before the ptr2
                    if (PTR1_des.ContainsKey(currentpos))
                    {
                        PTR1_des.TryGetValue(currentpos, out uint value);
                        outstream.WriteLine($"PTR1: {value}");
                    }

                    //Check if there is a ptr2 for the current position and write it before the data
                    foreach (KeyValuePair<uint, uint> ptr2 in PTR2)
                    {
                        if (currentpos == ptr2.Value)
                        {
                            string ptr2data = ShiftJIS.GetString(rawstrings.Skip(Convert.ToInt32(ptr2.Key)).TakeWhile(b => b != 0).ToArray()).Replace("－", "−");
                            outstream.WriteLine($"PTR2: {ptr2data}");
                        }
                    }

                    //Check if there is a ptr1 for the current position otherwise right the bytes
                    if (PTR1.ContainsKey(currentpos))
                    {
                        PTR1.TryGetValue(currentpos, out uint value);
                        if (value > datasize)
                        {
                            string ptr1data = ShiftJIS.GetString(rawstrings.Skip(Convert.ToInt32(value - (datasize + (ptr1count * 4) + (ptr2count * 8)))).TakeWhile(b => b != 0).ToArray());
                            outstream.WriteLine(ptr1data);
                        }
                        else
                        {
                            PTR1_des.TryGetValue(value, out uint value2);
                            outstream.WriteLine($"POINTER1: {value2}");
                        }
                        bin.Seek(4, SeekOrigin.Current);
                    }
                    else
                    {
                        byte[] data = bin.ReadBytes(4);
                        outstream.WriteLine($"0x{BitConverter.ToString(data).Replace("-", "")}");
                    }
                    Console.WriteLine($"Processed {currentpos}");
                }
            }
            bin.Close();
            fileStream.Close();
        }

        //bin files have 5 major sections, header, main data, pointer region 1 and 2, and the end region
        //the header is formatted with file size, data size, num of ptr1, num of prt2, and 16 bytes of padding
        //main sections just holds all the data that the game using exclusing strings
        //pointer region 1 is a list of where each pointer in the main section is, each item is 4 bytes
        //pointer region 2 is a list of embbed lables in side the main section which doesn't show as data, each item is 8 bytes, location of pointer and it's string location
        //the end secions hold a list of all the strings in null terminated shift-jis, first are the ptr2, then the prt1

        /// <summary>Creates a bin file used in 3ds FE games from a text file</summary>
        /// <param name="inpath"> Input txt to compile into a bin</param> 
        public static void PackBin(string inpath)
        {
            string[] txtfile = File.ReadAllLines(inpath);
            FileStream newfilestream = File.Create(inpath.Replace(".txt", ".bin"));
            PTR1 = new Dictionary<uint, uint>();
            PTR1_des = new Dictionary<uint, uint>();
            PTR2_Data = new Dictionary<string, uint>();
            PTR1_Data = new Dictionary<uint, string>();
            StringSection = new Dictionary<string, uint>();
            var ShiftJIS = Encoding.GetEncoding(932);

            using (BinaryStream outbin = new BinaryStream(newfilestream))
            {
                //Write innital pass included dummy sections, main data, and strings
                //write dummy header
                for (int i = 0; i < 0x20; i++)
                {
                    outbin.Write((byte)0);
                }

                //Phrase Text file
                foreach (string line in txtfile)
                {
                    long CurrentPos = outbin.Tell();
                    if (line.StartsWith("0x")) //Convert the raw data string into bytes
                    {
                        uint data = Convert.ToUInt32(LittleEndian.ReadUInt32FromArray(FEIO.HexStringToByteArray(line.Replace("0x", ""))));
                        outbin.Write(data);
                    }
                    else if (line.StartsWith("POINTER1")) //Write dummy bytes and log pointer number
                    {
                        PTR1.Add((uint)CurrentPos, Convert.ToUInt32(line.Replace("POINTER1: ", "")));
                        outbin.Write((uint)0);
                    }
                    else if (line.StartsWith("PTR1")) //Log location to for Pointer1 rewrite
                    {
                        PTR1_des.Add(Convert.ToUInt32(line.Replace("PTR1: ", "")), (uint)CurrentPos);
                    }
                    else if (line.StartsWith("PTR2")) //Log string and Position
                    {
                        PTR2_Data.Add(line.Replace("PTR2: ", ""), (uint)CurrentPos);
                    }
                    else //Log Location and string to write later
                    {
                        PTR1_Data.Add((uint)CurrentPos, line);
                        outbin.Write((uint)0);
                    }
                }
                int EndofDataSecton = (int)outbin.Tell();

                uint ptr2Count;
                uint ptr1Count = (uint)PTR1.Count + (uint)PTR1_Data.Count;
                if (PTR2_Data == null)
                    ptr2Count = 0;
                else
                    ptr2Count = (uint)PTR2_Data.Count;

                //write dummy ptr sections
                for (int i = 0; i < ptr1Count; i++)
                {
                    outbin.Write((uint)0);
                }
                for (int i = 0; i < ptr2Count; i++)
                {
                    outbin.Write((long)0);
                }
                int BeginningOfTheEnd = (int)outbin.Tell();

                //sort strings and remove dupicates
                List<string> strings = new List<string>();
                foreach (var data in PTR2_Data)
                {
                    if (!strings.Contains(data.Key))
                        strings.Add(data.Key);
                }
                foreach (var data in PTR1_Data)
                {
                    if (!strings.Contains(data.Value))
                        strings.Add(data.Value);
                }

                //Write String Section
                foreach (string line in strings)
                {
                    long CurrentPos = outbin.Tell();
                    StringSection.Add(line, (uint)CurrentPos);
                    byte[] linebytes = ShiftJIS.GetBytes(line.Replace("\\", "/"));
                    outbin.Write(linebytes);
                    outbin.Write((byte)0);
                }

                //Write Second pass with count data and pointers
                int EOF = (int)outbin.Tell();
                outbin.Seek(0, SeekOrigin.Begin);

                //Write Header info
                outbin.Write(EOF);
                outbin.Write(EndofDataSecton - 0x20);
                outbin.Write(ptr1Count);
                outbin.Write(ptr2Count);

                List<uint> PTR1Sec = new List<uint>();
                //Write PTR1 data pointers
                foreach (var data in PTR1)
                {
                    outbin.Seek(data.Key, SeekOrigin.Begin);
                    outbin.Write(PTR1_des[data.Value] - 0x20);
                    PTR1Sec.Add(data.Key);
                }
                //Write PTR1 string pointers
                foreach (var data in PTR1_Data)
                {
                    outbin.Seek(data.Key, SeekOrigin.Begin);
                    outbin.Write(StringSection[data.Value] - 0x20);
                    PTR1Sec.Add(data.Key);
                }
                //Write the pointer list to the PTR1 section
                outbin.Seek(EndofDataSecton, SeekOrigin.Begin);
                foreach (uint pointer in PTR1Sec)
                {
                    outbin.Write(pointer - 0x20);
                }
                //Write Data for PTR2 section
                foreach (var data in PTR2_Data)
                {
                    outbin.Write(data.Value - 0x20);
                    outbin.Write((uint)(StringSection[data.Key] - BeginningOfTheEnd));
                }

            }
            newfilestream.Close();
        }
    }
}
