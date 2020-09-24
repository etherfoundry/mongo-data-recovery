using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDataRecovery
{
    class BSONTypeLookup
    {
        public Dictionary<int, BSONObject> types = new Dictionary<int, BSONObject>();
        public BSONTypeLookup()
        {
            foreach (Type t in System.Reflection.Assembly.GetAssembly(typeof(BSONObject)).GetTypes().Where(x=>
                x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof(BSONObject))))
            {
                
                Console.WriteLine(t.Name);
                
                var tt = Activator.CreateInstance(t) as BSONObject;
                types.Add(tt.TypeId(), tt);
            }
        }

        public Type GetType(int type)
        {
            var yy = this.types[type];
            return yy.GetType();
        }
    }
    abstract class BSONObject
    {
        public abstract int TypeId();
    }
    
    abstract class BSONObject<T> : BSONObject
    {
        public T storage;
    }

    class BSONDouble : BSONObject<double>
    {
        public override int TypeId() => 0x01;
    }

    class BSONString : BSONObject<string>
    {
        public override int TypeId() => 0x02;
    }

    class BSONArray : BSONObject<BSONObject[]>
    {
        public override int TypeId() => 0x04;
    }

    class BSONBinary : BSONObject<byte[]>
    {
        public override int TypeId() => 0x05;
        public int subtype;
    }

    class BSONUndefined : BSONObject
    {
        public override int TypeId() => 0x06;
    }

    class ObjectId
    {
        public byte[] hex;
        public ObjectId(byte[] oid)
        {
            hex = oid;
        }

        public string ToHexString()
        {
            return BitConverter.ToString(hex).Replace("-", "");
        }
            /*
            static UInt32 randCounter = 0;
            static Random rng = new Random();

            UInt32 myRandCounter = 0;
            UInt32 secondsSinceEpoch = 0;
            byte[] rand = new byte[5];

            public ObjectId()
            {
                if (randCounter == 0) randCounter = ((uint)(rng.Next()+ rng.Next()) % 0xFFFFFF );

                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                myRandCounter = (randCounter++) % 0xFFFFFF;
                secondsSinceEpoch = (uint)t.TotalSeconds % 0xFFFFFFFF;
                rng.NextBytes(rand);
            }


            public ObjectId(byte[] hex)
            {
                if(BitConverter.IsLittleEndian)
                {
                    Array.Reverse(hex);
                }
                myRandCounter = (uint)(hex[0] << 16 | hex[1] << 8 | hex[2]);
                Array.ConstrainedCopy(hex, 3, rand, 0, 5);
                secondsSinceEpoch = BitConverter.ToUInt32(hex, 8);

                //Array.ConstrainedCopy(BitConverter.GetBytes(BitConverter.ToUInt32(hex, 10)), 1, myRandCounter, 0, 3);

            }

            public string ToHexString()
            {
                var sb = new StringBuilder(12);
                var secArr = BitConverter.GetBytes(secondsSinceEpoch);
                Array.Reverse(secArr);
                sb.Append(BitConverter.ToString(secArr).Replace("-", ""));
                var rbuf = new byte[rand.Length];
                Array.Copy(rand, rbuf, rand.Length);
                Array.Reverse(rbuf);
                sb.Append(BitConverter.ToString(rbuf).Replace("-", ""));

                var randCtrArr = BitConverter.GetBytes(randCounter);
                Array.Reverse(randCtrArr);
                sb.Append(BitConverter.ToString(randCtrArr,1).Replace("-", ""));
                return sb.ToString();

            }

            public ObjectId Create()
            {
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                int secondsSinceEpoch = (int)t.TotalSeconds;
                return new ObjectId();
            }*/
        }

    class BSONObjectId : BSONObject<ObjectId>
    {
        public override int TypeId() => 0x07;
    }
    
    class BSONTrue : BSONObject<bool>
    {
        public override int TypeId() => 0x08;
        public BSONTrue()
        {
            storage = true;
        }
    }

    class BSONDateTimeUTC : BSONObject<UInt64>
    {
        public override int TypeId() => 0x09;
    }

    class BSONNULL : BSONObject
    {
        public override int TypeId() => 0x0A;
    }

    class BSONInt32 : BSONObject<Int32>
    {
        public override int TypeId() => 0x10;
    }

    class BSONTimestamp : BSONObject<UInt64>
    {
        public override int TypeId() => 0x11;
    }

    class BSONInt64 : BSONObject<Int64>
    {
        public override int TypeId() => 0x12;
    }
}
