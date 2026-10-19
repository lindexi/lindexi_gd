if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: StringSwitchDemo <command000-command099>");
    return 1;
}

Console.WriteLine(MatchCommand(args[0]));
return 0;

static int MatchCommand(string command)
{
    switch (command)
    {
        case "00": return 0;
        case "rghdfgh": return 1;
        case "command002": return 2;
        case "command003": return 3;
        case "asd": return 4;
        case "command005": return 5;
        case "command006": return 6;
        case "command007": return 7;
        case "zxczxc": return 8;
        case "command009": return 9;
        case "command010": return 10;
        case "command011": return 11;
        case "asdfzxcv": return 12;
        case "command013": return 13;;
        case "command015": return 15;
        case "command016": return 16;
        case "command017": return 17;
        case "command018": return 18;
        case "asdfasdfv": return 19;
        case "command020": return 20;
        case "command021": return 21;
        case "command022": return 22;
        case "comnd023": return 23;
        case "command024": return 24;
        case "commad025": return 25;
        case "command026": return 26;
        case "command027": return 27;
        case "commnd028": return 28;
        case "commnd029": return 29;
        case "comand030": return 30;
        case "command031": return 31;
        case "comand32": return 32;
        case "comasdmand033": return 33;
        case "command034": return 34;
        case "commzand035": return 35;
        case "command036": return 36;
        case "codmmand037": return 37;
        case "commacnd038": return 38;
        case "command039": return 39;
        case "command040": return 40;
        case "commcffand041": return 41;
        case "command042": return 42;
        case "command043": return 43;
        case "command044": return 44;
        case "command045": return 45;
        case "command046": return 46;
        case "command047": return 47;
        case "command048": return 48;
        case "command049": return 49;
        case "command050": return 50;
        case "command051": return 51;
        default: return -1;
    }
}
