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
        }

        private readonly IByteArrayPool _byteArrayPool;

        /// <summary>
        /// Load directory entries from compound file. Header and FAT MUST be already loaded.
        /// </summary>
        private void LoadDirectoriesWithLowMemory()
        {
           var directoryChain = GetSectorChainLowMemory(header.FirstDirectorySectorID, SectorType.Normal);

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
        /// Get a sector chain from a compound file given the first sector ID
        /// and the required sector type.
        /// </summary>
        /// <param name="secId">First chain sector's id </param>
        /// <param name="chainType">Type of Sectors in the required chain (mini sectors, normal sectors or FAT)</param>
        /// <returns>A list of Sectors as the result of their concatenation</returns>
        private SectorList GetSectorChainLowMemory(int secId, SectorType chainType)
        {
            //switch (chainType)
            //{
            //    case SectorType.DIFAT:
            //        return GetDifatSectorChain();

            //    case SectorType.FAT:
            //        return GetFatSectorChain();

            //    case SectorType.Normal:
            //        return GetNormalSectorChain(secId);
            //GetNormalSectorChain(secId);
            return GetNormalSectorChainLowMemory(secId);

            //    case SectorType.Mini:
            //        return GetMiniSectorChain(secId);

            //    default:
            //        throw new CFException("Unsupproted chain type");
            //}


        }

        /// <summary>
        /// Get a standard sector chain
        /// </summary>
        /// <param name="secID">First SecID of the required chain</param>
        /// <returns>A list of sectors</returns>
        private SectorList GetNormalSectorChainLowMemory(int secID)
        {
            var sectorIdIndexList = new List<int>();
            int nextSecID = secID;
            var fatSectors = GetFatSectorChainLowMemory();
            //HashSet<int> processedSectors = new HashSet<int>();

            var fatStream =
                new ReadonlyStreamViewForSectorList(fatSectors, fatSectors.Count * GetSectorSize(), sourceStream,
                    _byteArrayPool);

            while (nextSecID != Sector.ENDOFCHAIN)
            {
                if (nextSecID < 0)
                    throw new CFCorruptedFileException($"Next Sector ID reference is below zero. NextID : {nextSecID}");

                //if (nextSecID >= sectors.Count)
                //    throw new CFCorruptedFileException(
                //        $"Next Sector ID reference an out of range sector. NextID : {nextSecID} while sector count {sectors.Count}");

                sectorIdIndexList.Add(nextSecID);

                const int sizeOfInt32 = 4;
                fatStream.Seek(nextSecID * sizeOfInt32, SeekOrigin.Begin);
                int next = fatStream.ReadInt32();

                //EnsureUniqueSectorIndex(next, processedSectors);
                nextSecID = next;
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
                HashSet<int> processedSectors = new HashSet<int>();
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

                    EnsureUniqueSectorIndex(nextSecId, processedSectors);

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



    class ReadonlyStreamViewForSectorList:Stream
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

        public int ReadInt32()
        {
            var buffer = _byteArrayPool.Rent(4);
            const int sizeOfInt32 = 4;
            Read(buffer, 0, sizeOfInt32);
            return BitConverter.ToInt32(buffer, 0);
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
                sectorChain.Read(sectorIndex, buffer, readPosition, offset + readCount, needToReadCount);
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

    class SectorList
    {
        public SectorList(List<int> idIndexList, Stream sourceStream, int sectorSize, SectorType type)
        {
            IdIndexList = idIndexList;
            SourceStream = sourceStream;
            SectorSize = sectorSize;
            Type = type;
        }

        public SectorList(Stream sourceStream, int sectorSize, SectorType type)
        {
            SourceStream = sourceStream;
            SectorSize = sectorSize;
            Type = type;

            IdIndexList = new List<int>();
        }

        public List<int> IdIndexList { set; get; }

        public int Count => IdIndexList.Count;

        public Stream SourceStream { get; }

        public int SectorSize { get; }

        public SectorType Type { get; }

        public int Read(int sectorIndex, byte[] buffer, int position, int offset, int count)
        {
            var idIndex = IdIndexList[sectorIndex];
            var sectorPosition = SectorSize + idIndex * SectorSize;

            var streamPosition = sectorPosition + position;
            SourceStream.Seek(streamPosition, SeekOrigin.Begin);
            return SourceStream.Read(buffer, offset, count);
        }
    }
}
