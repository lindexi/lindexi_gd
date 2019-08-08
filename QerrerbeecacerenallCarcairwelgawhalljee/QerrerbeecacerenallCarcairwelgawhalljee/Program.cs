using System;
using System.Collections.Generic;

namespace QerrerbeecacerenallCarcairwelgawhalljee
{
    class Program
    {
        static void Main(string[] args)
        {
            var linkedList = new LinkedList<int>(new int[]
            {
                1, 2, 5, 7, 23,
            })
            {
            };


            LinkedListNode<int> listNode = linkedList.First;
            Sort(listNode);
        }

        private static void Sort(LinkedListNode<int> listNode)
        {
            var list = new List<int>();

            while (listNode != null)
            {
                list.Add(listNode.Value);
                listNode = listNode.Next;
            }

            list.Sort();

            foreach (var temp in list)
            {
                Console.WriteLine(temp);
            }
        }
    }
}