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

u32 RegisterCrc32(u8* Buffer, u8 Length)
{
	unsigned int i = 0, j = 0; // byte counter, bit counter
	unsigned int byte = 0;
	unsigned int poly = 0x04C11DB7;
	unsigned int crc = 0xFFFFFFFF;
	unsigned int* message = (unsigned int*)Buffer;
	unsigned int msgsize = Length / 4;

	i = 0;
	byte = 0x5BE87C00;  /* Firt Data Head */
	for (j = 0; j < 32; j++)
	{
		if ((int)(crc ^ byte) < 0)
		{
			crc = (crc << 1) ^ poly;
		}
		else
		{
			crc = crc << 1;
		}
		byte = byte << 1;    // Ready next msg bit.
	}

	for (i = 0; i < msgsize; i++)
	{
		byte = message[i];
		for (j = 0; j < 32; j++)
		{
			if ((int)(crc ^ byte) < 0)
			{
				crc = (crc << 1) ^ poly;
			}
			else
			{
				crc = crc << 1;
			}
			byte = byte << 1;    // Ready next msg bit.
		}
	}

	byte = 0x75D63F00;  /* End  Data */
	for (j = 0; j < 32; j++)
	{
		if ((int)(crc ^ byte) < 0)
		{
			crc = (crc << 1) ^ poly;
		}
		else
		{
			crc = crc << 1;
		}
		byte = byte << 1;    // Ready next msg bit.
	}

	return crc;
}

int main()
{
	u8 Register[REGISTER_DATA_LEN] = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };

    std::cout << RegisterCrc32(Register, REGISTER_DATA_LEN);
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
