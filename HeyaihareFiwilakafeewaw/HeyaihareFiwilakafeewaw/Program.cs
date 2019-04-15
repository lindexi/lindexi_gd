using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using my.utils;

namespace HeyaihareFiwilakafeewaw
{
    class Program
    {
        static void Main(string[] args)
        {
            var sourceFolder = @"F:\temp\source";
            var targetFolder = @"F:\temp\target";
            var outFolder = @"F:\temp\out";

            foreach (var temp in GetDiffFile(new DirectoryInfo(sourceFolder), new DirectoryInfo(targetFolder)))
            {
                var incrementalData = FileToIncrementalData(temp.SourceFile.FullName, temp.TargetFile.FullName);
                IncrementalDataToFile(incrementalData, Path.Combine(outFolder, temp.SourceFile.Name));
            }

            //var file1 = @"F:\temp\file1";
            //var file2 = @"F:\temp\file2";

            //var foo = FileToIncrementalData(file1, file2);


            //IncrementalDataToFile(foo, @"F:\temp\file2.inc");

            
            Console.Read();
        }

        private static IEnumerable<DiffFile> GetDiffFile(DirectoryInfo sourceFolder, DirectoryInfo targetFolder)
        {
            // 比较两个文件夹不同文件
            // 也就是在 Target 里面存在同时在 sourceFolder 也存在的文件才算
            foreach (var temp in targetFolder.GetFiles())
            {
                var sourceFile = new FileInfo(Path.Combine(sourceFolder.FullName, temp.Name));
                if (sourceFile.Exists)
                {
                    yield return new DiffFile(sourceFile, temp);
                }
            }
        }


        private static Diff.Item[] FileToIncrementalData(string sourceFile, string targetFile)
        {
            var array1 = File.ReadAllBytes(sourceFile).Select(temp => (int) temp).ToArray();
            var array2 = File.ReadAllBytes(targetFile).Select(temp => (int) temp).ToArray();

            return Diff.DiffInt(array1, array2);
        }

        private static void IncrementalDataToFile(Diff.Item[] incrementalData, string file)
        {
            using (var stream = new BinaryWriter(new FileStream(file, FileMode.Create)))
            {
                foreach (var temp in incrementalData)
                {
                    stream.Write(temp.StartA);
                    stream.Write(temp.StartB);
                    stream.Write(temp.deletedA);
                    stream.Write(temp.insertedB);
                    for (int i = 0; i < temp.insertedB; i++)
                    {
                        Debug.Assert(temp.InsertedData[i] <= byte.MaxValue);
                        stream.Write((byte) temp.InsertedData[i]);
                    }
                }
            }
        }


        /// <summary>
        /// 从差分文件还原文件
        /// </summary>
        /// <param name="incrementalDataFile"></param>
        /// <param name="sourceFile"></param>
        /// <param name="outFile"></param>
        private static void IncrementalDataFile(string incrementalDataFile, string sourceFile, string outFile)
        {
            using (var sourceStream = new BinaryReader(new FileStream(sourceFile, FileMode.Open)))
            using (var incrementalDataStream = new BinaryReader(new FileStream(incrementalDataFile, FileMode.Open)))
            using (var outStream = new BinaryWriter(new FileStream(outFile, FileMode.Create)))
            {
                while (incrementalDataStream.BaseStream.Position < incrementalDataStream.BaseStream.Length)
                {
                    var incrementalData = ReadIncrementalData(incrementalDataStream);

                    var buffer =
                        sourceStream.ReadBytes((int) (incrementalData.StartA - sourceStream.BaseStream.Position));

                    outStream.Write(buffer);

                    sourceStream.BaseStream.Position += incrementalData.DeletedA;

                    outStream.Write(incrementalData.InsertedData);
                }
            }
        }

        private static IncrementalData ReadIncrementalData(BinaryReader stream)
        {
            var startA = stream.ReadInt32();
            var startB = stream.ReadInt32();
            var deletedA = stream.ReadInt32();
            var insertedB = stream.ReadInt32();
            var insertedData = stream.ReadBytes(insertedB);

            return new IncrementalData()
            {
                DeletedA = deletedA,
                InsertedB = insertedB,
                InsertedData = insertedData,
                StartA = startA,
                StartB = startB
            };
        }

        class DiffFile
        {
            /// <inheritdoc />
            public DiffFile(FileInfo sourceFile, FileInfo targetFile)
            {
                SourceFile = sourceFile;
                TargetFile = targetFile;
            }

            public FileInfo SourceFile { get; }

            public FileInfo TargetFile { get; }
        }


        public class IncrementalData
        {
            /// <summary>Start Line number in Data A.</summary>
            public int StartA { set; get; }

            /// <summary>Start Line number in Data B.</summary>
            public int StartB { set; get; }

            /// <summary>Number of changes in Data A.</summary>
            public int DeletedA { set; get; }

            /// <summary>Number of changes in Data B.</summary>
            public int InsertedB { set; get; }

            /// <summary>
            /// 插入数据
            /// </summary>
            public byte[] InsertedData { set; get; }
        }
    }
}
