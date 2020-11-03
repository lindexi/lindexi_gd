using Blog.Model;
using CommandLine;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System;
using System.Diagnostics.CodeAnalysis;

namespace BlogTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<FileNameConverter, FileListConverter>(args).MapResult<FileNameConverter, FileListConverter, int>(ConvertFileName, ConvertFileList, _ => 0);
        }

        private static int ConvertFileName(FileNameConverter fileNameConverter)
        {
            List<BlogExcerptModel> blogList = new List<BlogExcerptModel>();
            var folder = fileNameConverter.FolderPath.Trim();
            foreach (var file in Directory.GetFiles(folder, "*.md"))
            {
                var blog = ParseBlog(file);

                if (blog == null)
                {
                    continue;
                }

                var fileName = BlogNameConverter.ConvertTitleToFileName(blog.Title);
                fileName += ".md";
                blog.FileName = fileName;
                Debug.WriteLine(fileName);
                File.Move(file, Path.Combine(folder, fileName), true);

                blogList.Add(blog);
            }

            blogList.Sort(new BlogExcerptModelComparer());
            blogList.Reverse();

            var json = JsonSerializer.Serialize(blogList, new JsonSerializerOptions()
            {
                IgnoreNullValues = true,
                WriteIndented = false,
            });

            File.WriteAllText(Path.Combine(folder, "Summary.json"), json);

            File.WriteAllText(Path.Combine(folder, "FileList.txt"), string.Join("\n", blogList.Select(temp => temp.Title)));

            var pageCount = blogList.Count * 1.0 / MaxBlogPrePage;

            for (int i = 0; i < pageCount; i++)
            {
                BlogPageModel blogPage = new BlogPageModel
                {
                    PageCount = (int)pageCount,
                    BlogList = blogList.Skip(i * MaxBlogPrePage).Take(MaxBlogPrePage).ToList()
                };

                json = JsonSerializer.Serialize(blogPage, new JsonSerializerOptions()
                {
                    IgnoreNullValues = true,
                    WriteIndented = false,
                });

                File.WriteAllText(Path.Combine(folder, $"Summary-{i}.json"), json);
            }

            return 0;
        }

        private const int MaxBlogPrePage = 25;

        private static BlogExcerptModel ParseBlog(string file)
        {
            var text = File.ReadAllText(file);

            if (text.Contains("<!-- 草稿 -->") || text.Contains("<!-- 不发布 -->"))
            {
                return null;
            }

            var reader = new StringReader(text);
            var firstLine = reader.ReadLine();
            string title;
            if (firstLine.StartsWith("#"))
            {
                title = firstLine[1..];
                title = title.Trim();
            }
            else
            {
                title = Path.GetFileNameWithoutExtension(file);
            }

            var excerpt = new StringBuilder();

            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    if (excerpt.Length > 300)
                    {
                        excerpt.Remove(300, excerpt.Length - 300);
                    }

                    break;
                }
                else if (line.Contains("<!--more-->"))
                {
                    break;
                }

                excerpt.AppendLine(line);
            }

            DateTime? createTime = null;
            var match = Regex.Match(text, @"<!-- CreateTime:([\S ]+) -->");
            if (match.Success)
            {
                if (DateTime.TryParse(match.Groups[1].Value, out var time))
                {
                    createTime = time;
                }
            }

            if (createTime is null)
            {
                createTime = File.GetCreationTime(file);
            }

            return new BlogExcerptModel
            {
                Title = title,
                Excerpt = excerpt.ToString().Trim(),
                Time = createTime,
            };
        }


        private static int ConvertFileList(FileListConverter fileListConverter)
        {
            return 0;
        }
    }

    class BlogExcerptModelComparer : IComparer<BlogExcerptModel>
    {
        public int Compare([AllowNull] BlogExcerptModel x, [AllowNull] BlogExcerptModel y)
        {
            if (x?.Time is null && y?.Time is null) return 0;
            if (x?.Time is null) return -1;
            if (y?.Time is null) return 1;

            return x.Time.Value.CompareTo(y.Time.Value);
        }
    }

    [Verb("ConvertFileList")]
    public class FileListConverter
    {

    }

    [Verb("ConvertFileName")]
    public class FileNameConverter
    {
        [Option('f')]
        public string FolderPath { set; get; }
    }
}
