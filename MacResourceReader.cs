using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Belmondo.WolfMapLib
{
    public class MacResourceReader : IMapReader<Array>
    {
        protected enum BrgrConstant
        {
            IDLOGO = 128,
            MACPLAY = 129,
            MPLAYPAL = 130,
            IDPAL = 131,
            BLACKPAL = 132,
            TITLEPIC = 133,
            TITLEPAL = 134,
            SOUNDLST = 135,
            DARKMAP = 136,
            WALLLIST = 137,
            BJFACE = 138,
            INTERPIC = 139,
            INTERPAL = 140,
            INTERBJ = 141,
            FACE320 = 142,
            FACE512 = 143,
            FACE640 = 144,
            PLAYPAL = 145,
            MAPLIST = 146,
            SONGLIST = 147,
            GETPSYCH = 148,
            YUMMYPIC = 149,
            YUMMYPAL = 150,
            FINETAN = 151,
            FINESIN = 152,
            SCALEATZ = 153,
            VIEWANGX = 154,
            XVIEWANG = 155
        }

        /*
        public struct MacBinHeader
        {
            public byte Version;
            public byte FilenameLength;
            public string Filename;
            public string FileType;
            public string FileCreator;
            public byte FinderFlags;
            public byte Pad0;
            public ushort VerticalPosition;
            public ushort HorizontalPosition;
            public ushort FolderId;
            public byte ProtectedFlag;
            public byte Pad1;
            public uint DataForkLength;
            public uint ResourceForkLength;
            public uint CreationDate;
            public uint ModifiedDate;
            public byte[] Pad2;
            public ushort Reserved;
        }
        */

        public struct ResHeader
        {
            public uint ResourceOffset;
            public uint MapOffset;
            public uint ResourceLength;
            public uint MapLength;
        }

        public struct MapHeader
        {
            public uint ReservedHandle;
            public ushort ReferenceNumber;
            public ushort ForkAttributes;
            public ushort TypeListOffset;
            public ushort NameListOffset;
            public ushort NumTypesMinusOne;
        }

        public struct ResType
        {
            public string Type;
            public ushort NumResourcesMinusOne;
            public ushort Offset;
        }

        public struct ResReference
        {
            public struct RefData
            {
                public ushort ResId;
                public ushort NameOffset;
                public byte Attributes;
                public byte HiDataOffset; // Yay a 24-bit type!
                public ushort LoDataOffset;
                public uint ReservedHandle;
            }

            public RefData Ref;
            public uint DataOffset;
            public string Name;
        }

        protected readonly FileInfo MacBin;

        public MacResourceReader(FileInfo macBin)
        {
            MacBin = macBin;

            using (var reader = new BinaryReader(MacBin.OpenRead()))
            {
                var resHeader = new ResHeader();

                resHeader.ResourceOffset = reader.ReadUInt32BE();
                resHeader.MapOffset = reader.ReadUInt32BE();
                resHeader.ResourceLength = reader.ReadUInt32BE();
                resHeader.MapLength = reader.ReadUInt32BE();

                reader.BaseStream.Seek(resHeader.MapOffset, SeekOrigin.Current);

                var mapHeader = new MapHeader();

                mapHeader.ReservedHandle = reader.ReadUInt32BE();
                mapHeader.ReferenceNumber = reader.ReadUInt16BE();
                mapHeader.ForkAttributes = reader.ReadUInt16BE();
                mapHeader.TypeListOffset = reader.ReadUInt16BE();
                mapHeader.NameListOffset = reader.ReadUInt16BE();
                mapHeader.NumTypesMinusOne = reader.ReadUInt16BE();

                reader.BaseStream.Seek(resHeader.MapOffset + mapHeader.TypeListOffset, SeekOrigin.Begin);

                var numTypes = reader.ReadUInt16BE() + 1;
                var resTypes = new ResType[numTypes];
                var brgrIdx = -1;

                for (var i = 0; i < numTypes; i++)
                {
                    var resType = new ResType();
                    resType.Type = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    resType.NumResourcesMinusOne = reader.ReadUInt16BE();
                    resType.Offset = reader.ReadUInt16BE();

                    if (resType.Type == "BRGR")
                    {
                        brgrIdx = i;
                    }

                    resTypes[i] = resType;
                }

                if (brgrIdx < 0)
                {
                    throw new Exception("No map resource found.");
                }

                reader.BaseStream.Seek(resHeader.MapOffset + mapHeader.TypeListOffset + resTypes[brgrIdx].Offset, SeekOrigin.Begin);

                var brgrRefs = new ResReference[resTypes[brgrIdx].NumResourcesMinusOne + 1];

                for (var i = 0; i < resTypes[brgrIdx].NumResourcesMinusOne + 1; i++)
                {
                    var resRef = new ResReference();
                    var refData = new ResReference.RefData();

                    refData.ResId = reader.ReadUInt16BE();
                    refData.NameOffset = reader.ReadUInt16BE();
                    refData.Attributes = reader.ReadByte();
                    refData.HiDataOffset = reader.ReadByte();
                    refData.LoDataOffset = reader.ReadUInt16BE();
                    refData.ReservedHandle = reader.ReadUInt32BE();

                    resRef.Ref = refData;
                    resRef.DataOffset = (uint)((refData.HiDataOffset << 16) | (refData.LoDataOffset));

                    brgrRefs[i] = resRef;
                }

                // get names

                for (var i = 0; i < brgrRefs.Length; i++)
                {
                    if (brgrRefs[i].Ref.NameOffset == 0xFFFF)
                    {
                        continue;
                    }

                    reader.BaseStream.Seek(resHeader.MapOffset + mapHeader.NameListOffset + brgrRefs[i].Ref.NameOffset, SeekOrigin.Begin);
                    var nameLen = reader.ReadByte();
                    brgrRefs[i].Name = Encoding.ASCII.GetString(reader.ReadBytes(nameLen));
                }

                // get data

                for (var i = 0; i < brgrRefs.Length; i++)
                {
                    reader.BaseStream.Seek(brgrRefs[i].DataOffset + resHeader.ResourceOffset, SeekOrigin.Begin);
                    var dataLen = reader.ReadUInt32BE();
                    var data = reader.ReadBytes((int)dataLen);
                }
            }
        }

        public Array GetMap(int mapNum)
        {
            throw new NotImplementedException();
        }
    }
}
