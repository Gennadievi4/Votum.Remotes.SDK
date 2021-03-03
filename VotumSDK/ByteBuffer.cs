using System.Collections.Generic;
using System.Linq;

namespace Votum
{
    public class ByteBuffer
    {
        private readonly List<byte> _Bytes;

        public int Position { get; private set; }
        
        public ByteBuffer(int size)
        {
            _Bytes = new byte[size].ToList();
        }
        
        public void Put(byte theByte)
        {
            _Bytes[Position] = theByte;
            Position++;
        }

        public void Put(byte[] bytes)
        {
            foreach (var thebyte in bytes)
            {
                Put(thebyte);
            }
        }

        public byte[] ToArray()
        {
            return _Bytes.ToArray();
        }

        public void Put(byte[] bytes, int startIndex, int Length)
        {
            for (var i = startIndex; i < Length; i++)
            {
                Put(bytes[i]);
            }
        }
    }
}
