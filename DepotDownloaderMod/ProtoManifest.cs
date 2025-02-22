using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ProtoBuf;
using SteamKit2;
using static SteamKit2.Internal.CMsgClientUGSGetGlobalStatsResponse;

namespace DepotDownloader
{
    [ProtoContract]
    class ProtoManifest
    {
        // Proto ctor
        private ProtoManifest()
        {
            Files = new List<FileData>();
        }

        public ProtoManifest(DepotManifest sourceManifest, ulong id) : this()
        {
            sourceManifest.Files.ForEach(f => Files.Add(new FileData(f)));
            ID = id;
            CreationTime = sourceManifest.CreationTime;
        }

        [ProtoContract]
        public class FileData
        {
            // Proto ctor
            private FileData()
            {
                Chunks = new List<ChunkData>();
            }

            public FileData(DepotManifest.FileData sourceData) : this()
            {
                FileName = sourceData.FileName;
                sourceData.Chunks.ForEach(c => Chunks.Add(new ChunkData(c)));
                Flags = sourceData.Flags;
                TotalSize = sourceData.TotalSize;
                FileHash = sourceData.FileHash;
            }

            [ProtoMember(1)]
            public string FileName { get; internal set; }

            /// <summary>
            /// Gets the chunks that this file is composed of.
            /// </summary>
            [ProtoMember(2)]
            public List<ChunkData> Chunks { get; private set; }

            /// <summary>
            /// Gets the file flags
            /// </summary>
            [ProtoMember(3)]
            public EDepotFileFlag Flags { get; private set; }

            /// <summary>
            /// Gets the total size of this file.
            /// </summary>
            [ProtoMember(4)]
            public ulong TotalSize { get; private set; }

            /// <summary>
            /// Gets the hash of this file.
            /// </summary>
            [ProtoMember(5)]
            public byte[] FileHash { get; private set; }
        }

        [ProtoContract(SkipConstructor = true)]
        public class ChunkData
        {
            public ChunkData(DepotManifest.ChunkData sourceChunk)
            {
                ChunkID = sourceChunk.ChunkID;
                Checksum = sourceChunk.Checksum;
                Offset = sourceChunk.Offset;
                CompressedLength = sourceChunk.CompressedLength;
                UncompressedLength = sourceChunk.UncompressedLength;
            }

            /// <summary>
            /// Gets the SHA-1 hash chunk id.
            /// </summary>
            [ProtoMember(1)]
            public byte[] ChunkID { get; private set; }

            /// <summary>
            /// Gets the expected Adler32 checksum of this chunk.
            /// </summary>
            [ProtoMember(2)]
            public byte[] Checksum { get; private set; }

            /// <summary>
            /// Gets the chunk offset.
            /// </summary>
            [ProtoMember(3)]
            public ulong Offset { get; private set; }

            /// <summary>
            /// Gets the compressed length of this chunk.
            /// </summary>
            [ProtoMember(4)]
            public uint CompressedLength { get; private set; }

            /// <summary>
            /// Gets the decompressed length of this chunk.
            /// </summary>
            [ProtoMember(5)]
            public uint UncompressedLength { get; private set; }
        }

        [ProtoMember(1)]
        public List<FileData> Files { get; private set; }

        [ProtoMember(2)]
        public ulong ID { get; private set; }

        [ProtoMember(3)]
        public DateTime CreationTime { get; private set; }


        public static ProtoManifest LoadFromFile(string filename, out byte[] checksum,bool fromdepotdownloader = true)
        {
            if (!File.Exists(filename))
            {
                checksum = null;
                return null;
            }

            using (var ms = new MemoryStream())
            {
                using (var fs = File.Open(filename, FileMode.Open))

                    if (fromdepotdownloader)
                    {
                        using (var ds = new DeflateStream(fs, CompressionMode.Decompress))
                            ds.CopyTo(ms);

                    }
                    else
                    {
                        fs.CopyTo(ms);
                    }
                checksum = Util.SHAHash(ms.ToArray());

                ms.Seek(0, SeekOrigin.Begin);
                if (fromdepotdownloader)
                {
                    return Serializer.Deserialize<ProtoManifest>(ms);
                }
                else
                {
                    return new ProtoManifest(DepotManifest.Deserialize(ms.ToArray()),0);
                }
            }
        }


        public void SaveToFile(string filename, out byte[] checksum)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, this);

                checksum = Util.SHAHash(ms.ToArray());

                ms.Seek(0, SeekOrigin.Begin);

                using (var fs = File.Open(filename, FileMode.Create))
                using (var ds = new DeflateStream(fs, CompressionMode.Compress))
                    ms.CopyTo(ds);
            }
        }
    }
}
