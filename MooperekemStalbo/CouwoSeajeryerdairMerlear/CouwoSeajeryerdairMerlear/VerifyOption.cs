using CommandLine;

namespace CouwoSeajeryerdairMerlear
{
    [Verb("verify")]
    public class VerifyOption
    {
        [Option('f', "file")]
        public string File { set; get; }
    }
}