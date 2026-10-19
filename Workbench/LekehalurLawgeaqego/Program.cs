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
        case "command013": return 13;
        case "command014": return 14;
        case "command015": return 15;
        case "command016": return 16;
        case "command017": return 17;
        case "command018": return 18;
        case "asdfasdfv": return 19;
        case "command020": return 20;
        case "command021": return 21;
        case "command022": return 22;
        case "command023": return 23;
        case "command024": return 24;
        case "command025": return 25;
        case "command026": return 26;
        case "command027": return 27;
        case "command028": return 28;
        case "command029": return 29;
        case "command030": return 30;
        case "command031": return 31;
        case "command032": return 32;
        case "command033": return 33;
        case "command034": return 34;
        case "command035": return 35;
        case "command036": return 36;
        case "command037": return 37;
        case "command038": return 38;
        case "command039": return 39;
        case "command040": return 40;
        case "command041": return 41;
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
        case "command052": return 52;
        case "command053": return 53;
        case "command054": return 54;
        case "command055": return 55;
        case "command056": return 56;
        case "command057": return 57;
        case "command058": return 58;
        case "command059": return 59;
        case "command060": return 60;
        case "command061": return 61;
        case "command062": return 62;
        case "command063": return 63;
        case "command064": return 64;
        case "command065": return 65;
        case "command066": return 66;
        case "command067": return 67;
        case "command068": return 68;
        case "command069": return 69;
        case "command070": return 70;
        case "command071": return 71;
        case "command072": return 72;
        case "command073": return 73;
        case "command074": return 74;
        case "command075": return 75;
        case "command076": return 76;
        case "command077": return 77;
        case "command078": return 78;
        case "command079": return 79;
        case "command080": return 80;
        case "command081": return 81;
        case "command082": return 82;
        case "command083": return 83;
        case "command084": return 84;
        case "command085": return 85;
        case "command086": return 86;
        case "command087": return 87;
        case "command088": return 88;
        case "command089": return 89;
        case "command090": return 90;
        case "command091": return 91;
        case "command092": return 92;
        case "command093": return 93;
        case "command094": return 94;
        case "command095": return 95;
        case "command096": return 96;
        case "command097": return 97;
        case "command098": return 98;
        case "command099": return 99;
        default: return -1;
    }
}
