// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Atomics;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Profiling;

namespace Box2D.NET
{
    public static class B2Buffers
    {
        private static b2AllocFcn b2_allocFcn;
        private static b2FreeFcn b2_freeFcn;
        private static B2AtomicInt b2_byteCount;

        public static T[] b2GrowAlloc<T>(T[] oldMem, int oldSize, int newSize) where T : new()
        {
            B2_ASSERT(newSize > oldSize);
            T[] newMem = new T[newSize];
            if (oldSize > 0)
            {
                Array.Copy(oldMem, newMem, oldSize);
                b2Free(oldMem, oldSize);
            }

            if (!typeof(T).IsValueType)
            {
                for (int i = oldSize; i < newSize; ++i)
                {
                    newMem[i] = new T();
                }
            }

            return newMem;
        }


        // public static void memset<T>(Span<T> array, T value, int count)
        // {
        //     array.Slice(0, count).Fill(value);
        // }

        // public static void memcpy<T>(Span<T> dst, Span<T> src, int count)
        // {
        //     src.Slice(0, count).CopyTo(dst);
        // }
        //
        // public static void memcpy<T>(Span<T> dst, Span<T> src)
        // {
        //     src.CopyTo(dst);
        // }


        /// @return the total bytes allocated by Box2D
        public static int b2GetByteCount()
        {
            return b2AtomicLoadInt(ref b2_byteCount);
        }

        /// This allows the user to override the allocation functions. These should be
        /// set during application startup.
        public static void b2SetAllocator(b2AllocFcn allocFcn, b2FreeFcn freeFcn)
        {
            b2_allocFcn = allocFcn;
            b2_freeFcn = freeFcn;
        }

        public static T[] b2Alloc<T>(int size) where T : new()
        {
            if (size == 0)
            {
                return null;
            }

            T[] ptr = null;
            if (typeof(T).IsValueType)
            {
                ptr = new T[size];
            }
            else
            {
                ptr = new T[size];
                for (int i = 0; i < size; i++)
                {
                    ptr[i] = new T();
                }
            }

            // This could cause some sharing issues, however Box2D rarely calls b2Alloc.
            b2AtomicFetchAddInt(ref b2_byteCount, size);

            // Allocation must be a multiple of 32 or risk a seg fault
            // https://en.cppreference.com/w/c/memory/aligned_alloc
            int size32 = ((size - 1) | 0x1F) + 1;

            // if (b2_allocFcn != null)
            // {
            //     T[] ptr = b2_allocFcn(size32, B2_ALIGNMENT);
            //     b2TracyCAlloc(ptr, size);
            //
            //     return ptr;
            // }

            b2TracyCAlloc(ptr, size);

            return ptr;
        }

        public static void b2Free<T>(T[] mem, int size)
        {
            if (mem == null)
            {
                return;
            }

            b2TracyCFree(mem);

            // if (b2_freeFcn != null)
            // {
            //     b2_freeFcn(mem);
            // }
            // else
            // {
            // }

            b2AtomicFetchAddInt(ref b2_byteCount, -size);
        }

        public static void b2Free<T>(T mem, int size)
        {
            if (mem == null)
            {
                return;
            }

            b2TracyCFree(mem);

            // if (b2_freeFcn != null)
            // {
            //     b2_freeFcn(mem);
            // }
            // else
            // {
            // }

            b2AtomicFetchAddInt(ref b2_byteCount, -size);
        }
    }
}