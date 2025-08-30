// See https://aka.ms/new-console-template for more information

const double x0 = 0.35;
const double x1 = 0.9;

const double y_out = 0.5;

var a = x0;
var b = x1;

var w11 = 0.1;
var w12 = 0.8;

var w21 = 0.4;
var w22 = 0.6;

var w31 = 0.3;
var w32 = 0.9;

var count = 0;

while (true)
{
    var z0 = w11 * a + w12 * b;
    var z1 = w21 * a + w22 * b;

    var y0 = F(z0);
    var y1 = F(z1);

    var z2 = w31 * y0 + w32 * y1;

    var y2 = F(z2);

    var c = C(y2);

    if (c < 0.0000001)
    {
        break;
    }

    var dc_dw31 = (y2 - y_out) * (y2 * (1 - y2)) * y0;
    var dc_dw32 = (y2 - y_out) * (y2 * (1 - y2)) * y1;

    var dc_dw11 = (y2 - y_out) * (y2 * (1 - y2)) * w31 * (y0 * (1 - y0)) * a;
    var dc_dw12 = (y2 - y_out) * (y2 * (1 - y2)) * w31 * (y0 * (1 - y0)) * b;

    var dc_dw21 = (y2 - y_out) * (y2 * (1 - y2)) * w32 * (y1 * (1 - y1)) * a;
    var dc_dw22 = (y2 - y_out) * (y2 * (1 - y2)) * w32 * (y1 * (1 - y1)) * b;

    w31 = w31 - dc_dw31;
    w32 = w32 - dc_dw32;

    w11 = w11 - dc_dw11;
    w12 = w12 - dc_dw12;

    w21 = w21 - dc_dw21;
    w22 = w22 - dc_dw22;

    count++;
}

Console.WriteLine("Hello, World!");

double F(double x)
{
    return 1.0 / (1 + Math.Pow(Math.E, -x));
}

static double C(double y2)
{
    return 1.0 / 2 * Math.Pow((y2 - y_out), 2);
}