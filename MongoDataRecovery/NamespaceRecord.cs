using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDataRecovery
{
    class NamespaceRecord
    {
        public UInt32 Offset;
        public byte[] NsHash = new byte[4];
        public byte[] NsNameArray = new byte[128];
        public string NsName;
        public UInt32 ExtentFirstFile;
        public UInt32 ExtentFirst;
        public UInt32 ExtentLastFile;
        public UInt32 ExtentLast;
        public List<DeletedRecordLocation> DeletedRecordLocations = new List<DeletedRecordLocation>();
        public UInt64 DataSize;
        public UInt64 RecordCount;
        public UInt32 SizeOfLastExtent;
        public UInt32 IndexCount;
        public byte[] IndexData = new byte[304];
    }

    class DeletedRecordLocation
    {
        public UInt32 FileNumber;
        public UInt32 Offset;
    }
}
