using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDataRecovery
{
    class BSONDocument : BSONObject
    {
        public Dictionary<string, BSONObject> Fields = new Dictionary<string, BSONObject>();
        public override int TypeId() => 0x03;
    }
}
