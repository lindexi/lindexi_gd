using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace HiaiemhiuBobmnawa
{
    [ComVisible(true)]
    [Guid(FilePluralizationGeneratorId)]
    [CodeGeneratorRegistrationAttribute(typeof(FilePluralizationGenerator), nameof(FilePluralizationGenerator), VSConstants.UICONTEXT.CSharpProject_string, GeneratesDesignTimeSource = true)]
    [ProvideObject(typeof(FilePluralizationGenerator))]
    public sealed class FilePluralizationGenerator : IVsSingleFileGenerator
    {
        public int DefaultExtension(out string defaultExtension)
        {
            defaultExtension = ".generated.cs";
            return defaultExtension.Length;
        }

        public const string FilePluralizationGeneratorId = "2F6B300C-6A90-4C08-8F11-D4E0C75391C4";

        public int Generate(string wszInputFilePath, string bstrInputFileContents, string wszDefaultNamespace, IntPtr[] rgbOutputFileContents, out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
        {
            var bytes = Encoding.UTF8.GetBytes("林德熙");
            var length = bytes.Length;

            rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(length);
            Marshal.Copy(bytes, 0, rgbOutputFileContents[0], length);
            pcbOutput = (uint) length;

            return VSConstants.S_OK;
        }
    }
}
