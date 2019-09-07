// Whwlhebenalyujaii.cpp : 此文件包含 "main" 函数。程序执行将在此处开始并结束。
//

#include <iostream>
#include "stdlib.h"

struct LinkedListNode
{
	LinkedListNode* Next;
	LinkedListNode* Previous;
	int Value;
} typedef LinkedListNode;

void Sort(LinkedListNode* list)
{
	if (list->Next != NULL && list->Next->Next != NULL)
	{
		LinkedListNode* pre, * cur, * next;
		LinkedListNode* head = list;
		LinkedListNode* end = NULL;
		//从链表头开始将较大值往后沉
		while (head->Next != end)
		{
			for (pre = head,
				cur = pre->Next,
				next = cur->Next;

				next != end;

				pre = pre->Next,
				cur = cur->Next,
				next = next->Next)
			{
				//相邻的节点比较
				if (cur->Value > next->Value)
				{
					cur->Next = next->Next;
					pre->Next = next;
					next->Next = cur;
					LinkedListNode* temp = next;
					next = cur;
					cur = temp;
				}
			}
			end = cur;
		}

		cur = list;
		while (cur != NULL)
		{
			printf("%d ", cur->Value);
			cur = cur->Next;
		}
		printf("\n");
	}
}

int main()
{
	LinkedListNode* head = (LinkedListNode*)malloc(sizeof(LinkedListNode));
	head->Value = 1;

	LinkedListNode* previous = head;

	LinkedListNode* next = (LinkedListNode*)malloc(sizeof(LinkedListNode));
	head->Next = next;
	next->Previous = previous;
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
