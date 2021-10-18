using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenMcdf
{
    public partial class CompoundFile
    {
        public CompoundFile(Stream stream, IByteArrayPool byteArrayPool) //:this(stream)
        {
            this.header = new Header();
            this.directoryEntries = new List<IDirectoryEntry>();
            _byteArrayPool = byteArrayPool;

            this.sourceStream = stream;

            header.Read(stream);

            int sectorCount = Ceiling(((double)(stream.Length - GetSectorSize()) / (double)GetSectorSize()));

            if (stream.Length > 0x7FFFFF0)
                this._transactionLockAllocated = true;

            // Do not cache
            //sectors = new SectorCollection();
            LoadDirectoriesWithLowMemory();

            this.rootStorage
                = new CFStorage(this, directoryEntries[0]);

            DIFAT_SECTOR_FAT_ENTRIES_COUNT = (GetSectorSize() / 4) - 1;
            FAT_SECTOR_ENTRIES_COUNT = (GetSectorSize() / 4);

            _disableCache = true;
        }

        private readonly bool _disableCache;

        private readonly IByteArrayPool _byteArrayPool;

        /// <summary>
        /// Load directory entries from compound file. Header and FAT MUST be already loaded.
        /// </summary>
        private void LoadDirectoriesWithLowMemory()
        {
           var directoryChain = GetNormalSectorChainLowMemory(header.FirstDirectorySectorID);

           if (!(directoryChain.Count > 0))
               throw new CFCorruptedFileException("Directory sector chain MUST contain at least 1 sector");

           if (header.FirstDirectorySectorID == Sector.ENDOFCHAIN)
               header.FirstDirectorySectorID = directoryChain.IdIndexList[0];

           var dirReader = new ReadonlyStreamViewForSectorList(directoryChain, directoryChain.Count * GetSectorSize(),
               sourceStream, _byteArrayPool);

           while (dirReader.Position < dirReader.Length)
           {
               DirectoryEntry directoryEntry
                   = (DirectoryEntry) DirectoryEntry.New(string.Empty, StgType.StgInvalid, directoryEntries);
               //We are not inserting dirs. Do not use 'InsertNewDirectoryEntry'
               directoryEntry.Read(dirReader, this.Version);
           }
        }

        /// <summary>
        /// Get a standard sector chain
        /// </summary>
        /// <param name="secID">First SecID of the required chain</param>
        /// <returns>A list of sectors</returns>
        private SectorList GetNormalSectorChainLowMemory(int secID)
        {
            var sectorIdIndexList = new List<int>();
            int nextSecId = secID;
            var fatSectors = GetFatSectorChainLowMemory();

            var fatStream =
                new ReadonlyStreamViewForSectorList(fatSectors, fatSectors.Count * GetSectorSize(), sourceStream,
                    _byteArrayPool);

            while (nextSecId != Sector.ENDOFCHAIN)
            {
                if (nextSecId < 0)
                    throw new CFCorruptedFileException($"Next Sector ID reference is below zero. NextID : {nextSecId}");

                sectorIdIndexList.Add(nextSecId);

                const int sizeOfInt32 = 4;
                fatStream.Seek(nextSecId * sizeOfInt32, SeekOrigin.Begin);
                int next = fatStream.ReadInt32();

                nextSecId = next;
            }

            var sectorList = new SectorList(sectorIdIndexList,sourceStream,GetSectorSize(), SectorType.Normal);
            return sectorList;
        }

        /// <summary>
        /// Get the FAT sector chain
        /// </summary>
        /// <returns>List of FAT sectors</returns>
        private SectorList GetFatSectorChainLowMemory()
        {
           const int N_HEADER_FAT_ENTRY = 109; //Number of FAT sectors id in the header

            int nextSecId;

            List<Sector> difatSectors = GetDifatSectorChain();

            var idIndexList = new List<int>(Math.Min(header.FATSectorsNumber, N_HEADER_FAT_ENTRY));

            int idx = 0;

            // Read FAT entries from the header Fat entry array (max 109 entries)
            while (idx < header.FATSectorsNumber && idx < N_HEADER_FAT_ENTRY)
            {
                nextSecId = header.DIFAT[idx];
                idIndexList.Add(nextSecId);

                idx++;
            }

            //Is there any DIFAT sector containing other FAT entries ?
            if (difatSectors.Count > 0)
            {
                StreamView difatStream
                    = new StreamView
                        (
                        difatSectors,
                        GetSectorSize(),
                        header.FATSectorsNumber > N_HEADER_FAT_ENTRY ?
                            (header.FATSectorsNumber - N_HEADER_FAT_ENTRY) * 4 :
                            0,
                        null,
                            sourceStream
                        );

                int i = 0;

                while (idIndexList.Count < header.FATSectorsNumber)
                {
                    nextSecId = difatStream.ReadInt32();

                    idIndexList.Add(nextSecId);

                    if (difatStream.Position == ((GetSectorSize() - 4) + i * GetSectorSize()))
                    {
                        // Skip DIFAT chain fields considering the possibility that the last FAT entry has been already read
                        var sign = difatStream.ReadInt32();
                        if (sign == Sector.ENDOFCHAIN)
                            break;
                        else
                        {
                            i++;
                            continue;
                        }
                    }
                }
            }

            return new SectorList(idIndexList, sourceStream, GetSectorSize(), SectorType.FAT);
        }
    }



    class ReadonlyStreamViewForSectorList:Stream,IStreamReader
    {

        public ReadonlyStreamViewForSectorList(SectorList sectorChain, long length, Stream sourceStream,
            IByteArrayPool byteArrayPool)
        {
            _sectorChain = sectorChain;
            _sourceStream = sourceStream;
            _byteArrayPool = byteArrayPool;
            Length = length;
        }


        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public long Seek(long offset)
        {
            return Seek(offset, SeekOrigin.Begin);
        }

        public T ReadValue<T>(int length, Func<byte[], T> convert)
        {
            var buffer = _byteArrayPool.Rent(length);
            Read(buffer, 0, length);

            var result = convert(buffer);

            _byteArrayPool.Return(buffer);
            return result;
        }

        byte IStreamReader.ReadByte()
        {
            return ReadValue(1, buffer => buffer[0]);
        }

        public ushort ReadUInt16()
        {
            return ReadValue(2, buffer => (ushort) (buffer[0] | (buffer[1] << 8)));
        }

        public int ReadInt32()
        {
            return ReadValue(4, buffer => BitConverter.ToInt32(buffer, 0));
        }

        public uint ReadUInt32()
        {
            return ReadValue(4, buffer => (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24)));
        }

        public long ReadInt64()
        {
            return ReadValue(8, buffer =>
            {
                uint ls = (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
                uint ms = (uint)((buffer[4]) | (buffer[5] << 8) | (buffer[6] << 16) | (buffer[7] << 24));
                return (long)(((ulong)ms << 32) | ls);
            });
        }

        public ulong ReadUInt64()
        {
            return ReadValue(8, buffer => BitConverter.ToUInt64(buffer, 0));
        }

        public byte[] ReadBytes(int count)
        {
            byte[] result = new byte[count];
            Read(result, 0, count);
            return result;
        }

        public byte[] ReadBytes(int count, out int readCount)
        {
            byte[] result = new byte[count];
            readCount = Read(result, 0, count);
            return result;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int readCount = 0;
            var sectorChain = _sectorChain;
            if (sectorChain == null || sectorChain.Count <= 0)
            {
                return 0;
            }

            var sectorSize = sectorChain.SectorSize;
            // First sector
            int sectorIndex = (int)(Position / sectorSize);

            // Bytes to read count is the min between request count
            // and sector border

            var needToReadCount = Math.Min(sectorChain.SectorSize - ((int)Position % sectorSize),
                count);
            if (sectorIndex < sectorChain.Count)
            {
                var readPosition = (int) (Position % sectorSize);

                sectorChain.Read(sectorIndex, buffer, readPosition, offset, needToReadCount);
            }

            readCount += needToReadCount;
            sectorIndex++;
            // Central sectors
            while (readCount < (count - sectorSize))
            {
                needToReadCount = sectorSize;
                var readPosition = 0;
                sectorChain.Read(sectorIndex, buffer, readPosition, offset + readCount, needToReadCount);

                readCount += needToReadCount;
                sectorIndex++;
            }

            // Last sector
            needToReadCount = count - readCount;
            if (needToReadCount != 0)
            {
                if (sectorIndex > sectorChain.Count) throw new CFCorruptedFileException("The file is probably corrupted.");
                var readPosition = 0;
                readCount += sectorChain.Read(sectorIndex, buffer, readPosition, offset + readCount, needToReadCount);
            }

            Position += readCount;
            return readCount;
        }


        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                Position = offset;
                return Position;
            }

            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length { get; }

        public override long Position { set; get; }

        private readonly SectorList _sectorChain;
        private readonly Stream _sourceStream;
        private readonly IByteArrayPool _byteArrayPool;
    }
}
