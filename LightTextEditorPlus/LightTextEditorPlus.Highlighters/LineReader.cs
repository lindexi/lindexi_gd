using Markdig.Syntax;

namespace LightTextEditorPlus.Highlighters;

/// <summary>
/// 行读取器，用于将传入的文本按行进行读取
/// </summary>
/// <param name="text"></param>
internal sealed class LineReader(string text)
{
    /// <summary>
    /// 读取下一行的内容
    /// </summary>
    /// <returns>如果还能读取到一行，则返回这一行的信息。否则返回 -1 长度</returns>
    public SourceSpan ReadLine()
    {
        if (_currentPosition >= text.Length)
        {
            return new SourceSpan(0, -1);
        }

        int lineStart = _currentPosition;
        while (_currentPosition < text.Length)
        {
            char currentChar = text[_currentPosition];
            if (currentChar is '\r' or '\n')
            {
                break;
            }

            _currentPosition++;
        }

        int lineEnd = _currentPosition - 1;

        if (_currentPosition < text.Length)
        {
            if (text[_currentPosition] == '\r' && _currentPosition + 1 < text.Length && text[_currentPosition + 1] == '\n')
            {
                _currentPosition += 2;
            }
            else
            {
                _currentPosition++;
            }
        }

        return new SourceSpan(lineStart, lineEnd);
    }

    /// <summary>
    /// 当前读取到的坐标
    /// </summary>
    private int _currentPosition;
}
