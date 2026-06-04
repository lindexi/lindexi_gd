using System;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Controls;
using LightTextEditorPlus;
using SimpleWrite.Business.TextEditors.CommandPatterns;

namespace SimpleWrite.Business.PluginCommandPatterns;

/// <summary>
/// 简易计算器命令模式：识别算术表达式（加减乘除），计算并在 Title 显示结果，执行时将结果复制到剪贴板
/// </summary>
sealed class CalculatorCommandPattern : ICommandPattern
{
    public int Priority => 0;
    public bool SupportSingleLine => true;

    private string _lastResult = string.Empty;

    public string Title => string.IsNullOrEmpty(_lastResult) ? "计算器" : $"= {_lastResult}";

    public ValueTask<bool> IsMatchAsync(string text)
    {
        if (TryEvaluate(text, out string result))
        {
            _lastResult = result;
            return ValueTask.FromResult(true);
        }

        _lastResult = string.Empty;
        return ValueTask.FromResult(false);
    }

    public async Task DoAsync(string text, TextEditor textEditor)
    {
        if (!TryEvaluate(text, out string result))
        {
            return;
        }

        TopLevel? topLevel = TopLevel.GetTopLevel(textEditor);
        if (topLevel?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(result);
        }
    }

    private static bool TryEvaluate(string text, out string result)
    {
        result = string.Empty;

        string normalized = text.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        if (!ContainsOperator(normalized))
        {
            return false;
        }

        string expression = RemoveWhitespace(normalized);
        if (expression.Length == 0)
        {
            return false;
        }

        var parser = new ExpressionParser(expression);
        if (!parser.TryParse(out double value))
        {
            return false;
        }

        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return false;
        }

        // 整数则去掉小数点
        if (Math.Abs(value - Math.Round(value, MidpointRounding.AwayFromZero)) < 1e-12)
        {
            result = ((long)Math.Round(value, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            result = value.ToString("G", CultureInfo.InvariantCulture);
        }

        return true;
    }

    private static bool ContainsOperator(string text)
    {
        foreach (char c in text)
        {
            if (c is '+' or '-' or '*' or '/')
            {
                return true;
            }
        }

        return false;
    }

    private static string RemoveWhitespace(string text)
    {
        // 预估长度即原长度，大多数情况下文本不含空白字符
        char[] buffer = new char[text.Length];
        int index = 0;
        foreach (char c in text)
        {
            if (!char.IsWhiteSpace(c))
            {
                buffer[index++] = c;
            }
        }

        return new string(buffer, 0, index);
    }

    /// <summary>
    /// 简单递归下降表达式解析器，支持 + - * / 和括号
    /// </summary>
    private ref struct ExpressionParser
    {
        private readonly ReadOnlySpan<char> _expression;
        private int _position;
        private TokenType _currentToken;
        private double _currentNumber;

        public ExpressionParser(ReadOnlySpan<char> expression)
        {
            _expression = expression;
            _position = 0;
            _currentToken = TokenType.None;
            _currentNumber = 0;
            Advance();
        }

        public bool TryParse(out double result)
        {
            if (_currentToken == TokenType.None)
            {
                result = 0;
                return false;
            }

            if (!ParseExpression(out result))
            {
                return false;
            }

            // 解析完成后应到达末尾
            return _currentToken == TokenType.None;
        }

        private bool ParseExpression(out double result)
        {
            if (!ParseTerm(out result))
            {
                return false;
            }

            while (_currentToken is TokenType.Plus or TokenType.Minus)
            {
                TokenType op = _currentToken;
                Advance();
                if (!ParseTerm(out double right))
                {
                    return false;
                }

                if (op == TokenType.Plus)
                {
                    result += right;
                }
                else
                {
                    result -= right;
                }
            }

            return true;
        }

        private bool ParseTerm(out double result)
        {
            if (!ParseUnary(out result))
            {
                return false;
            }

            while (_currentToken is TokenType.Multiply or TokenType.Divide)
            {
                TokenType op = _currentToken;
                Advance();
                if (!ParseUnary(out double right))
                {
                    return false;
                }

                if (op == TokenType.Multiply)
                {
                    result *= right;
                }
                else
                {
                    if (Math.Abs(right) < 1e-15)
                    {
                        return false; // 除零
                    }

                    result /= right;
                }
            }

            return true;
        }

        private bool ParseUnary(out double result)
        {
            if (_currentToken is TokenType.Plus or TokenType.Minus)
            {
                TokenType op = _currentToken;
                Advance();
                if (!ParseUnary(out result))
                {
                    return false;
                }

                if (op == TokenType.Minus)
                {
                    result = -result;
                }

                return true;
            }

            return ParsePrimary(out result);
        }

        private bool ParsePrimary(out double result)
        {
            if (_currentToken == TokenType.Number)
            {
                result = _currentNumber;
                Advance();
                return true;
            }

            if (_currentToken == TokenType.LeftParen)
            {
                Advance(); // 跳过 '('
                if (!ParseExpression(out result))
                {
                    return false;
                }

                if (_currentToken != TokenType.RightParen)
                {
                    return false;
                }

                Advance(); // 跳过 ')'
                return true;
            }

            result = 0;
            return false;
        }

        private void Advance()
        {
            SkipWhitespace();

            if (_position >= _expression.Length)
            {
                _currentToken = TokenType.None;
                return;
            }

            char c = _expression[_position];

            if (char.IsDigit(c) || (c == '.' && _position + 1 < _expression.Length && char.IsDigit(_expression[_position + 1])))
            {
                ParseNumber();
                return;
            }

            _currentToken = c switch
            {
                '+' => TokenType.Plus,
                '-' => TokenType.Minus,
                '*' => TokenType.Multiply,
                '/' => TokenType.Divide,
                '(' => TokenType.LeftParen,
                ')' => TokenType.RightParen,
                _ => TokenType.Invalid
            };

            _position++;
        }

        private void ParseNumber()
        {
            int start = _position;
            bool hasDecimal = false;

            while (_position < _expression.Length)
            {
                char c = _expression[_position];
                if (char.IsDigit(c))
                {
                    _position++;
                }
                else if (c == '.' && !hasDecimal)
                {
                    hasDecimal = true;
                    _position++;
                }
                else
                {
                    break;
                }
            }

            ReadOnlySpan<char> numberSpan = _expression[start.._position];
#if NET7_0_OR_GREATER
            if (double.TryParse(numberSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
#else
            if (double.TryParse(numberSpan.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
#endif
            {
                _currentToken = TokenType.Number;
                _currentNumber = value;
            }
            else
            {
                _currentToken = TokenType.Invalid;
            }
        }

        private void SkipWhitespace()
        {
            while (_position < _expression.Length && char.IsWhiteSpace(_expression[_position]))
            {
                _position++;
            }
        }

        private enum TokenType
        {
            None,
            Number,
            Plus,
            Minus,
            Multiply,
            Divide,
            LeftParen,
            RightParen,
            Invalid
        }
    }
}
