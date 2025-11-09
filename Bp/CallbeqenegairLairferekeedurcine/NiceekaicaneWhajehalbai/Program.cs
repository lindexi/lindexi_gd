// See https://aka.ms/new-console-template for more information

const double x0 = 0.35;
const double x1 = 0.9;

const double y_out = 0.5;

var a = x0;
var b = x1;

var l1_w11 = 0.1;
var l1_w12 = 0.8;

var l1_w21 = 0.4;
var l1_w22 = 0.6;

var l2_w31 = 0.3;
var l2_w32 = 0.9;

var l2_w41 = 0.2;
var l2_w42 = 0.7;

var l3_w51 = 0.5;
var l3_w52 = 0.4;

var count = 0;

while (true)
{
    var l1_z0 = l1_w11 * a + l1_w12 * b;
    var l1_z1 = l1_w21 * a + l1_w22 * b;

    var l1_y0 = F(l1_z0);
    var l1_y1 = F(l1_z1);

    var l2_z2 = l2_w31 * l1_y0 + l2_w32 * l1_y1;
    var l2_z3 = l2_w41 * l1_y0 + l2_w42 * l1_y1;

    var l2_y2 = F(l2_z2);
    var l2_y3 = F(l2_z3);

    var l3_z4 = l3_w51 * l2_y2 + l3_w52 * l2_y3;
    var l3_y4 = F(l3_z4);

    var c = C(l3_y4);

    if (c < 0.0000001)
    {
        break;
    }

    var dc_dy4 = (l3_y4 - y_out);
    var dy4_dz4 = DF(l3_y4);

    var dz4_dw51 = l2_y2;
    var dz4_dw52 = l2_y3;

    var dc_dw51 = dc_dy4 * dy4_dz4 * dz4_dw51;
    var dc_dw52 = dc_dy4 * dy4_dz4 * dz4_dw52;

    var dz4_dy2 = l3_w51;
    var dy2_dz2 = DF(l2_y2);
    var dz2_dw31 = l1_y0;
    var dc_dw31 = dc_dy4 * dy4_dz4 * dz4_dy2 * dy2_dz2 * dz2_dw31;

    var dz2_dw32 = l1_y1;
    var dc_dw32 = dc_dy4 * dy4_dz4 * dz4_dy2 * dy2_dz2 * dz2_dw32;

    var dz4_dy3 = l3_w52;
    var dy3_dz3 = DF(l2_y3);
    var dz3_dw41 = l1_y0;
    var dc_dw41 = dc_dy4 * dy4_dz4 * dz4_dy3 * dy3_dz3 * dz3_dw41;
    var dz3_dw42 = l1_y1;
    var dc_dw42 = dc_dy4 * dy4_dz4 * dz4_dy3 * dy3_dz3 * dz3_dw42;

    var dz2_dy0 = l2_w32;
    var dy0_dz0 = DF(l1_y0);
    var dz_dw11 = a;
    var dc_dw11 = dc_dy4 * dy4_dz4 * dz4_dy2 * dy2_dz2 * dz2_dy0 * dy0_dz0 * dz_dw11;
    var dz_dw12 = b;
    var dc_dw12 = dc_dy4 * dy4_dz4 * dz4_dy2 * dy2_dz2 * dz2_dy0 * dy0_dz0 * dz_dw12;

    var dz3_dy1 = l2_w42;
    var dy1_dz1 = DF(l1_y1);
    var dz1_dw21 = a;
    var dc_dw21 = dc_dy4 * dy4_dz4 * dz4_dy3 * dy3_dz3 * dz3_dy1 * dy1_dz1 * dz1_dw21;
    var dz1_dw22 = b;
    var dc_dw22 = dc_dy4 * dy4_dz4 * dz4_dy3 * dy3_dz3 * dz3_dy1 * dy1_dz1 * dz1_dw22;

    l3_w51 = l3_w51 - dc_dw51;
    l3_w52 = l3_w52 - dc_dw52;

    l2_w31 = l2_w31 - dc_dw31;
    l2_w32 = l2_w32 - dc_dw32;
    l2_w41 = l2_w41 - dc_dw41;
    l2_w42 = l2_w42 - dc_dw42;

    l1_w11 = l1_w11 - dc_dw11;
    l1_w12 = l1_w12 - dc_dw12;
    l1_w21 = l1_w21 - dc_dw21;
    l1_w22 = l1_w22 - dc_dw22;

    count++;
}

Console.WriteLine("Hello, World!");

double F(double x)
{
    return 1.0 / (1 + Math.Exp(-x));
}

double DF(double y)
{
    return y * (1 - y);
}

static double C(double y2)
{
    return 1.0 / 2 * Math.Pow((y2 - y_out), 2);
}