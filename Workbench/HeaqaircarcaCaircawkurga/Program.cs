// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Runtime.CompilerServices;

var x1 = 0.35;
var x2 = 0.9;

const double y0 = 0.5;

var w31 = 0.1;
var w32 = 0.8;
var w41 = 0.4;
var w42 = 0.6;

var w53 = 0.3;
var w54 = 0.9;

while (true)
{
    //       z3
    // z1 =[    ]
    //       z4

    var z3 = w31 * x1 + w32 * x2;
    var z4 = w41 * x1 + w42 * x2;

    //       y3
    // y1 =[    ]
    //       y4

    var y3 = F(z3);
    var y4 = F(z4);

    var z2 = w53 * y3 + w54 * y4;
    var y2 = F(z2);

    var c = C(y2);

    if (c < 0.0001)
    {
        break;
    }

    var y5 = y2;
    var z5 = z2;

    //y2 = 0.690;
    //y3 = 0.68;

    var dc_dw53 = (y2 - y0) * (y5 * (1 - y5)) * y3;
    var dc_dw54 = (y2 - y0) * (y5 * (1 - y5)) * y4;
    var dc_dw31 = (y2 - y0) * (y5 * (1 - y5)) * w53 * (y3 * (1 - y3)) * x1;

    w31 = w31 - dc_dw31;
    // 0.099070928839190345
    // 0.09661944
    // dc_dw31 = 0.1 - 0.09661944 = 0.0033805600000000047
}


_ = x1;

Console.WriteLine("Hello, World!");
Console.ReadLine();

double F(double x)
{
    return 1.0 / (1 + Math.Pow(Math.E, -x));
}

static double C(double y2)
{
    return 1.0 / 2 * Math.Pow((y2 - y0), 2);
}