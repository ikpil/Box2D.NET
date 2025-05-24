// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;

namespace Box2D.NET
{
    public static class B2Profiling
    {
#if BOX2D_PROFILE
        /// Tracy profiler instrumentation
        /// https://github.com/wolfpld/tracy
        public static void b2TracyCZoneC( object ctx, object color, object active )
        {
            TracyCZoneC(ctx, color, active);
        }
        public static void b2TracyCZoneNC(object object ctx, object name, object color, object active )
        {
            TracyCZoneNC(ctx, name, color, active);
        }
        public static void b2TracyCZoneEnd(object ctx)
        {
            TracyCZoneEnd(context);
        }
#else
        [Conditional("DEBUG")]
        public static void b2TracyCZoneC(B2TracyCZone ctx, B2HexColor color, bool active)
        {
        }

        [Conditional("DEBUG")]
        public static void b2TracyCZoneNC(B2TracyCZone ctx, string name, B2HexColor color, bool active)
        {
        }

        [Conditional("DEBUG")]
        public static void b2TracyCZoneEnd(B2TracyCZone ctx)
        {
        }

        [Conditional("DEBUG")]
        public static void TracyCFrameMark()
        {
        }
#endif


#if BOX2D_PROFILE
        public static void b2TracyCAlloc<T>(T[] ptr, int size)
        {
            TracyCAlloc( ptr, size )
        }

        public static void b2TracyCFree<T>(T[] ptr)
        {
            TracyCFree( ptr )
        }
#else
        public static void b2TracyCAlloc<T>(T[] ptr, int size)
        {
        }

        public static void b2TracyCFree<T>(T[] ptr)
        {
        }

        public static void b2TracyCFree<T>(T ptr)
        {
        }

#endif

    }
} 