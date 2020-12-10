using System;
using System.Collections.Generic;

namespace PageAllocator
{
    class Program
    {
        unsafe static void Main(string[] args)
        {
            /*PageAllocator.Initialize();
            int* a1 = (int*)PageAllocator.MemAlloc(12);
            *a1 = 9999;
            int* b1 = (int*)PageAllocator.MemAlloc(5);
            *b1 = 99199;
            int* a2 = (int*)PageAllocator.MemAlloc(12);
            *a2 = 99;
            int* b2 = (int*)PageAllocator.MemAlloc(5);
            *b2 = 199;
            b1 = (int*)PageAllocator.MemReAlloc(b1, 32);
            PageAllocator.Dump();
            PageAllocator.MemFree(a1);
            PageAllocator.Dump();*/
            List<int> a = new List<int>();
            a.Add(1);
            a.Add(2);
            a.Add(3);
            a.RemoveAt(0);
            foreach (var item in a)
            {
                Console.WriteLine(item);
            }
            a.Insert(0, 1);
            foreach (var item in a)
            {
                Console.WriteLine(item);
            }
        }
    }
}
