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
        textEditor.Text = await ReadAllTextAsync(file);
    }

    public async Task<string> ReadAllTextAsync(FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using var fileStream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var encodingDifferentiater = new EncodingDifferentiater(fileStream);
        var result = await encodingDifferentiater.InspectFileEncodingAsync();

        fileStream.Position = 0;
        using var streamReader = new StreamReader(fileStream, result.Encoding);
        return await streamReader.ReadToEndAsync();
    }
}
