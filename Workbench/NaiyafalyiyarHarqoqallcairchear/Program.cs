// See https://aka.ms/new-console-template for more information

// -value:test --foo:test /fx1:test -number:42 /flag:true -PascalCase:value --camelCase:value --kebab-case:value --flag1 --flag2:false --v1 v1 -value2 v2 /value3 v3 --valueue=testue -c d1 -e:f2 -g:ho
using NaiyafalyiyarHarqoqallcairchear;

string[] argument = 
[
    "-value:test",
    "--foo:test",
    "/fx1:test",
    "-number:42",
    "/flag:true",
    "-PascalCase:value",
    "--camelCase:value",
    "--kebab-case:value",
    "--flag1",
    "--flag2:false",
    "--v1",
    "v1",
    "-value2",
    "v2",
    "/value3",
    "v3",
    "--valueue:testue",
    "-c",
    "d1",
    "-e:f2",
    "-g:ho"
];

_ = LindexiCommandLine.Parse<FlexibleOption>(argument);

Console.WriteLine("Hello, World!");
