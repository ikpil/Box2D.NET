// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Box2D.NET.Primitives;
using static Box2D.NET.array;

namespace Box2D.NET
{
    public static class id_pool
    {
        public static int b2GetIdCount(b2IdPool pool)
        {
            return pool.nextIndex - pool.freeArray.count;
        }

        public static int b2GetIdCapacity(b2IdPool pool)
        {
            return pool.nextIndex;
        }

        public static int b2GetIdBytes(b2IdPool pool)
        {
            return b2Array_ByteCount(ref pool.freeArray);
        }


        public static b2IdPool b2CreateIdPool()
        {
            b2IdPool pool = new b2IdPool();
            pool.freeArray = b2Array_Create<int>(32);
            return pool;
        }

        public static void b2DestroyIdPool(ref b2IdPool pool)
        {
            b2Array_Destroy(ref pool.freeArray);
            pool = new b2IdPool(); // TODO: @ikpil check pool
        }

        public static int b2AllocId(b2IdPool pool)
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

        public static void b2FreeId(b2IdPool pool, int id)
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
        public static void b2ValidateFreeId(b2IdPool pool, int id)
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
        public static void b2ValidateFreeId(b2IdPool pool, int id)
        {
            B2_UNUSED(pool);
            B2_UNUSED(id);
        }

#endif
    }
}