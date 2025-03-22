﻿// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;
using static Box2D.NET.B2Constants;

namespace Box2D.NET
{
    public class B2RuntimeValidator
    {
        public static readonly B2RuntimeValidator Shared = new B2RuntimeValidator();

        private B2RuntimeValidator()
        {
        }

        public int ThrowIfSafeRuntimePlatform()
        {
            // 
            CheckSizeSeries();

            // check fixed array series
            CheckFixedArraySeries();

            // check union series
            CheckUnionSeries();

            CheckB2FloatW();

            CheckB2Simplex();

            return 0;
        }

        private float TestFloat()
        {
            long ticks = DateTime.UtcNow.Ticks;
            return (ticks % 100000) / 100000f;
        }

        private B2Vec2 TestVec2()
        {
            var random = new B2Vec2();
            random.X = TestFloat();
            random.Y = random.X + 1.0f;
            return random;
        }

        private void ThrowIf(bool condition, string message)
        {
            if (condition)
                return;

            throw new NotSupportedException(message);
        }

        private void CheckSizeSeries()
        {
            ThrowIf(B2FixedArray8<int>.Size == B2_MAX_POLYGON_VERTICES, "");
            ThrowIf(B2FixedArray8<B2Vec2>.Size == B2_MAX_POLYGON_VERTICES, "");
        }

        private void CheckFixedArraySeries()
        {
            var array1 = new B2FixedArray1<B2Vec2>();
            var array2 = new B2FixedArray2<B2Vec2>();
            var array3 = new B2FixedArray3<B2Vec2>();
            var array4 = new B2FixedArray4<B2Vec2>();

            //
            var array7 = new B2FixedArray7<B2Vec2>();
            var array8 = new B2FixedArray8<B2Vec2>();
            var array11 = new B2FixedArray11<B2Vec2>();
            var array12 = new B2FixedArray12<B2Vec2>();

            //
            var array16 = new B2FixedArray16<B2Vec2>();
            var array64 = new B2FixedArray64<B2Vec2>();
            var array1024 = new B2FixedArray1024<B2Vec2>();

            ThrowIfInvalidFixedArray(array1.AsSpan(), TestVec2, "");
            ThrowIfInvalidFixedArray(array2.AsSpan(), TestVec2, "");
            ThrowIfInvalidFixedArray(array3.AsSpan(), TestVec2, "");
            ThrowIfInvalidFixedArray(array4.AsSpan(), TestVec2, "");

            ThrowIfInvalidFixedArray(array7.AsSpan(), TestVec2, "");
            ThrowIfInvalidFixedArray(array8.AsSpan(), TestVec2, "");
            ThrowIfInvalidFixedArray(array11.AsSpan(), TestVec2, "");
            ThrowIfInvalidFixedArray(array12.AsSpan(), TestVec2, "");

            ThrowIfInvalidFixedArray(array16.AsSpan(), TestVec2, "");
            ThrowIfInvalidFixedArray(array64.AsSpan(), TestVec2, "");
            ThrowIfInvalidFixedArray(array1024.AsSpan(), TestVec2, "");
        }

        private void ThrowIfInvalidFixedArray<T>(Span<T> span, Func<T> randomValue, string message)
        {
            // original
            var original = new T[span.Length];
            for (int i = 0; i < span.Length; ++i)
            {
                original[i] = randomValue.Invoke();
            }

            // span
            for (int i = 0; i < span.Length; ++i)
            {
                span[i] = original[i];
            }


            // check
            for (int i = 0; i < span.Length; ++i)
            {
                ThrowIf(original[i].Equals(span[i]), message);
            }
        }

