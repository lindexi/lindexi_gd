using CommandLine;

namespace CouwoSeajeryerdairMerlear
{
    [Verb("upload")]
    public class UploadOption
    {
        /// <summary>
        /// 下载的包
        /// </summary>
        [Option('f',Required = true)]
        public string File { set; get; }
    }
}