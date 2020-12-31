// FewaybofallfurhaWerenekecai.cpp : 此文件包含 "main" 函数。程序执行将在此处开始并结束。
//

#include <iostream>

typedef unsigned char u8;
typedef unsigned short u16;
typedef unsigned  u32;


#define REGISTER_MAIN           0xF0
#define REGISTER_SUB_REQUEST    0x00
#define REGISTER_SUB_ACK_OLD    0x01
#define REGISTER_SUB_ACK_NEW    0x02
#define REGISTER_DATA_LEN       8
#define ENCODE_DATA_LEN         8

#define  SVN_ENCODE_DATA_BITS_START_OFFSET  0x05
#define  RAND_RIGSTER_DATA_BITS_START_OFFSET  0x05

#define MCU_HANDLER_TIME_OUT_MSEC  100

#define SVN_RAND_MIN            50000
#define SVN_RAND_MAX            99999999

/* if(SVN<47479),NG; if(SVN>=47479),PASS */
#define SVN_PASS_MIN            47479
#define SVN_PASS_MAX            99999999

#define ROTATE_LEFT(x, s, n)    ((x) << (n)) | ((x) >> ((s) - (n)))
#define ROTATE_RIGHT(x, s, n)   ((x) >> (n)) | ((x) << ((s) - (n)))


u32 DataDecode(u8* Data)
{
	u8 buf[9];
	u8 i = 1;
	u32 SvnVersion = 0;

	buf[7] = Data[0];
	buf[5] = Data[1];
	buf[3] = Data[2];
	buf[1] = Data[3];
	buf[2] = Data[4];
	buf[4] = Data[5];
	buf[6] = Data[6];
	buf[8] = Data[7];

	for (i = 1; i < 9; i++)
	{
		buf[i] = (buf[i] / (i + 7)) - i;
		if (buf[i] > 9)
		{
			return 0;
		}
	}

	for (i = 1; i < 9; i++)
	{
		SvnVersion = SvnVersion * 10 + buf[9 - i];
	}

	return SvnVersion;
}

int main()
{
	u8 DecodeBuf[REGISTER_DATA_LEN] = { 0x4C, 0x42, 0x23, 0x05, 0x8D, 0x12, 0xD4, 0x96 };

	u32 crc = 0;
	u32 n = 0;
	u32 i = 0;

	crc = 1704837083;

	for (i = 0; i < 8; i++)
	{
		n = (crc >> ((7 - i) * 4)) % 8;     /* 计算循环右移的值 */
		DecodeBuf[i] = ROTATE_RIGHT(DecodeBuf[i], 8, (crc >> ((7 - i) * 4)) % 8);
	}

	auto svn = DataDecode(DecodeBuf);
}

// 运行程序: Ctrl + F5 或调试 >“开始执行(不调试)”菜单
// 调试程序: F5 或调试 >“开始调试”菜单

// 入门使用技巧: 
//   1. 使用解决方案资源管理器窗口添加/管理文件
//   2. 使用团队资源管理器窗口连接到源代码管理
//   3. 使用输出窗口查看生成输出和其他消息
//   4. 使用错误列表窗口查看错误
//   5. 转到“项目”>“添加新项”以创建新的代码文件，或转到“项目”>“添加现有项”以将现有代码文件添加到项目
//   6. 将来，若要再次打开此项目，请转到“文件”>“打开”>“项目”并选择 .sln 文件
