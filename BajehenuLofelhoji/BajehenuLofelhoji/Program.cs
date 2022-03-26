// See https://aka.ms/new-console-template for more information

var nasSetupSubRepositoryRelativeFolderPath = @"Foo 1\Foo 2";

var fileName = "Foo 3应用.exe";

var relative = Path.Combine(nasSetupSubRepositoryRelativeFolderPath, fileName);

var url = "http://download.lindexi.com";

var uri = new Uri(url);

Uri.TryCreate(uri, relative, out var result);

var t = result.AbsoluteUri;


var c = (char) ('9' + 2);
var b1 = IsAsciiLetterOrDigit(c);
var b2 = IsAsciiLetterOrDigit2(c);

c = (char) ('0' - 2);
b1 = IsAsciiLetterOrDigit(c);
b2 = IsAsciiLetterOrDigit2(c);
var t2 = Convert.ToString('a', 2);

Console.Read();

static bool IsAsciiLetterOrDigit(char character) =>
    ((((uint) (character - 'A')) & ~0x20) < 26) ||
    (((uint) (character - '0')) < 10);

static bool IsAsciiLetterOrDigit2(char character)
{
    if (IsAsciiLetter(character))
    {
        return true;
    }
    if (character >= '0')
    {
        return (character <= '9');
    }
    return false;
}

static bool IsAsciiLetter(char character)
{
    if ((character >= 'a') && (character <= 'z'))
    {
        return true;
    }
    if (character >= 'A')
    {
        return (character <= 'Z');
    }
    return false;
}