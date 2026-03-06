using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EncodingUtf8AndGBKDifferentiater;

using LightTextEditorPlus;

namespace SimpleWrite.Business.FileHandlers;

internal class TextFileReader
{
    public async Task ReadToTextEditor(FileInfo file, TextEditor textEditor)
    {
        // 先识别文件编码
        await using var fileStream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var encodingDifferentiater = new EncodingDifferentiater(fileStream);
        var result = await encodingDifferentiater.InspectFileEncodingAsync();

        fileStream.Position = 0;
        var streamReader = new StreamReader(fileStream, result.Encoding);
        var text = await streamReader.ReadToEndAsync();
        textEditor.Text = text;
    }
}
