using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MongoDataRecovery
{
    class Program
    {
        static bool outputNamespaceInfo = true, pauseAfterEachNs = false;
        static string
            dbName = @"meow",
            targetNs = @"cats",
            nsFileName = $@"{dbName}.ns",
            dataPath = @"F:\dataFiles",
            outputPath = @"F:\outputFiles",
            outputPathMethod1,
            outputPathMethod2;
        static Dictionary<int, System.IO.MemoryMappedFiles.MemoryMappedFile> mmapFiles = new Dictionary<int, System.IO.MemoryMappedFiles.MemoryMappedFile>();

        static void Main(string[] args)
        {
            Console.WriteLine("Data Path:");
            dataPath = Console.ReadLine();
            Console.WriteLine("DB Name:");
            dbName = Console.ReadLine();
            Console.WriteLine("Target Collection (if you can't find it, look at the namespace info:");
            targetNs = Console.ReadLine();

            Console.WriteLine("Output Path:");
            outputPath = Console.ReadLine();
            outputPathMethod1 = Path.Combine(outputPath, @"method1\");
            outputPathMethod2 = Path.Combine(outputPath, @"method2\");
            if (!Directory.Exists(outputPathMethod1)) Directory.CreateDirectory(outputPathMethod1);
            if (!Directory.Exists(outputPathMethod2)) Directory.CreateDirectory(outputPathMethod2);
            Method1();
            Method2();
            Console.WriteLine("Method 1 and Method 2 complete.");
            Console.ReadLine();
        }

        public static void Method2()
        {
            //string dataFilename = "";
            var files = Directory.GetFiles(dataPath, dbName + "." + "*");

            foreach(var dataFilename in files)
            {
                if (dataFilename == $@"{dbName}.ns")
                    continue;
                var rng = new Random();

                Console.WriteLine($@"Working on file {dataFilename}");

                var dataFilePath = System.IO.Path.Combine(dataPath, dataFilename);
                if (!File.Exists(dataFilePath))
                {
                    Console.WriteLine($"{dataFilePath} does not exist, sorry, expect lots of missing documents :(");
                    continue;
                }

                using (var nsFile = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(dataFilePath, System.IO.FileMode.Open))
                using (var nsStream = nsFile.CreateViewStream(0, 0, System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read))
                {
                    var nsBuffer = new byte[1024 * 1024 * 1024];
                    var readCount = nsStream.Read(nsBuffer, 0, nsBuffer.Length);
                    if (readCount == 0)
                    {
                        Console.WriteLine("I think we reached EOF!");
                    }
                    var targetNsBuffer = new byte[] { 0xEE, 0xEE, 0xEE, 0xEE, 0x07, 0x5F, 0x69, 0x64, 0x00 };   // Looking for {0xEEEEEEEE, 0x07, "_id", \0}
                    int offset = 0;

                    for (var i = 0; i < nsBuffer.Length; i++)
                    {
                        if (nsBuffer[i] == targetNsBuffer[0])
                        {
                            offset = i;
                            bool matched = true;
                            for (var j = 0; j < targetNsBuffer.Length; j++)
                            {
                                if (nsBuffer[i + j] != targetNsBuffer[j])
                                {
                                    matched &= false;
                                    break;
                                }
                            }

                            if (matched)
                            {
                                Console.WriteLine($"omg: {offset}");
                                offset -= 0x10;
                                var dataSize =   (BitConverter.ToInt32(nsBuffer, offset) - 0x10);
                                offset += 0x04;
                                var extentOffset= (BitConverter.ToInt32(nsBuffer, offset));
                                offset += 0x08;
                                offset += 0x04;
                                byte[] BSONData = new byte[dataSize];
                                Array.ConstrainedCopy(nsBuffer, offset, BSONData, 0, dataSize);
                                string ns = "unknown";
                                if (System.Text.ASCIIEncoding.ASCII.GetString(nsBuffer, extentOffset, 4) == "DCBA")
                                {
                                    ns = System.Text.ASCIIEncoding.ASCII.GetString(nsBuffer, extentOffset + 0x18 + 0x04, 128).Trim(new char[] { '\0' });
                                    Console.WriteLine($"NS:{ns}");
                                }
                                var b = BitConverter.GetBytes(dataSize);
                                b.CopyTo(BSONData, 0);
                                try
                                {
                                    File.WriteAllBytes(Path.Combine(outputPathMethod2, $@"{ns}-{OutputFile}.bson"), BSONData);
                                }
                                catch
                                {
                                    var nsFileTemp = rng.Next() * rng.Next();
                                    File.WriteAllText(Path.Combine(outputPathMethod2, $@"{nsFileTemp}-{OutputFile}.bson.err"), Path.Combine(outputPathMethod2, $@"{ns}-{OutputFile}.bson"));
                                    File.WriteAllBytes(Path.Combine(outputPathMethod2, $@"{nsFileTemp}-{OutputFile}.bson"), BSONData);
                                }
                                
                                OutputFile++;
                            }
                        }
                    }
                }
            }
        }

        public static void Method1()
        {
            List<NamespaceRecord> nsRecords = new List<NamespaceRecord>();
            using (var nsFile = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(System.IO.Path.Combine(dataPath, nsFileName), System.IO.FileMode.Open))
            using (var nsStream = nsFile.CreateViewStream(0, 0, System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read))
            {
                var nsBuffer = new byte[1024 * 1024 * 1024];
                var readCount = nsStream.Read(nsBuffer, 0, nsBuffer.Length);
                if (readCount == 0)
                {
                    Console.WriteLine("I think we reached EOF!");
                }
                var targetNsBuffer = System.Text.ASCIIEncoding.ASCII.GetBytes(targetNs);
                //targetNsBuffer = new byte[5] { 0, 0, 0 ,0 ,0};
                int offset = 0;

                for (var i = 0; i < nsBuffer.Length; i++)
                {
                    if (nsBuffer[i] == targetNsBuffer[0])
                    {
                        offset = i;
                        bool matched = true;
                        for (var j = 0; j < targetNsBuffer.Length; j++)
                        {
                            if (nsBuffer[i + j] != targetNsBuffer[j])
                            {
                                matched &= false;
                                break;
                            }
                        }

                        if (matched)
                        {
                            using (var nsView = nsFile.CreateViewStream(offset - 4, 0x158, System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read))
                            {
                                byte[] bitbuf = new byte[8];
                                int pos = 0;
                                var ns = new NamespaceRecord();
                                nsView.Read(ns.NsHash, 0, 4); pos += 4;
                                nsView.Read(ns.NsNameArray, 0, 128); pos += 128;
                                ns.NsName = System.Text.ASCIIEncoding.ASCII.GetString(ns.NsNameArray, 0, 128).Trim(new char[] { '\0' });

                                nsView.Read(bitbuf, 0, 8);
                                ns.ExtentFirstFile = BitConverter.ToUInt32(bitbuf, 0);
                                ns.ExtentFirst = BitConverter.ToUInt32(bitbuf, 4);

                                nsView.Read(bitbuf, 0, 8);
                                ns.ExtentLastFile = BitConverter.ToUInt32(bitbuf, 0);
                                ns.ExtentLast = BitConverter.ToUInt32(bitbuf, 4);

                                // deleted records
                                for (int dri = 0; dri < 19; dri++)
                                {
                                    nsView.Read(bitbuf, 0, 8);
                                    var del = new DeletedRecordLocation()
                                    {
                                        FileNumber = BitConverter.ToUInt32(bitbuf, 0),
                                        Offset = BitConverter.ToUInt32(bitbuf, 4)
                                    };

                                    if (del.FileNumber != 0xffffffff)
                                        ns.DeletedRecordLocations.Add(del);
                                }

                                nsView.Read(bitbuf, 0, 8);
                                ns.DataSize = BitConverter.ToUInt64(bitbuf, 0);

                                nsView.Read(bitbuf, 0, 8);
                                ns.RecordCount = BitConverter.ToUInt64(bitbuf, 0);

                                nsView.Read(bitbuf, 0, 4);
                                ns.SizeOfLastExtent = BitConverter.ToUInt32(bitbuf, 0);

                                nsView.Read(bitbuf, 0, 4);
                                ns.IndexCount = BitConverter.ToUInt32(bitbuf, 0);

                                nsView.Read(ns.IndexData, 0, 304);

                                offset -= 4;
                                ns.Offset = (uint)offset;
                                nsRecords.Add(ns);

                                Console.WriteLine($"found namespace at offset: {offset}");
                                Console.WriteLine($"Name: {ns.NsName.Replace(targetNs + ".", "")}");
                                if (outputNamespaceInfo || ns.NsName.EndsWith(".chunks") || ns.NsName.EndsWith(".files"))
                                {
                                    Console.WriteLine($"ExtentFirstFile: {ns.ExtentFirstFile}");
                                    Console.WriteLine($"ExtentFirstOffset: {ns.ExtentFirst}");
                                    Console.WriteLine($"ExtentLastFile: {ns.ExtentLastFile}");
                                    Console.WriteLine($"ExtentLastOffset: {ns.ExtentLast}");
                                    Console.WriteLine($"DataSize: {ns.DataSize}");
                                    Console.WriteLine($"RecordCt: {ns.RecordCount}");
                                    Console.WriteLine($"Last Ext Size: {ns.SizeOfLastExtent}");
                                    Console.WriteLine($"Index Ct: {ns.IndexCount}");
                                    Console.WriteLine("Deleted Records:");
                                    foreach (var d in ns.DeletedRecordLocations)
                                    {
                                        Console.WriteLine($"File: {d.FileNumber}, Offset = {d.Offset}");
                                    }

                                    if (pauseAfterEachNs)
                                        Console.ReadLine();
                                    Console.WriteLine();
                                }
                            }
                        }
                    }
                    /*
                    if(match)
                    {

                        match = false;
                    }*/
                }
            }
            Console.WriteLine("Namespace search complete.");

            foreach (var ns in nsRecords.Where(n => n.NsName.EndsWith(".chunks.$_id_")))
            {
                DeletedRecordParser(ns);
            }

            Console.WriteLine("Done with method 1 ;-;");
            //Console.ReadLine();
        }

        public static void DeletedRecordParser(NamespaceRecord ns)
        {
            foreach(var deletedRecord in ns.DeletedRecordLocations)
            {
                Console.WriteLine($"Opening first deleted record chain: {ns.NsName}:{deletedRecord.FileNumber}:{deletedRecord.Offset}");
                DeletedRecordParserTail(ns, deletedRecord.FileNumber, deletedRecord.Offset);
            }
        }
        public static int OutputFile = 0;
        public static void DeletedRecordParserTail(NamespaceRecord ns, UInt32 dataFileNumber, UInt32 offset)
        {
            do
            {

                var dfilePath = Path.Combine(dataPath, $"{dbName}.{dataFileNumber}");
                System.IO.MemoryMappedFiles.MemoryMappedFile dFile;
                if (!mmapFiles.ContainsKey((int)dataFileNumber))
                    mmapFiles.Add((int)dataFileNumber, System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(dfilePath, System.IO.FileMode.Open));

                dFile = mmapFiles[(int)dataFileNumber];
                UInt32 nextFileNumber = 0xffffffff;
                UInt32 nextOffset;

                using (var dStream = dFile.CreateViewStream(offset, 0, System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read))
                {
                    byte[] bitbuf = new byte[8];
                    dStream.Read(bitbuf, 0, 4);
                    var dataSize = BitConverter.ToUInt32(bitbuf, 0) - 0x10;
                    dStream.Read(bitbuf, 0, 4); // discard

                    dStream.Read(bitbuf, 0, 4); // file number
                    nextFileNumber = BitConverter.ToUInt32(bitbuf, 0);
                    dStream.Read(bitbuf, 0, 4); // offset
                    nextOffset = BitConverter.ToUInt32(bitbuf, 0);

                    byte[] BSONData = new byte[dataSize];
                    dStream.Read(BSONData, 0, (int)dataSize);

                    if (BSONData.Length != 64)
                    {
                        //if(dataFileNumber == 7)
                            Console.WriteLine($"BSON document @ f:{dataFileNumber}:{offset}+{dataSize}");
                        try
                        {
                            var b = BitConverter.GetBytes(dataSize);
                            b.CopyTo(BSONData, 0);
                            File.WriteAllBytes(Path.Combine(outputPathMethod1, $@"{OutputFile}.bson"), BSONData);
                            OutputFile++;
                            //ParseBSON(BSONData);
                        }
                        catch (Exception e)
                        {
                                Console.WriteLine($"Could not save BSON document: f:{dataFileNumber}:{offset}+{dataSize} - {e.Message}");
                        }
                    }


                    //var x = new MongoDB.Bson.BsonBinaryData(BSONData);
                    //MongoDB.Bson.BsonBinaryReader
                }

                dataFileNumber = nextFileNumber;
                offset = nextOffset;
            }
            while (dataFileNumber != 0xffffffff);
            /*if ()
                DeletedRecordParserTail(ns, nextFileNumber, nextOffset);*/
        }
        
    }
}
