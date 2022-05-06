using System;
using System.IO;

namespace LsTool
{
    public enum Version
    {
        GZ = 0,
        TPP = 1,
    }
    public class Program
    {
        public static void Main(string[] args)
        {

            //Check if argument is folder and add folder contents to arguments
            foreach (string arg in args)
            {
                if (Directory.Exists(arg))
                {
                    foreach (string file in Directory.GetFiles(arg))
                    {
                        Array.Resize(ref args, args.Length + 1);
                        args[args.Length - 1] = file;
                    };
                    continue;
                };
            };

            foreach (string arg in args)
            {
                //Argument just isn't a file
                if (!File.Exists(arg))
                {
                    Console.WriteLine($"{arg} Not an existing file!!!");
                    continue;
                };

                //Check extension for what to do
                string extension = Path.GetExtension(arg).Substring(1);

                if (extension==string.Empty)
                {
                    Console.WriteLine($"{arg} has no extension!!!");
                    continue;
                };

                switch(extension)
                {
                    case "lsst":
                        UnpackLsst(arg);
                        break;
                    case "st":
                        ReadStBinary(arg);
                        break;
                    case "ls":
                        ReadLsBinary(arg);
                        break;
                    case "ls2":
                        ReadLs2Binary(arg);
                        break;
                    default:
                        Console.WriteLine($"{arg} Unsupported extension!!!");
                        break;
                };
            };

            Console.WriteLine("Done!!!");
            Console.Read(); //DEBUG Hold onscreen
        }
        public static void ReadStBinary(string path)
        {
            //Filename for logs
            string fileName = Path.GetFileName(path);
            //Actual binary reading time
            using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                Console.WriteLine($"Reading {fileName}...");
                if (reader.BaseStream.Length <= 0)
                {
                    Console.WriteLine($"{fileName} is empty!!!");
                    return;
                };

                int entryCount = reader.ReadInt32(); //1
                short unknown0 = reader.ReadInt16(); //6
                short unknown1 = reader.ReadInt16(); //100
                int unknown2 = reader.ReadInt32(); //0
                short unknown3 = reader.ReadInt16(); //8
                int unknown4 = reader.ReadInt32(); //1
                string subtitleId = reader.ReadCString();
                Console.WriteLine($"    {fileName}'s subtitleId is {subtitleId}!!!");
                reader.AlignStream(4);
                //idk the inverse hash is here again
                //just in case: uint64, write in inverse endian, shift by two bytes (8 bits) to fill it in
                //then it's just padding whatever
                //padding that could be taken care of with a lsst-sab repacker

                //write the string to a txt
                string outputPath = Path.GetDirectoryName(path) + "\\" + fileName + ".txt";
                using (BinaryWriter writer = new BinaryWriter(new FileStream(outputPath, FileMode.Create)))
                {
                    writer.Write(subtitleId);
                };
            }
        }
        public static void ReadLsBinary(string path)
        {
            //Filename for logs
            string fileName = Path.GetFileName(path);
            //Actual binary reading time
            using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                Console.WriteLine($"Reading {fileName}...");
                LsTrack ls = new LsTrack();
                ls.ReadBinary(reader, Version.GZ);
                string newPath = Path.GetDirectoryName(path);
                newPath = newPath + "/" + Path.GetFileNameWithoutExtension(path) + ".ls2";
                using (BinaryWriter writer = new BinaryWriter(new FileStream(newPath, FileMode.Create)))
                {
                    ls.WriteBinary(writer, Version.TPP, fileName);
                };
            };
        }
        public static void ReadLs2Binary(string path)
        {
            //Filename for logs
            string fileName = Path.GetFileName(path);
            //Actual binary reading time
            using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                Console.WriteLine($"Reading {fileName}...");
                LsTrack ls2 = new LsTrack();
                ls2.ReadBinary(reader, Version.TPP);
            };
        }
        public static void UnpackLsst(string path)
        {
            //Filename for logs
            string fileName = Path.GetFileNameWithoutExtension(path);
            //Actual binary reading time
            using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                Console.WriteLine($"Reading {fileName}...");
                if (reader.BaseStream.Length <= 0)
                {
                    Console.WriteLine($"{fileName} is empty!!!");
                    return;
                };

                int entryCount = reader.ReadInt32();
                if (entryCount > 2 || entryCount < 0)
                {
                    Console.WriteLine($"{fileName} has invalid entry count!!!");
                    return;
                };

                //get metadata
                string[] extensions = new string[entryCount];
                int[] offsetsToStart = new int[entryCount];
                for (int i = 0; i < entryCount; i++)
                {
                    extensions[i] = reader.ReadCString();
                    reader.AlignStream(4);
                    offsetsToStart[i] = reader.ReadInt32();
                    Console.WriteLine($"{fileName}.{extensions[i]} starts at {offsetsToStart[i]}");
                };

                //get file sizes from metadata
                int[] sizes = new int[entryCount];
                for (int i = 0; i < entryCount; i++)
                {
                    if (i + 1 < entryCount) //if not the last
                    {
                        sizes[i] = offsetsToStart[i + 1] - offsetsToStart[i];
                        Console.WriteLine($"{fileName}.{extensions[i]} is {sizes[i]} long,");
                    }
                    else //if the last
                    {
                        sizes[i] = (int)reader.BaseStream.Length - offsetsToStart[i];
                        Console.WriteLine($"{fileName}.{extensions[i]} is {sizes[i]} long!");
                    };
                };

                //get file data
                byte[][] data = new byte[entryCount][];
                for (int i = 0; i < entryCount; i++)
                {
                    reader.BaseStream.Position = offsetsToStart[i];
                    data[i] = reader.ReadBytes(sizes[i]);
                    Console.WriteLine($"{fileName}.{extensions[i]} is read!");
                };

                //write files from accumulated data
                for (int i = 0; i < entryCount; i++)
                {
                    string outputPath = Path.GetDirectoryName(path) + "\\" + fileName + "." + extensions[i];
                    using (BinaryWriter writer = new BinaryWriter(new FileStream(outputPath, FileMode.Create)))
                    {
                        Console.WriteLine($"Writing {fileName}.{extensions[i]}...");
                        writer.Write(data[i]);
                    };
                    Console.WriteLine($"{fileName}.{extensions[i]} is done!!!");
                };
            };
        }
        public static LsTrack ReadBinary(string path)
        {
            LsTrack track = new LsTrack();
            return track;
        }
        public static void WriteBinary(LsTrack track)
        {

        }
    }
}
