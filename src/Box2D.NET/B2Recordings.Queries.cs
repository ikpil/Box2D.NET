// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Threading;

namespace Box2D.NET
{
    public static partial class B2Recordings
    {
        // Bound cached scratch memory independently from the number of active queries.
        private const int B2_MAX_RETAINED_QUERY_BUFFER_CAPACITY = 64 * 1024;
        // Cache the method-group delegate even when compiled with the netstandard C# version.
        private static readonly Func<B2ObjectPool<B2RecQueryWriter>> _queryWriterPoolFactory = b2CreateQueryWriterPool;

        private static B2ObjectPool<B2RecQueryWriter> b2CreateQueryWriterPool()
        {
            return new B2ObjectPool<B2RecQueryWriter>(() => new B2RecQueryWriter(), b2ResetQueryWriter);
        }

        private static bool b2ResetQueryWriter(B2RecQueryWriter writer)
        {
            // Keep ordinary byte arrays for the next query, but never retain user objects
            // or let one unusually large query pin its buffer for the world's lifetime.
            writer.userFcn = default;
            writer.userContext = null;
            writer.buf.size = 0;
            if (writer.buf.capacity > B2_MAX_RETAINED_QUERY_BUFFER_CAPACITY)
            {
                b2RecBufFree(ref writer.buf);
            }
            writer.countOffset = 0;
            writer.hitCount = 0;
            return true;
        }

        // Rent only while recording; a null lease makes using a no-op for ordinary queries.
        internal static B2RecQueryWriter b2RentQueryWriterIfRecording(B2World world)
        {
            B2Recording recording = world.recording;
            if (recording == null)
            {
                return null;
            }

            B2ObjectPool<B2RecQueryWriter> pool = LazyInitializer.EnsureInitialized(
                ref world.queryWriterPool, ref world.queryWriterPoolLock, _queryWriterPoolFactory);
            B2RecQueryWriter writer = pool.Get();
            writer.owner = pool;
            return writer;
        }

        // Recording trampolines: replace the user fcn pointer so hits are captured before dispatch
        internal static bool b2RecOverlapTrampoline(B2ShapeId id, B2RecQueryWriter w)
        {
            b2OverlapResultFcn userFcn = w.userFcn.overlapFcn;
            bool ret = userFcn(id, w.userContext);
            b2RecW_SHAPEID(ref w.buf, id);
            b2RecW_BOOL(ref w.buf, ret);
            w.hitCount++;
            return ret;
        }

        internal static float b2RecCastTrampoline<T>(B2ShapeId id, B2Vec2 point, B2Vec2 normal, float fraction,
            B2RecQueryWriter w) where T : class
        {
            b2CastResultFcn<T> userFcn = (b2CastResultFcn<T>)w.userFcn.castFcn;
            T userContext = (T)w.userContext;
            float ret = userFcn(id, point, normal, fraction, ref userContext);
            w.userContext = userContext;
            b2RecW_SHAPEID(ref w.buf, id);
            b2RecW_VEC2(ref w.buf, point);
            b2RecW_VEC2(ref w.buf, normal);
            b2RecW_F32(ref w.buf, fraction);
            b2RecW_F32(ref w.buf, ret);
            w.hitCount++;
            return ret;
        }

        internal static bool b2RecPlaneTrampoline(B2ShapeId id, ref B2PlaneResult plane, B2RecQueryWriter w)
        {
            b2PlaneResultFcn userFcn = w.userFcn.planeFcn;
            bool ret = userFcn(id, ref plane, w.userContext);
            b2RecW_SHAPEID(ref w.buf, id);
            b2RecW_PLANERESULT(ref w.buf, plane);
            b2RecW_BOOL(ref w.buf, ret);
            w.hitCount++;
            return ret;
        }
    }
}
