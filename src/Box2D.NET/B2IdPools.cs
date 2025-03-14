﻿// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using static Box2D.NET.B2Arrays;

namespace Box2D.NET
{
    public static class B2IdPools
    {
        public static int b2GetIdCount(B2IdPool pool)
        {
            return pool.nextIndex - pool.freeArray.count;
        }

        public static int b2GetIdCapacity(B2IdPool pool)
        {
            return pool.nextIndex;
        }

        public static int b2GetIdBytes(B2IdPool pool)
        {
            return b2Array_ByteCount(ref pool.freeArray);
        }


        public static B2IdPool b2CreateIdPool()
        {
            B2IdPool pool = new B2IdPool();
            pool.freeArray = b2Array_Create<int>(32);
            return pool;
        }

        public static void b2DestroyIdPool(ref B2IdPool pool)
        {
            b2Array_Destroy(ref pool.freeArray);
            //*pool = ( b2IdPool ) {0}
            pool.Clear();
        }

        public static int b2AllocId(B2IdPool pool)
        {
            int count = pool.freeArray.count;
            if (count > 0)
            {
                int id = b2Array_Pop(ref pool.freeArray);
                return id;
            }

            int nextId = pool.nextIndex;
            pool.nextIndex += 1;
            return nextId;
        }

        public static void b2FreeId(B2IdPool pool, int id)
        {
            Debug.Assert(pool.nextIndex > 0);
            Debug.Assert(0 <= id && id < pool.nextIndex);

            if (id == pool.nextIndex)
            {
                pool.nextIndex -= 1;
                return;
            }

            b2Array_Push(ref pool.freeArray, id);
        }

#if B2_VALIDATE
        public static void b2ValidateFreeId(B2IdPool pool, int id)
        {
            int freeCount = pool.freeArray.count;
            for (int i = 0; i < freeCount; ++i)
            {
                if (pool.freeArray.data[i] == id)
                {
                    return;
                }
            }

            Debug.Assert(false);
        }

#else
        public static void b2ValidateFreeId(B2IdPool pool, int id)
        {
            B2Cores.B2_UNUSED(pool);
            B2Cores.B2_UNUSED(id);
        }

#endif
    }
}
