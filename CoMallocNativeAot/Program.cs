using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.ComWrappers;

namespace CoMallocNativeAot
{
    unsafe class Program
    {
        [DllImport("ole32")]
        static extern void CoInitialize(IntPtr pvReserved);

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
            // This is required for NativeAOT,
            // since COM does not intialized by the compiler?
            CoInitialize(IntPtr.Zero);
            Console.WriteLine("No spy test");
            MemoryAllocationTest();
            Console.WriteLine("Spy test");
            ComWrappers.RegisterForMarshalling(new MallocSpyComWrapper());
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

        class MallocSpyComWrapper : ComWrappers
        {
            static ComInterfaceEntry* wrapperEntry;

            internal static Guid IMallocSpy_GUID = typeof(IMallocSpy).GUID;

            static MallocSpyComWrapper()
            {
                GetIUnknownImpl(out IntPtr fpQueryInteface, out IntPtr fpAddRef, out IntPtr fpRelease);

                var vtbl = new IMallocSpyVtbl()
                {
                    IUnknownImpl = new IUnknownVtbl()
                    {
                        QueryInterface = fpQueryInteface,
                        AddRef = fpAddRef,
                        Release = fpRelease
                    },
                    PreAlloc = &IMallocSpyProxy.PreAlloc,
                    PostAlloc = &IMallocSpyProxy.PostAlloc,
                    PreFree = &IMallocSpyProxy.PreFree,
                    PostFree = &IMallocSpyProxy.PostFree,
                    PreRealloc = &IMallocSpyProxy.PreRealloc,
                    PostRealloc = &IMallocSpyProxy.PostRealloc,
                    PreGetSize = &IMallocSpyProxy.PreGetSize,
                    PostGetSize = &IMallocSpyProxy.PostGetSize,
                    PreDidAlloc = &IMallocSpyProxy.PreDidAlloc,
                    PostDidAlloc = &IMallocSpyProxy.PostDidAlloc,
                    PreHeapMinimize = &IMallocSpyProxy.PreHeapMinimize,
                    PostHeapMinimize = &IMallocSpyProxy.PostHeapMinimize,
                };
                var vtblRaw = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IMallocSpyVtbl), sizeof(IMallocSpyVtbl));
                Marshal.StructureToPtr(vtbl, vtblRaw, false);

                var comInterfaceEntryMemory = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IMallocSpyVtbl), sizeof(ComInterfaceEntry));
                wrapperEntry = (ComInterfaceEntry*)comInterfaceEntryMemory.ToPointer();
                wrapperEntry->IID = IMallocSpy_GUID;
                wrapperEntry->Vtable = vtblRaw;

                //wrapperEntry = entry;
            }

            protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                // count = 0;
                // return null;
                count = 1;
                return wrapperEntry;
            }

            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                // Return NULL works,
                return null;
            }

            protected override void ReleaseObjects(System.Collections.IEnumerable objects)
            {
            }
        }

        public struct IUnknownVtbl
        {
            public IntPtr QueryInterface;
            public IntPtr AddRef;
            public IntPtr Release;
        }

        public unsafe struct IMallocSpyVtbl
        {
            public IUnknownVtbl IUnknownImpl;
            public delegate*<IntPtr, uint, uint> PreAlloc;
            internal delegate*<IntPtr, void*, void*> PostAlloc;
            internal delegate*<IntPtr, void*, bool, void*> PreFree;
            internal delegate*<IntPtr, bool, void> PostFree;
            internal delegate*<IntPtr, void*, uint, IntPtr*, bool, uint> PreRealloc;
            internal delegate*<IntPtr, void*, bool, void*> PostRealloc;
            internal delegate*<IntPtr, void*, bool, void*> PreGetSize;
            internal delegate*<IntPtr, uint, bool, uint> PostGetSize;
            internal delegate*<IntPtr, void*, bool, void*> PreDidAlloc;
            internal delegate*<IntPtr, void*, bool, int, int> PostDidAlloc;
            internal delegate*<IntPtr, void> PreHeapMinimize;
            internal delegate*<IntPtr, void> PostHeapMinimize;
        }

        internal class IMallocSpyProxy
        {
            public static uint PreAlloc(IntPtr thisPtr, uint cbRequest)
            {
                var inst = ComInterfaceDispatch.GetInstance<IMallocSpy>((ComInterfaceDispatch*)thisPtr);
                return inst.PreAlloc(cbRequest);
            }

            public static void* PostAlloc(IntPtr thisPtr, void* pActual)
            {
                var inst = ComInterfaceDispatch.GetInstance<IMallocSpy>((ComInterfaceDispatch*)thisPtr);
                return inst.PostAlloc(pActual);
            }

            public static void* PreFree(IntPtr thisPtr, void* pRequest, bool fSpyed)
            {
                var inst = ComInterfaceDispatch.GetInstance<IMallocSpy>((ComInterfaceDispatch*)thisPtr);
                return inst.PreFree(pRequest, fSpyed);
            }

            public static void PostFree(IntPtr thisPtr, bool fSpyed)
            {
                var inst = ComInterfaceDispatch.GetInstance<IMallocSpy>((ComInterfaceDispatch*)thisPtr);
                inst.PostFree(fSpyed);
            }

            public static uint PreRealloc(IntPtr thisPtr, void* pRequest, uint cbRequest, IntPtr* ppNewRequest, bool fSpyed)
            {
                var inst = ComInterfaceDispatch.GetInstance<IMallocSpy>((ComInterfaceDispatch*)thisPtr);
                return inst.PreRealloc(pRequest, cbRequest, ppNewRequest, fSpyed);
            }

            public static void* PostRealloc(IntPtr thisPtr, void* pActual, bool fSpyed)
            {
                var inst = ComInterfaceDispatch.GetInstance<IMallocSpy>((ComInterfaceDispatch*)thisPtr);
                return inst.PostRealloc(pActual, fSpyed);
            }

            public static void* PreGetSize(IntPtr thisPtr, void* pRequest, bool fSpyed)
            {
                var inst = ComInterfaceDispatch.GetInstance<IMallocSpy>((ComInterfaceDispatch*)thisPtr);
                return inst.PreGetSize(pRequest, fSpyed);
            }

            public static uint PostGetSize(IntPtr thisPtr, uint cbActual, bool fSpyed)
            {
                var inst = ComInterfaceDispatch.GetInstance<IMallocSpy>((ComInterfaceDispatch*)thisPtr);
                return inst.PostGetSize(cbActual, fSpyed);
            }

            public static void* PreDidAlloc(IntPtr thisPtr, void* pRequest, bool fSpyed)
            {
                var inst = ComInterfaceDispatch.GetInstance<IMallocSpy>((ComInterfaceDispatch*)thisPtr);
                return inst.PreDidAlloc(pRequest, fSpyed);
            }

            public static int PostDidAlloc(IntPtr thisPtr, void* pRequest, bool fSpyed, int fActual)
            {
                var inst = ComInterfaceDispatch.GetInstance<IMallocSpy>((ComInterfaceDispatch*)thisPtr);
                return inst.PostDidAlloc(pRequest, fSpyed, fActual);
            }

            public static void PreHeapMinimize(IntPtr thisPtr)
            {
                var inst = ComInterfaceDispatch.GetInstance<IMallocSpy>((ComInterfaceDispatch*)thisPtr);
                inst.PreHeapMinimize();
            }

            public static void PostHeapMinimize(IntPtr thisPtr)
            {
                var inst = ComInterfaceDispatch.GetInstance<IMallocSpy>((ComInterfaceDispatch*)thisPtr);
                inst.PostHeapMinimize();
            }
        }
    }
}
