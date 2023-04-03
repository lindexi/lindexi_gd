using System;
using System.Runtime.InteropServices;
using System.Text;

class Program
{
    [ComImport]
    [Guid("D53552C8-77E3-101A-B552-08002B33B0E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IWordBreaker
    {
        void Init([MarshalAs(UnmanagedType.Bool)] bool fQuery, uint maxTokenSize, [MarshalAs(UnmanagedType.Bool)] out bool pfLicense);
        void BreakText([In] ref TEXT_SOURCE pTextSource, [In] ref IWordSink pWordSink, [In] ref HILITE_TEXT pHiliteText);
        void ComposePhrase(byte[] pwcNoun, uint cwcNoun, byte[] pwcModifier, uint cwcModifier, uint ulAttachmentType, IntPtr pPhrase, [MarshalAs(UnmanagedType.Bool)] bool fNounPhrase);
        void GetLicenseToUse([MarshalAs(UnmanagedType.LPWStr)] out string ppwcsLicense);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TEXT_SOURCE
    {
        //[MarshalAs(UnmanagedType.FunctionPtr)]
        public delegate uint pfnFillTextBuffer([MarshalAs(UnmanagedType.LPWStr)] out string pText, ref uint dwFlags);

        //[MarshalAs(UnmanagedType.FunctionPtr)]
        public delegate void pfnNotifySink(uint start, uint end);

        [MarshalAs(UnmanagedType.Interface)]
        public object pID;     // reserved pointer

        public pfnFillTextBuffer fillTextBuffer;
        public pfnNotifySink notifySink;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HILITE_TEXT
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pwcInBuffer;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string pwcOutBuffer;

        public uint cwc;
        public uint ichReq;
        public uint cchReq;
        public uint iHilitePos;
        public uint iHiLiteLen;
    }

    private class WordSink : IWordSink
    {
        public void PutWord(uint cwc, [MarshalAs(UnmanagedType.LPWStr)] string pwcInBuf, uint cwcSrcLen, uint cwcSrcPos)
        {
            Console.WriteLine(pwcInBuf.Substring((int) cwcSrcPos, (int) cwc));
        }

        public void PutAltWord(uint cwc, string pwcInBuf, uint cwcSrcLen, uint cwcSrcPos)
        {
            throw new NotImplementedException();
        }

        public void StartAltPhrase()
        {
            throw new NotImplementedException();
        }

        public void EndAltPhrase()
        {
            throw new NotImplementedException();
        }
    }

    [ComImport]
    [Guid("CC907054-C058-101A-B554-08002B33B0E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IWordSink
    {
        void PutWord(uint cwc, [MarshalAs(UnmanagedType.LPWStr)] string pwcInBuf, uint cwcSrcLen, uint cwcSrcPos);
        void PutAltWord(uint cwc, [MarshalAs(UnmanagedType.LPWStr)] string pwcInBuf, uint cwcSrcLen, uint cwcSrcPos);
        void StartAltPhrase();
        void EndAltPhrase();
    }

    [DllImport("ole32.dll")]
    private static extern int CoInitialize(IntPtr pvReserved);

    [DllImport("ole32.dll",SetLastError = true)]
    private static extern int CoCreateInstance([MarshalAs(UnmanagedType.LPStruct)] Guid rclsid, IntPtr pUnkOuter, uint dwClsContext, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IWordBreaker ppv);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();

    [STAThread]
    static void Main(string[] args)
    {
        CoInitialize(IntPtr.Zero);

        try
        {
            string text = "Hello, World!";     // 要检查单词边界的文本
            int length = Encoding.Unicode.GetByteCount(text) / 2;    // 获取文本长度

            // 创建 Word Breaker 实例
            Guid CLSID_ComWordBreaker = new Guid("80F1BF5A-C610-11D3-AD60-00C04F6BB853");
            Guid IID_IWordBreaker = new Guid("D53552C8-77E3-101A-B552-08002B33B0E6");
            IWordBreaker wordBreaker;
            bool pfLicense;
            const int CLSCTX_INPROC_SERVER = 0x1;
            var result = CoCreateInstance(CLSID_ComWordBreaker, IntPtr.Zero, (uint) CLSCTX_INPROC_SERVER, IID_IWordBreaker, out wordBreaker);
            var lastPInvokeError = Marshal.GetLastPInvokeError();
            
            wordBreaker.Init(true, 64, out pfLicense);

            // 检查单词边界并输出单词
            TEXT_SOURCE ts = new TEXT_SOURCE();
            ts.fillTextBuffer = delegate (out string textChunk, ref uint dwFlags)
            {
                textChunk = text;
                dwFlags = 0;
                return (uint) length;
            };

            IWordSink wordSink = new WordSink();
            HILITE_TEXT haHiliteText = new HILITE_TEXT();
            wordBreaker.BreakText(ref ts, ref wordSink, ref haHiliteText);

            Marshal.ReleaseComObject(wordBreaker);
        }
        finally
        {
            CoUninitialize();
        }
    }
}