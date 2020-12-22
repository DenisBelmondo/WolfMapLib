using System;
using System.IO;

namespace Wolf3D
{
    public static class Helpers
    {
        private static byte[] Reverse(this byte[] b)
        {
            Array.Reverse(b);
            return b;
        }

        public static ushort ReadUInt16BE(this BinaryReader reader)
        {
            return BitConverter.ToUInt16(reader.ReadBytes(2).Reverse(), 0);
        }

        public static short ReadInt16BE(this BinaryReader reader)
        {
            return BitConverter.ToInt16(reader.ReadBytes(2).Reverse(), 0);
        }

        public static uint ReadUInt32BE(this BinaryReader reader)
        {
            return BitConverter.ToUInt32(reader.ReadBytes(4).Reverse(), 0);
        }

        public static int ReadInt32BE(this BinaryReader reader)
        {
            return BitConverter.ToInt32(reader.ReadBytes(4).Reverse(), 0);
        }
    }
}
