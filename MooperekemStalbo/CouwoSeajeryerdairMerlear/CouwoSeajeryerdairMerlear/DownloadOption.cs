using CommandLine;

namespace CouwoSeajeryerdairMerlear
{
    [Verb("download")]
    public class DownloadOption
    {
        /// <summary>
        /// 下载的包
        /// </summary>
        [Option('p')]
        public string Package { set; get; }

        [Option('v')]
        public string Version { set; get; }

        [Option('o')]
        public string Output { set; get; }
    }
}