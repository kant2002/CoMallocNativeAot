using System;
using System.Runtime.InteropServices;

namespace CoMallocNativeAot
{
    unsafe class Program
    {
        [DllImport("ole32")]
        static extern void* CoTaskMemAlloc(uint size);

        [DllImport("ole32")]
        static extern void* CoTaskMemFree(void* data);

        [DllImport("ole32")]
        static extern int CoRegisterMallocSpy(IMallocSpy mallocSpy);

        [DllImport("ole32")]
        static extern int CoRevokeMallocSpy();

        static void Main()
        {
            Console.WriteLine("No spy test");
            MemoryAllocationTest();
            Console.WriteLine("Spy test");
            MemoryAllocationTestWithSpy();
            Console.WriteLine("Done");
        }

        static void MemoryAllocationTestWithSpy()
        {
            var spy = new Spy();
            var hr = CoRegisterMallocSpy(spy);
            try
            {
                Console.WriteLine("Status code for call to CoRegisterMallocSpy: {0}", hr);
                if (hr != 0)
                {
                    return;
                }

                MemoryAllocationTest();
            }
            finally
            {
                hr = CoRevokeMallocSpy();
                Console.WriteLine("Status code for call to CoRevokeMallocSpy: {0}", hr);
            }
        }
        static void MemoryAllocationTest()
        {
            var ptr = CoTaskMemAlloc(128);
            if (ptr == null)
            {
                Console.WriteLine("No memory allocated");
                return;
            }

            Console.WriteLine("Allocation ok!");
            CoTaskMemFree(ptr);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000001d-0000-0000-C000-000000000046")]
        public interface IMallocSpy
        {
            [PreserveSig]
            uint PreAlloc(uint cbRequest);
            [PreserveSig]
            void* PostAlloc(void* pActual);
            [PreserveSig]
            void* PreFree(void* pRequest, bool fSpyed);
            [PreserveSig]
            void PostFree(bool fSpyed);
            [PreserveSig]
            uint PreRealloc(void* pRequest, uint cbRequest, IntPtr* ppNewRequest, bool fSpyed);
            [PreserveSig]
            void* PostRealloc(void* pActual, bool fSpyed);
            [PreserveSig]
            void* PreGetSize(void* pRequest, bool fSpyed);
            [PreserveSig]
            uint PostGetSize(uint cbActual, bool fSpyed);
            [PreserveSig]
            void* PreDidAlloc(void* pRequest, bool fSpyed);
            [PreserveSig]
            int PostDidAlloc(void* pRequest, bool fSpyed, int fActual);
            [PreserveSig]
            void PreHeapMinimize();
            [PreserveSig]
            void PostHeapMinimize();
        }

        public class Spy : IMallocSpy
        {
            public uint PreAlloc(uint cbRequest)
            {
                Console.WriteLine("PreAlloc({0})", cbRequest);
                return cbRequest;
            }
            public void* PostAlloc(void* pActual)
            {
                Console.WriteLine("PostAlloc(0x{0:X2})", (IntPtr)pActual);
                return pActual;
            }
            public void* PreFree(void* pRequest, bool fSpyed)
            {
                Console.WriteLine("PreFree(0x{0:X2},{1})", (IntPtr)pRequest, fSpyed);
                return pRequest;
            }
            public void PostFree(bool fSpyed)
            {
                Console.WriteLine("PostFree({0})", fSpyed);
            }
            public uint PreRealloc(void* pRequest, uint cbRequest, IntPtr* ppNewRequest, bool fSpyed)
            {
                return cbRequest;
            }
            public void* PostRealloc(void* pActual, bool fSpyed)
            {
                return pActual;
            }
            public void* PreGetSize(void* pRequest, bool fSpyed)
            {
                return pRequest;
            }
            public uint PostGetSize(uint cbActual, bool fSpyed)
            {
                return cbActual;
            }
            public void* PreDidAlloc(void* pRequest, bool fSpyed)
            {
                return pRequest;
            }
            public int PostDidAlloc(void* pRequest, bool fSpyed, int fActual)
            {
                return fActual;
            }
            public void PreHeapMinimize()
            {
            }
            
            public void PostHeapMinimize()
            {
            }
        }
    }
}
