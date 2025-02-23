﻿using System;

namespace Box2D.NET.Primitives
{
    /// A 2D rigid transform
    public struct b2Transform
    {
        public b2Vec2 p;
        public b2Rot q;

        public b2Transform(b2Vec2 p, b2Rot q)
        {
            this.p = p;
            this.q = q;
        }

        public bool TryWriteBytes(Span<byte> bytes)
        {
            if (!BitConverter.TryWriteBytes(bytes.Slice(0, 4), p.x))
                return false;

            if (!BitConverter.TryWriteBytes(bytes.Slice(4, 4), p.y))
                return false;
            
            if (!BitConverter.TryWriteBytes(bytes.Slice(8, 4), q.c))
                return false;
            
            if (!BitConverter.TryWriteBytes(bytes.Slice(12, 4), q.s))
                return false;

            return true;
        }
    }
}