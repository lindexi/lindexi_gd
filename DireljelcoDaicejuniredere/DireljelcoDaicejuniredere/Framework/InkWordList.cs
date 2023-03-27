using System.Runtime.InteropServices;

namespace DireljelcoDaicejuniredere;

[CoClass(typeof(InkWordListClass))]
[Guid("76BA3491-CB2F-406B-9961-0E0C4CDAAEF2")]
[ComImport]
internal interface InkWordList : IInkWordList
{
}