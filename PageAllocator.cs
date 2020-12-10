using System;
using System.Collections.Generic;
using System.Text;

namespace PageAllocator
{
    unsafe static class PageAllocator
    {
        static byte[] Memory;
        static Descriptor[] Descriptors;
        static Dictionary<int, List<Descriptor>> NotFullPages = new Dictionary<int, List<Descriptor>>();
        const int PageCount = 100;
        const int PageSize = 4096;
        static int DescriptorsCount = 0;

        static public void Initialize()
        {
            Memory = new byte[PageCount * PageSize];
            Descriptors = new Descriptor[PageCount];
        }

        static public void* MemAlloc(int size)
        {
            if (size > PageSize / 2) return BigAlloc(size);
            else return SmallAlloc(size);
        }

        static void* BigAlloc(int size)
        {
            int pages;
            Descriptor des;
            for (pages = 1; pages * PageSize < size; pages++) { }
            if (NotFullPages.ContainsKey(pages * PageSize) && NotFullPages[pages * PageSize].Count != 0)
            {
                des = NotFullPages[pages * PageSize][0];
                byte* ret = des.Byte;
                des.FreeBlocks--;
                NotFullPages[pages * PageSize].RemoveAt(0);
                des.Byte = null;
                return ret;
            }
            if (DescriptorsCount + pages > PageCount) return FindBig(size);
            fixed (byte* addr = &Memory[DescriptorsCount * PageSize])
            {
                if (!NotFullPages.ContainsKey(pages * PageSize)) NotFullPages.Add(pages * PageSize, new List<Descriptor>());
                des = new Descriptor(pages * PageSize);
                Descriptors[DescriptorsCount - 1] = des;
                for (int i = 1; i < pages; i++)
                {
                    Descriptors[DescriptorsCount] = new Descriptor();
                }
                return addr;
            }
        }
        static void* FindBig(int size)
        {
            int thissize = int.MaxValue;
            int addrbyte = -1;
            Descriptor descriptor = null;
            for (int i = 0; i < DescriptorsCount; i++)
            {
                if (Descriptors[i].FreeBlocks > 0 && Descriptors[i].BlockSize >= size && Descriptors[i].BlockSize < thissize)
                {
                    thissize = Descriptors[i].BlockSize;
                    descriptor = Descriptors[i];
                    addrbyte = i * PageSize;
                }
            }
            if (descriptor == null) return null;
            descriptor.FreeBlocks--;
            NotFullPages[thissize].RemoveAt(NotFullPages[thissize].IndexOf(descriptor));
            fixed (byte* addr = &Memory[addrbyte])
            {
                return addr;
            }
        }
        static void* SmallAlloc(int size)
        {
            if (size > PageSize / 2) return FindBig(size);
            int blocksize;
            Int32* next;
            Descriptor des;
            for (blocksize = 4; blocksize < size; blocksize *= 2) { }
            if (NotFullPages.ContainsKey(blocksize) && NotFullPages[blocksize].Count != 0)
            {
                des = NotFullPages[blocksize][0];
                byte* ret = des.Byte;
                des.FreeBlocks--;
                if (des.FreeBlocks > 0)
                {
                    next = (Int32*)des.Byte;
                    des.Byte = (byte*)*next;
                }
                else
                {
                    NotFullPages[blocksize].RemoveAt(0);
                    des.Byte = null;
                }
                return ret;
            }
            if (DescriptorsCount == PageCount) return SmallAlloc(blocksize + 1);
            fixed (byte* addr = &Memory[DescriptorsCount * PageSize])
            {
                des = new Descriptor(addr, blocksize);
                Descriptors[DescriptorsCount - 1] = des;
                if (!NotFullPages.ContainsKey(blocksize)) NotFullPages.Add(blocksize, new List<Descriptor>());
                NotFullPages[blocksize].Insert(0, des);
                des.FreeBlocks--;
                next = (Int32*)des.Byte;
                des.Byte = (byte*)*next;
                return addr;
            }
        }
        static public void MemFree(void* Addr)
        {
            Descriptor del = DescriptorOfBlock(Addr);
            if (del == null) return;
            del.FreeBlocks++;
            if (del.Byte != null)
            {
                int nextfree = (int)del.Byte;
                del.Byte = (byte*)Addr;
                Int32* block = (Int32*)del.Byte;
                *block = nextfree;
            }
            else
            {
                NotFullPages[del.BlockSize].Insert(0, del);
                del.Byte = (byte*)Addr;
            }
        }
        static public void* MemReAlloc(void* Addr, int newsize)
        {
            if (Addr == null) return MemAlloc(newsize);
            Descriptor des = DescriptorOfBlock(Addr);
            if (des == null) return null;
            void* adr = MemAlloc(newsize);
            if (adr != null)
            {
                byte* oldbyte = (byte*)Addr;
                byte* newbyte = (byte*)adr;
                int block = des.BlockSize < newsize ? des.BlockSize : newsize;
                for (int i = 0; i < block; i++)
                {
                    *newbyte = *oldbyte;
                    oldbyte = (byte*)((int)oldbyte + 1);
                    newbyte = (byte*)((int)newbyte + 1);
                }
                MemFree(Addr);
            }
            return adr;
        }
        static Descriptor DescriptorOfBlock(void* Addr)
        {
            int pagefirstaddr;
            fixed (byte* firstaddr = &Memory[0])
            {
                pagefirstaddr = (int)firstaddr;
            }
            if ((int)Addr < pagefirstaddr) return null;
            for (int i = 0; i < PageCount; i++)
            {
                pagefirstaddr += PageSize;
                if ((int)Addr < pagefirstaddr) return Descriptors[i];
            }
            return null;
        }
        static public void Dump()
        {
            for (int i = 0; i < DescriptorsCount; i++)
            {
                Console.WriteLine("[Page - {0}, BlockSize - {1}, FreeBlocks - {2}]", i, Descriptors[i].BlockSize, Descriptors[i].FreeBlocks);
                for (int j = 0; j < PageSize; j++)
                {
                    Console.Write(Memory[i * PageSize + j]);
                    Console.Write(" ");
                }
                Console.Write("\n");
            }
        }
        unsafe class Descriptor
        {
            public Descriptor()
            {
                DescriptorsCount++;
            }
            public Descriptor(int size)
            {
                DescriptorsCount++;
                BlockSize = size;
            }
            public Descriptor(byte* byt, int size)
            {
                DescriptorsCount++;
                Byte = byt;
                BlockSize = size;
                FreeBlocks = PageSize / size;
                byte* thisbyt = byt;
                for (int i = 0; i < FreeBlocks; i++)
                {
                    Int32* block = (Int32*)thisbyt;
                    *block = (int)thisbyt + BlockSize;
                    thisbyt = (byte*)*block;
                }
            }
            public byte* Byte { get; set; } = null;
            public int BlockSize { get; } = -1;
            public int FreeBlocks { get; set; } = 0;
        }
    }
}
