// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public static partial class B2Recordings
    {
        // Recording trampolines: replace the user fcn pointer so hits are captured before dispatch
        internal static bool b2RecOverlapTrampoline(B2ShapeId id, ref B2RecQueryWriter w)
        {
            b2OverlapResultFcn userFcn = w.userFcn.overlapFcn;
            bool ret = userFcn(id, w.userContext);
            b2RecW_SHAPEID(ref w.buf, id);
            b2RecW_BOOL(ref w.buf, ret);
            w.hitCount++;
            return ret;
        }

        internal static float b2RecCastTrampoline<T>(B2ShapeId id, B2Vec2 point, B2Vec2 normal, float fraction,
            ref B2RecQueryWriter w) where T : class
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

        internal static bool b2RecPlaneTrampoline(B2ShapeId id, ref B2PlaneResult plane, ref B2RecQueryWriter w)
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
