// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Ids;

namespace Box2D.NET
{
    public static partial class B2Recordings
    {
        // Overflow-safe growth for the player's accumulating draw arrays. Counts come from the replay
        // itself, not the file, so this only guards the byte-size multiply. Preserves keep elements.
        private static void b2RecGrow<T>(ref T[] data, ref int cap, int need, int keep, int elementSize)
        {
            if (need <= cap)
            {
                return;
            }

            long newCap64 = cap == 0 ? 8 : 2L * cap;
            if (newCap64 < need)
            {
                newCap64 = need;
            }
            B2_ASSERT(newCap64 <= int.MaxValue / elementSize);
            int newCap = checked((int)newCap64);

            T[] grown = new T[newCap];
            if (data != null && keep > 0)
            {
                Array.Copy(data, grown, keep);
            }
            data = grown;
            cap = newCap;
        }

        private static void b2RecGrowFrameQueries(B2RecPlayer player)
        {
            b2RecGrow(ref player.frameQueries, ref player.frameQueryCap, player.frameQueryCount + 1, player.frameQueryCount,
                B2SizeOf<B2RecDrawQuery>.Size);
        }

        private static void b2RecGrowFrameHits(B2RecPlayer player, int need)
        {
            b2RecGrow(ref player.frameHits, ref player.frameHitCap, player.frameHitCount + need, player.frameHitCount,
                B2SizeOf<B2RecRecordedHit>.Size);
        }

        // Grow the rdr's hit scratch to at least n entries
        private static void b2RecEnsureHits(B2RecReader rdr, int n)
        {
            b2RecReserveScratch(rdr, ref rdr.hits, ref rdr.hitCap, n, B2SizeOf<B2RecRecordedHit>.Size);
        }

        // Body and shape ids have world0 replaced with the replay world's slot index
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static B2BodyId b2RecMakeBodyId(B2RecReader rdr, B2BodyId recorded)
        {
            return new B2BodyId(recorded.index1, (ushort)(rdr.replayWorldId.index1 - 1), recorded.generation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static B2ShapeId b2RecMakeShapeId(B2RecReader rdr, B2ShapeId recorded)
        {
            return new B2ShapeId(recorded.index1, (ushort)(rdr.replayWorldId.index1 - 1), recorded.generation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static B2ChainId b2RecMakeChainId(B2RecReader rdr, B2ChainId recorded)
        {
            return new B2ChainId(recorded.index1, (ushort)(rdr.replayWorldId.index1 - 1), recorded.generation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static B2JointId b2RecMakeJointId(B2RecReader rdr, B2JointId recorded)
        {
            return new B2JointId(recorded.index1, (ushort)(rdr.replayWorldId.index1 - 1), recorded.generation);
        }

        private static void b2RecCheckBodyId(B2RecReader rdr, B2BodyId got, B2BodyId recorded)
        {
            b2RecCheckId(rdr, "body", got.index1, got.generation, recorded.index1, recorded.generation);
        }

        private static void b2RecCheckShapeId(B2RecReader rdr, B2ShapeId got, B2ShapeId recorded)
        {
            b2RecCheckId(rdr, "shape", got.index1, got.generation, recorded.index1, recorded.generation);
        }

        private static void b2RecCheckChainId(B2RecReader rdr, B2ChainId got, B2ChainId recorded)
        {
            b2RecCheckId(rdr, "chain", got.index1, got.generation, recorded.index1, recorded.generation);
        }

        private static void b2RecCheckJointId(B2RecReader rdr, B2JointId got, B2JointId recorded)
        {
            b2RecCheckId(rdr, "joint", got.index1, got.generation, recorded.index1, recorded.generation);
        }
    }
}
