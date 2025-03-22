// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
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

            return 0;
        }

        private B2Vec2 RandVec2()
        {
            long ticks = DateTime.UtcNow.Ticks;

            var random = new B2Vec2();
            random.X = (ticks % 100000) / 100000f;
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

            ThrowIfInvalidFixedArray(array1.AsSpan(), RandVec2, "");
            ThrowIfInvalidFixedArray(array2.AsSpan(), RandVec2, "");
            ThrowIfInvalidFixedArray(array3.AsSpan(), RandVec2, "");
            ThrowIfInvalidFixedArray(array4.AsSpan(), RandVec2, "");

            ThrowIfInvalidFixedArray(array7.AsSpan(), RandVec2, "");
            ThrowIfInvalidFixedArray(array8.AsSpan(), RandVec2, "");
            ThrowIfInvalidFixedArray(array11.AsSpan(), RandVec2, "");
            ThrowIfInvalidFixedArray(array12.AsSpan(), RandVec2, "");

            ThrowIfInvalidFixedArray(array16.AsSpan(), RandVec2, "");
            ThrowIfInvalidFixedArray(array64.AsSpan(), RandVec2, "");
            ThrowIfInvalidFixedArray(array1024.AsSpan(), RandVec2, "");
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
    }
}