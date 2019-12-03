using System;
using System.Collections.Generic;
using System.IO;

namespace LwufxgbaDljqkx
{
    interface IBinaryFile
    {
    }

    class DataInNewFile : IBinaryFile
    {
        public List<byte> Data { set; get; } = new List<byte>();
    }

    class DataInOldFile : IBinaryFile
    {
        public int OldFileIndex { set; get; }

        public int Length { set; get; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var oldFile = File.ReadAllBytes("Newtonsoft.Json 9.0.1.dll");
            var newFile = File.ReadAllBytes("Newtonsoft.Json 11.0.2.dll");

            List<IBinaryFile> file = GetBinary(oldFile, newFile);

            file = Deserialize(Serialize(file));

            if (Equals(newFile, new Span<byte>(GetNewFile(oldFile, file).ToArray())))
            {
            }
        }

        private static List<IBinaryFile> Deserialize(byte[] binary)
        {
            List<IBinaryFile> file = new List<IBinaryFile>();

            var binaryReader = new BinaryReader(new MemoryStream(binary));

            while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length)
            {
                var n = binaryReader.ReadInt32();
                if (n < 0)
                {
                    n = -n;
                    var data = new List<byte>(n);
                    for (int i = 0; i < n; i++)
                    {
                        data.Add(binaryReader.ReadByte());
                    }

                    file.Add(new DataInNewFile()
                    {
                        Data = data
                    });
                }
                else
                {
                    var oldFileIndex = n;
                    var length = binaryReader.ReadInt32();
                    file.Add(new DataInOldFile()
                    {
                        OldFileIndex = oldFileIndex,
                        Length = length
                    });
                }
            }

            return file;
        }

        private static byte[] Serialize(List<IBinaryFile> file)
        {
            var stream = new MemoryStream();
            var binaryWriter = new BinaryWriter(stream);

            foreach (var temp in file)
            {
                if (temp is DataInNewFile dataInNewFile)
                {
                    binaryWriter.Write(-dataInNewFile.Data.Count);
                    binaryWriter.Write(dataInNewFile.Data.ToArray());
                }

                if (temp is DataInOldFile dataInOldFile)
                {
                    binaryWriter.Write(dataInOldFile.OldFileIndex);
                    binaryWriter.Write(dataInOldFile.Length);
                }
            }

            return stream.ToArray();
        }

        private static List<IBinaryFile> GetBinary(byte[] oldFile, byte[] newFile)
        {
            var file = new List<IBinaryFile>();
            IBinaryFile binaryFile = null;

            for (int i = 0; i < newFile.Length; i++)
            {
                Console.WriteLine($"{i}/{newFile.Length}");

                (int findIndex, int findLength) = FindBinary(oldFile, newFile, i);
                if (findIndex < 0)
                {
                    //没有在之前文件找到数据，这部分数据是只有新文件
                    if (binaryFile is null)
                    {
                        binaryFile = new DataInNewFile();
                    }

                    ((DataInNewFile)binaryFile).Data.Add(newFile[i]);
                }
                else
                {
                    if (binaryFile is DataInNewFile)
                    {
                        file.Add(binaryFile);
                        binaryFile = null;
                    }

                    file.Add(new DataInOldFile()
                    {
                        OldFileIndex = findIndex,
                        Length = findLength
                    });

                    i += findLength - 1;
                }
            }

            if (binaryFile != null)
            {
                file.Add(binaryFile);
            }

            return file;
        }

        private static List<byte> GetNewFile(byte[] oldFile, List<IBinaryFile> file)
        {
            var newFile = new List<byte>();

            foreach (var temp in file)
            {
                if (temp is DataInNewFile dataInNewFile)
                {
                    newFile.AddRange(dataInNewFile.Data);
                }

                if (temp is DataInOldFile dataInOldFile)
                {
                    var span = new Span<byte>(oldFile, dataInOldFile.OldFileIndex, dataInOldFile.Length);

                    newFile.AddRange(span.ToArray());
                }
            }

            return newFile;
        }

        private static (int findIndex, int findLength) FindBinary(byte[] oldFile, byte[] newFile, int newFileIndex)
        {
            var findLength = 8;

            var startIndex = 0;

            var findIndex = FindBinary(oldFile, newFile, newFileIndex, findLength, startIndex);

            if (findIndex < 0)
            {
                return (-1, 0);
            }
            else
            {
                while (true)
                {
                    var currentFindIndex = findIndex;

                    while (true)
                    {
                        findLength++;
                        if (oldFile.Length > currentFindIndex + findLength - 1)
                        {
                            if (newFile.Length > newFileIndex + findLength - 1)
                            {
                                if (oldFile[currentFindIndex + findLength - 1] == newFile[newFileIndex + findLength - 1])
                                {
                                    continue;
                                }
                            }
                        }

                        break;
                    }

                    startIndex = findIndex;
                    findIndex = FindBinary(oldFile, newFile, newFileIndex, findLength, startIndex);

                    if (findIndex < 0)
                    {
                        return (currentFindIndex, findLength - 1);
                    }

                }
            }
        }

        private static int FindBinary(byte[] oldFile, byte[] newFile, int newFileIndex, int findLength, int startIndex)
        {
            if (newFile.Length <= newFileIndex + findLength)
            {
                return -1;
            }

            var arrayFind = new Span<byte>(newFile, newFileIndex, findLength);
            var findIndex = TryFindNewFile(oldFile, arrayFind, startIndex);
            return findIndex;
        }

        private static int TryFindNewFile(byte[] newFile, Span<byte> arrayFind, int startIndex)
        {
            var findLength = arrayFind.Length;
            for (int i = startIndex; i < newFile.Length; i++)
            {
                if (newFile.Length < i + findLength)
                {
                    return -1;
                }

                var source = new Span<byte>(newFile, i, findLength);

                if (Equals(source, arrayFind))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool Equals(Span<byte> source, Span<byte> arrayFind)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != arrayFind[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
