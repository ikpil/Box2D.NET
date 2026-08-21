// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.IO;
using System.Runtime.CompilerServices;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET
{
    public static partial class B2Recordings
    {
        // Lifecycle
        internal static void b2StartRecording(B2World world, in B2WorldDef def)
        {
            B2Recording rec = new B2Recording();

            try
            {
                // Open for read+write so b2World_SaveRecording can copy the file while it stays open
                rec.file = new FileStream(def.recordingPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                return;
            }

            rec.@lock = new object();

            rec.buffer = default;

            B2RecHeader hdr = new B2RecHeader();
            hdr.magic = B2_REC_MAGIC;
            hdr.versionMajor = 1;
            hdr.versionMinor = 0;
            hdr.buildHash = B2BuildHash.Value;
            hdr.simdWidth = (byte)B2_SIMD_WIDTH;
            hdr.pointerWidth = (byte)IntPtr.Size;
            hdr.bigEndian = 0;
            hdr.wallClockUnix = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            hdr.reserved = 0;

            // The header is fixed at 32 bytes. Write each field explicitly to avoid CLR padding.
            b2RecW_U32(ref rec.buffer, hdr.magic);
            b2RecW_U16(ref rec.buffer, hdr.versionMajor);
            b2RecW_U16(ref rec.buffer, hdr.versionMinor);
            b2RecW_U32(ref rec.buffer, hdr.buildHash);
            b2RecW_U8(ref rec.buffer, hdr.simdWidth);
            b2RecW_U8(ref rec.buffer, hdr.pointerWidth);
            b2RecW_U8(ref rec.buffer, hdr.bigEndian);
            b2RecW_U8(ref rec.buffer, hdr.reserved0);
            b2RecW_U64(ref rec.buffer, hdr.wallClockUnix);
            b2RecW_U64(ref rec.buffer, hdr.reserved);

            world.recording = rec;

            // Write the CreateWorld record, first record in file, self-describes the world
            B2RecArgs_CreateWorld a = new B2RecArgs_CreateWorld
            {
                def = def,
            };
            b2RecWrite_CreateWorld(rec, in a);
        }

        internal static void b2StopRecordingInternal(B2World world)
        {
            if (world.recording == null)
            {
                return;
            }

            B2Recording rec = world.recording;
            world.recording = null;

            // Write DestroyWorld so the file is self-contained
            B2WorldId wid = new B2WorldId((ushort)(world.worldId + 1), world.generation);
            B2RecArgs_DestroyWorld a = new B2RecArgs_DestroyWorld
            {
                world = wid,
            };
            b2RecWrite_DestroyWorld(rec, in a);

            b2FlushRecording(rec);
            rec.file.Dispose();
            b2RecBufFree(ref rec.buffer);
        }

        // Deterministic hash over all body transforms and velocities.
        // Called by both recorder and replayer to verify simulation reproduces exactly.
        // Deterministic over worker count: walk the bodies sparse array in index order, not
        // solver set order. Free slots have body->id == B2_NULL_INDEX, live slots have body->id == i
        internal static ulong b2HashWorldState(B2World world)
        {
            // FNV-1a 64-bit
            ulong hash = 14695981039346656037ul;
            const ulong prime = 1099511628211ul;

            int bodyCount = world.bodies.count;
            for (int i = 0; i < bodyCount; ++i)
            {
                B2Body body = world.bodies.data[i];
                if (body.id != i)
                {
                    // Free or never-used slot
                    continue;
                }

                B2BodySim sim = b2GetBodySim(world, body);
                hash = b2HashFloat(hash, sim.transform.p.X, prime);
                hash = b2HashFloat(hash, sim.transform.p.Y, prime);
                hash = b2HashFloat(hash, sim.transform.q.c, prime);
                hash = b2HashFloat(hash, sim.transform.q.s, prime);

                B2BodyState state = b2GetBodyState(world, body);
                if (state != null)
                {
                    hash = b2HashFloat(hash, state.linearVelocity.X, prime);
                    hash = b2HashFloat(hash, state.linearVelocity.Y, prime);
                    hash = b2HashFloat(hash, state.angularVelocity, prime);
                }
            }

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong b2HashFloat(ulong hash, float value, ulong prime)
        {
            uint bits = unchecked((uint)BitConverter.SingleToInt32Bits(value));
            return unchecked((hash ^ bits) * prime);
        }

        /// Flush the current recording buffer and copy the recording to @p path.
        /// The internal recording remains open and active. Use this to snapshot the session.
        /// @param worldId The world being recorded
        /// @param path Destination file path for the copy
        public static void b2World_SaveRecording(B2WorldId worldId, string path)
        {
            B2World world = b2GetUnlockedWorldFromId(worldId);
            if (world == null || world.recording == null || path == null)
            {
                return;
            }

            // Flush under the lock so a query committing from another thread can't race the shared buffer.
            // The file copy below touches only the file, which query commits never write, so it stays unlocked.
            lock (world.recording.@lock)
            {
                b2FlushRecording(world.recording);
            }
            world.recording.file.Flush();

            // Stream-copy the recording to the user path. Source file stays open.
            FileStream src = world.recording.file;
            long position;
            try
            {
                position = src.Position;
                src.Position = 0;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                return;
            }

            FileStream dst;
            try
            {
                dst = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                src.Position = position;
                return;
            }

            try
            {
                Span<byte> copyBuf = stackalloc byte[4096];
                int count;
                while ((count = src.Read(copyBuf)) > 0)
                {
                    dst.Write(copyBuf.Slice(0, count));
                }
            }
            catch (IOException)
            {
                // The C API is void and does not report stream-copy failures.
            }
            finally
            {
                dst.Dispose();
                src.Position = position;
            }
        }

        /// Stop the current recording, flush the buffer, and close the file.
        /// @param worldId The world being recorded
        public static void b2World_StopRecording(B2WorldId worldId)
        {
            B2World world = b2GetUnlockedWorldFromId(worldId);
            if (world == null)
            {
                return;
            }

            b2StopRecordingInternal(world);
        }
    }
}
