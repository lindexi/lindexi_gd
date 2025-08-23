// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Runtime.CompilerServices;

var z21 = F(0.755);
var z22 = F(0.68);

var z2 = 0.3* z21 + 0.9* z22;

var y2 = F(z2);
Debug.Assert(Math.Abs(y2 - 0.69) < 0.1);
var y0 = 0.5;

var C = 1.0 / 2 * (Math.Pow((y2 - y0), 2));
Debug.Assert(Math.Abs(C - 0.018) < 0.1);

var t = (0.690 - 0.5) * 0.690 * (1 - 0.690) * 0.663;
t = (0.690 - 0.5) * 0.690 * (1 - 0.690) * 0.68;

var x1 = 0.35;
var x2 = 0.9;
var w53 = 0.3;
var w54 = 0.9;
var w31 = 0.1;
var w32 = 0.8;
var z3 = w31 * x1 + w32 * x2;
var y3 = 0.680;

var dC_dw53 = (y2 - y0) * F(z2) * (1 - F(z2)) * y3;
// 0.02763
// 0.02766316507639054

t = (y2 - y0) * w54 * y2 * (1 - y2) * F(z3) * (1 - F(z3)) * x2;
var nw31 = w31 - t;
// 		nw31	0.0928328796166112	double
t = (y2 - y0)  * y2 * (1 - y2) * w53 * F(z3) * (1 - F(z3)) * x1;
var dc_dw31 = (y2 - y0) * F(z2) * (1 - F(z2)) * w53 * F(z3) * (1 - F(z3)) * x1;
nw31 = w31 - dc_dw31;
// 		nw31	0.099070928839190345	double






_ = z21;



Console.WriteLine("Hello, World!");
Console.ReadLine();

double F(double x)
{
    return 1.0 / (1 + Math.Pow(Math.E, -x));
}