        private void CheckUnionSeries()
        {
            // joint union
            {
                int unionSize = Marshal.SizeOf<B2JointUnion>();

                int distanceJointSize = Marshal.SizeOf<B2DistanceJoint>();
                int motorJointSize = Marshal.SizeOf<B2MotorJoint>();
                int mouseJointSize = Marshal.SizeOf<B2MouseJoint>();
                int revoluteJointSize = Marshal.SizeOf<B2RevoluteJoint>();
                int prismaticJointSize = Marshal.SizeOf<B2PrismaticJoint>();
                int weldJointSize = Marshal.SizeOf<B2WeldJoint>();
                int wheelJointSize = Marshal.SizeOf<B2WheelJoint>();

                int maxSize = 0;
                maxSize = Math.Max(maxSize, distanceJointSize);
                maxSize = Math.Max(maxSize, motorJointSize);
                maxSize = Math.Max(maxSize, mouseJointSize);
                maxSize = Math.Max(maxSize, revoluteJointSize);
                maxSize = Math.Max(maxSize, prismaticJointSize);
                maxSize = Math.Max(maxSize, weldJointSize);
                maxSize = Math.Max(maxSize, wheelJointSize);

                ThrowIf(unionSize == maxSize, "");
            }

            // shape union
            {
                int unionSize = Marshal.SizeOf<B2ShapeUnion>();

                int capsuleSize = Marshal.SizeOf<B2Capsule>();
                int circleSize = Marshal.SizeOf<B2Circle>();
                int polygonSize = Marshal.SizeOf<B2Polygon>();
                int segmentSize = Marshal.SizeOf<B2Segment>();
                int chainSegmentSize = Marshal.SizeOf<B2ChainSegment>();

                int maxSize = 0;
                maxSize = Math.Max(maxSize, capsuleSize);
                maxSize = Math.Max(maxSize, circleSize);
                maxSize = Math.Max(maxSize, polygonSize);
                maxSize = Math.Max(maxSize, segmentSize);
                maxSize = Math.Max(maxSize, chainSegmentSize);

                ThrowIf(unionSize == maxSize, "");
            }

            // B2TreeNodeConnectionUnion
            {
                int unionSize = Marshal.SizeOf<B2TreeNodeConnectionUnion>();

                int parentSize = Marshal.SizeOf<int>();
                int nextSize = Marshal.SizeOf<int>();

                int maxSize = 0;
                maxSize = Math.Max(maxSize, parentSize);
                maxSize = Math.Max(maxSize, nextSize);

                ThrowIf(unionSize == maxSize, "");
            }

            // B2TreeNodeDataUnion
            {
                var unionSize = Marshal.SizeOf<B2TreeNodeDataUnion>();
                int child2Size = Marshal.SizeOf<int>();
                int userDataSize = Marshal.SizeOf<int>();

                int maxSize = 0;
                maxSize = Math.Max(maxSize, child2Size);
                maxSize = Math.Max(maxSize, userDataSize);

                ThrowIf(unionSize == maxSize, "");
            }
        }

        private void CheckB2FloatW()
        {
            var temp = new B2FloatW();
            temp.X = TestFloat();
            temp.Y = temp.X + 1;
            temp.Z = temp.Y + 1;
            temp.W = temp.Z + 1;

            ThrowIf(temp.X.Equals(temp[0]), "");
            ThrowIf(temp.Y.Equals(temp[1]), "");
            ThrowIf(temp.Z.Equals(temp[2]), "");
            ThrowIf(temp.W.Equals(temp[3]), "");
        }

        private void CheckB2Simplex()
        {
            var temp = new B2Simplex();

            // v1
            temp.v1.wA = TestVec2();
            temp.v1.wB = temp.v1.wA + TestVec2();
            temp.v1.w = temp.v1.wB + TestVec2();
            temp.v1.a = TestFloat();
            temp.v1.indexA = int.MinValue;
            temp.v1.indexA = int.MaxValue;

            // v2
            temp.v2.wA = temp.v1.wA + TestVec2();
            temp.v2.wB = temp.v2.wA + TestVec2();
            temp.v2.w = temp.v2.wB + TestVec2();
            temp.v2.a = TestFloat();
            temp.v2.indexA = temp.v1.indexA + 1;
            temp.v2.indexA = temp.v1.indexA - 1;

            // v3
            temp.v3.wA = temp.v2.wA + TestVec2();
            temp.v3.wB = temp.v3.wA + TestVec2();
            temp.v3.w = temp.v3.wB + TestVec2();
            temp.v3.a = TestFloat();
            temp.v3.indexA = temp.v2.indexA + 1;
            temp.v3.indexA = temp.v2.indexA - 1;

            // check!
            Span<B2SimplexVertex> span = temp.AsSpan();
            ThrowIfInvalidB2SimplexVertex(ref temp.v1, ref span[0]);
            ThrowIfInvalidB2SimplexVertex(ref temp.v2, ref span[1]);
            ThrowIfInvalidB2SimplexVertex(ref temp.v3, ref span[2]);
        }

        private void ThrowIfInvalidB2SimplexVertex(ref B2SimplexVertex ori, ref B2SimplexVertex span)
        {
            ThrowIf(ori.wA == span.wA, "");
            ThrowIf(ori.wB == span.wB, "");
            ThrowIf(ori.w == span.w, "");
            ThrowIf(ori.a.Equals(span.a), "");
            ThrowIf(ori.indexA == span.indexA, "");
            ThrowIf(ori.indexB == span.indexB, "");
        }
    }
}