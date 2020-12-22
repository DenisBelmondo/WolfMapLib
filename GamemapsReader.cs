using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Wolf3D
{
    public class GamemapsReader : IMapReader<GamemapsReader.GameMap>
    {
        public class GamemapsReaderException : Exception
        {
            public GamemapsReaderException()
            {
            }

            public GamemapsReaderException(string message) : base(message)
            {
            }

            public GamemapsReaderException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected GamemapsReaderException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }

        public class MapHeadException : GamemapsReaderException
        {
            public MapHeadException()
            {
            }

            public MapHeadException(string message) : base(message)
            {
            }

            public MapHeadException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected MapHeadException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }

        public class GameMapsException : GamemapsReaderException
        {
            public GameMapsException()
            {
            }

            public GameMapsException(string message) : base(message)
            {
            }

            public GameMapsException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected GameMapsException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }

        public struct GameMap
        {
            public string Name;
            public int Width;
            public int Height;
            public ushort[][] Planes;

            public ushort GetTile(int x, int y, int planeNum)
            {
                return Planes[planeNum][y * Height + (x + 1)]; // x + 1 is a hack idk
            }
        }

        private const int NEARTAG = 0xA7;
        private const int FARTAG = 0xA8;
        private const int RLEWTAG = 0xABCD;
        private const int NUM_PLANES = 3;
        private const int NUM_MAPS = 100;

        protected readonly FileInfo MapHead;
        protected readonly FileInfo GameMaps;

        /// <summary>
        /// Decompresses Carmack-Compressed map data.
        /// </summary>
        ///
        /// <returns>
        /// Uncompressed RLEW map data as a unsigned short array.
        /// </returns>

        public static ushort[] CarmackExpand(byte[] source, int length)
        {
            var dest = new ushort[length];

            var inptr = 0;
            var outptr = 0;

            length /= 2;

            ushort READWORD()
            {
                return (ushort)(source[inptr++] | (source[inptr++] << 8));
            }

            while (length > 0)
            {
                var ch = READWORD();
                var chhigh = ch >> 8;

                if (chhigh == NEARTAG)
                {
                    var count = ch & 0xFF;

                    if (count == 0)
                    {
                        ch |= source[inptr++];
                        dest[outptr++] = ch;
                        length--;
                    }
                    else
                    {
                        var offset = source[inptr++];
                        var copyptr = outptr - offset;

                        length -= count;
                        if (length < 0)
                            return dest;

                        while (count-- > 0)
                            dest[outptr++] = dest[copyptr++];
                    }
                }
                else if (chhigh == FARTAG)
                {
                    var count = ch & 0xFF;

                    if (count == 0)
                    {
                        ch |= source[inptr++];
                        dest[outptr++] = ch;
                        length--;
                    }
                    else
                    {
                        var offset = READWORD();
                        var copyptr = offset;

                        length -= count;
                        if (length < 0)
                            return dest;

                        while (count-- > 0)
                            dest[outptr++] = dest[copyptr++];
                    }
                }
                else
                {
                    dest[outptr++] = ch;
                    length--;
                }
            }

            return dest;
        }

        /// <summary>
        /// Decodes RLEW-encoded map data.
        /// </summary>
        ///
        /// <returns>
        /// Uncompressed map data as a unsigned short array.
        /// </returns>

        public static ushort[] RlewExpand(ushort[] source, int length)
        {
            var dest = new ushort[length];

            var inptr = 0;
            var outptr = 0;

            var end = outptr + (length >> 1);

            do
            {
                ushort value = source[inptr++];
                if (value != RLEWTAG)
                {
                    dest[outptr++] = value;
                }
                else
                {
                    var count = source[inptr++];
                    value = source[inptr++];

                    for (var i = 0; i < count; i++)
                        dest[outptr++] = value;
                }
            } while (outptr < end);

            return dest;
        }

        public GamemapsReader(FileInfo mapHead, FileInfo gameMaps)
        {
            MapHead = mapHead;
            GameMaps = gameMaps;
        }

        public GameMap GetMap(int mapNum)
        {
            if (mapNum < 0 || mapNum >= NUM_MAPS)
            {
                throw new GameMapsException($"Map number {mapNum} is out of range or invalid.");
            }

            var mapOffs = -1;

            using (var mhReader = new BinaryReader(MapHead.OpenRead()))
            using (var gmReader = new BinaryReader(GameMaps.OpenRead()))
            {
                var magic = mhReader.ReadUInt16();

                if (magic != RLEWTAG)
                {
                    throw new MapHeadException($"{MapHead.FullName} is not a valid MAPHEAD file.");
                }

                mhReader.BaseStream.Seek(mapNum * 4, SeekOrigin.Current);

                mapOffs = mhReader.ReadInt32();

                if (mapOffs <= 0)
                {
                    throw new GameMapsException($"Map number {mapNum} does not exist.");
                }

                gmReader.BaseStream.Seek(mapOffs, SeekOrigin.Begin);

                var planeOff = new uint[NUM_PLANES];
                var planeLen = new ushort[NUM_PLANES];

                for (var i = 0; i < NUM_PLANES; i++)
                {
                    planeOff[i] = gmReader.ReadUInt32();
                }

                for (var i = 0; i < NUM_PLANES; i++)
                {
                    planeLen[i] = gmReader.ReadUInt16();
                }

                var width = gmReader.ReadUInt16();
                var height = gmReader.ReadUInt16();
                var name = Encoding.ASCII.GetString(gmReader.ReadBytes(16));

                var gmPlanes = new ushort[NUM_PLANES][];

                for (var i = 0; i < NUM_PLANES; i++)
                {
                    if (planeOff[i] == 0)
                    {
                        continue;
                    }

                    gmReader.BaseStream.Seek(planeOff[i], SeekOrigin.Begin);

                    // size of data after carmack expansion
                    var decarmSize = gmReader.ReadUInt16();
                    // carmacized data
                    var carmedSource = gmReader.ReadBytes(planeLen[i]);

                    gmPlanes[i] = RlewExpand(CarmackExpand(carmedSource, decarmSize), width * height * 2);
                }

                return new GameMap { Name = name, Width = width, Height = height, Planes = gmPlanes };
            }
        }
    }
}
