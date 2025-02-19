using System;
using System.Diagnostics;
using static Box2D.NET.atomic;
using static Box2D.NET.math_function;
using static Box2D.NET.constants;

namespace Box2D.NET
{
    public struct b2AtomicInt
    {
        public volatile int value;
    }

    public struct b2AtomicU32
    {
        public volatile uint value;
    }

    /// Version numbering scheme.
    /// See https://semver.org/
    public struct b2Version
    {
        /// Significant changes
        public int major;

        /// Incremental changes
        public int minor;

        /// Bug fixes
        public int revision;

        public b2Version(int major, int minor, int revision)
        {
            this.minor = major;
            this.minor = minor;
            this.revision = revision;
        }
    }

    public enum b2TracyCZone
    {
        pair_task,
        update_pairs,
        create_contacts,
        prepare_overflow_contact,
        warmstart_overflow_contact,
        solve_contact,
        overflow_resitution,
        prepare_contact,
        warm_start_contact,
        restitution,
        store_impulses,
        merge_islands,
        split,
        sensor_task,
        overlap_sensors,
        sensor_state,
        integrate_velocity,
        prepare_joints,
        warm_joints,
        solve_joints,
        integrate_positions,
        ccd,
        finalize_transfprms,
        bullet_body_task,
        merge,
        prepare_stages,
        solve_constraints,
        update_transforms,
        hit_events,
        refit_bvh,
        bullets,
        sleep_islands,
        collide_task,
        tree_task,
        collide,
        contact_state,
        world_step,
    }

    public static class core
    {
        private static b2AtomicInt b2_byteCount;


        public static T[] b2GrowAlloc<T>(T[] oldMem, int oldSize, int newSize) where T : new()
        {
            Debug.Assert(newSize > oldSize);
            T[] newMem = new T[newSize];
            if (oldSize > 0)
            {
                Array.Copy(oldMem, newMem, oldSize);
                b2Free(oldMem, oldSize);
            }

            if (!typeof(T).IsValueType)
            {
                for (int i = oldSize; i < newSize; ++i)
                {
                    newMem[i] = new T();
                }
            }

            return newMem;
        }


        public static void memset<T>(Span<T> array, T value, int count)
        {
            array.Slice(0, count).Fill(value);
        }

        public static void memcpy<T>(Span<T> dst, Span<T> src, int count)
        {
            src.Slice(0, count).CopyTo(dst);
        }

        public static void memcpy<T>(Span<T> dst, Span<T> src)
        {
            src.CopyTo(dst);
        }

        // Used to prevent the compiler from warning about unused variables
        public static void B2_UNUSED<T1>(T1 a)
        {
            // ...
        }

        public static void B2_UNUSED<T1, T2>(T1 a, T2 b)
        {
            // ...
        }

        public static void B2_UNUSED<T1, T2, T3>(T1 a, T2 b, T3 c)
        {
            // ...
        }

        public static void B2_UNUSED<T1, T2, T3, T4>(T1 a, T2 b, T3 c, T4 d)
        {
            // ...
        }

        public static void B2_UNUSED<T1, T2, T3, T4, T5>(T1 a, T2 b, T3 c, T4 d, T5 e)
        {
            // ...
        }

        public static void B2_UNUSED<T1, T2, T3, T4, T5, T6>(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f)
        {
            // ...
        }


        /// Prototype for user allocation function
        /// @param size the allocation size in bytes
        /// @param alignment the required alignment, guaranteed to be a power of 2
        public delegate T[] b2AllocFcn<T>(int size, int alignment);

        /// Prototype for user free function
        /// @param mem the memory previously allocated through `b2AllocFcn`
        public delegate void b2FreeFcn<T>(T[] mem);

        /// Prototype for the user assert callback. Return 0 to skip the debugger break.
        public delegate int b2AssertFcn(string condition, string fileName, int lineNumber);


        /// @return the total bytes allocated by Box2D
        public static int b2GetByteCount()
        {
            return b2AtomicLoadInt(ref b2_byteCount);
        }


        /// Get the current version of Box2D
        public static b2Version b2GetVersion()
        {
            return new b2Version(3, 1, 0);
        }


        // TODO: @ikpil. check SIMD
// // Define SIMD
// #if BOX2D_ENABLE_SIMD
// 	#if defined( B2_CPU_X86_X64 )
// 		#if defined( BOX2D_AVX2 )
// 			#define B2_SIMD_AVX2
// 			#define B2_SIMD_WIDTH 8
// 		#else
// 			#define B2_SIMD_SSE2
// 			#define B2_SIMD_WIDTH 4
// 		#endif
// 	#elif defined( B2_CPU_ARM )
// 		#define B2_SIMD_NEON
// 		#define B2_SIMD_WIDTH 4
// 	#elif defined( B2_CPU_WASM )
// 		#define B2_CPU_WASM
// 		#define B2_SIMD_SSE2
// 		#define B2_SIMD_WIDTH 4
// 	#else
// 		#define B2_SIMD_NONE
// 		#define B2_SIMD_WIDTH 4
// 	#endif
// #else
        // note: I tried width of 1 and got no performance change
        public const int B2_SIMD_WIDTH = 4;
//#endif

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
        TracyCZoneEnd(ctx);
    }
#else
        public static void b2TracyCZoneC(b2TracyCZone ctx, b2HexColor color, bool active)
        {
        }

        public static void b2TracyCZoneNC(b2TracyCZone ctx, string name, b2HexColor color, bool active)
        {
        }

        public static void b2TracyCZoneEnd(b2TracyCZone ctx)
        {
        }

        public static void TracyCFrameMark()
        {
        }
#endif

        // Returns the number of elements of an array
        public static int B2_ARRAY_COUNT<T>(T[] A)
        {
            return A.Length;
        }

        public static void B2_CHECK_DEF(b2WheelJointDef DEF)
        {
            Debug.Assert(DEF.internalValue == B2_SECRET_COOKIE);
        }


        public static void B2_CHECK_DEF(b2WeldJointDef DEF)
        {
            Debug.Assert(DEF.internalValue == B2_SECRET_COOKIE);
        }

        public static void B2_CHECK_DEF(b2PrismaticJointDef DEF)
        {
            Debug.Assert(DEF.internalValue == B2_SECRET_COOKIE);
        }

        public static void B2_CHECK_DEF(b2RevoluteJointDef DEF)
        {
            Debug.Assert(DEF.internalValue == B2_SECRET_COOKIE);
        }


        public static void B2_CHECK_DEF(b2NullJointDef DEF)
        {
            Debug.Assert(DEF.internalValue == B2_SECRET_COOKIE);
        }

        public static void B2_CHECK_DEF(b2MouseJointDef DEF)
        {
            Debug.Assert(DEF.internalValue == B2_SECRET_COOKIE);
        }

        public static void B2_CHECK_DEF(b2MotorJointDef DEF)
        {
            Debug.Assert(DEF.internalValue == B2_SECRET_COOKIE);
        }

        public static void B2_CHECK_DEF(b2DistanceJointDef DEF)
        {
            Debug.Assert(DEF.internalValue == B2_SECRET_COOKIE);
        }

        public static void B2_CHECK_DEF(b2ChainDef DEF)
        {
            Debug.Assert(DEF.internalValue == B2_SECRET_COOKIE);
        }

        public static void B2_CHECK_DEF(b2BodyDef DEF)
        {
            Debug.Assert(DEF.internalValue == B2_SECRET_COOKIE);
        }

        public static void B2_CHECK_DEF(b2WorldDef DEF)
        {
            Debug.Assert(DEF.internalValue == B2_SECRET_COOKIE);
        }

        public static void B2_CHECK_DEF(b2ShapeDef DEF)
        {
            Debug.Assert(DEF.internalValue == B2_SECRET_COOKIE);
        }


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
#endif


        // This allows the user to change the length units at runtime
        public static float b2_lengthUnitsPerMeter = 1.0f;

        /// Box2D bases all length units on meters, but you may need different units for your game.
        /// You can set this value to use different units. This should be done at application startup
        /// and only modified once. Default value is 1.
        /// For example, if your game uses pixels for units you can use pixels for all length values
        /// sent to Box2D. There should be no extra cost. However, Box2D has some internal tolerances
        /// and thresholds that have been tuned for meters. By calling this function, Box2D is able
        /// to adjust those tolerances and thresholds to improve accuracy.
        /// A good rule of thumb is to pass the height of your player character to this function. So
        /// if your player character is 32 pixels high, then pass 32 to this function. Then you may
        /// confidently use pixels for all the length values sent to Box2D. All length values returned
        /// from Box2D will also be pixels because Box2D does not do any scaling internally.
        /// However, you are now on the hook for coming up with good values for gravity, density, and
        /// forces.
        /// @warning This must be modified before any calls to Box2D
        public static void b2SetLengthUnitsPerMeter(float lengthUnits)
        {
            Debug.Assert(b2IsValidFloat(lengthUnits) && lengthUnits > 0.0f);
            b2_lengthUnitsPerMeter = lengthUnits;
        }

        /// Get the current length units per meter.
        public static float b2GetLengthUnitsPerMeter()
        {
            return b2_lengthUnitsPerMeter;
        }

        public static int b2DefaultAssertFcn(string condition, string fileName, int lineNumber)
        {
            Console.Write($"BOX2D ASSERTION: {condition}, {fileName}, line {lineNumber}\n");

            // return non-zero to break to debugger
            return 1;
        }

        private static b2AssertFcn b2AssertHandler = b2DefaultAssertFcn;

        /// Override the default assert callback
        /// @param assertFcn a non-null assert callback
        public static void b2SetAssertFcn(b2AssertFcn assertFcn)
        {
            Debug.Assert(assertFcn != null);
            b2AssertHandler = assertFcn;
        }

        public static int b2InternalAssertFcn(string condition, string fileName, int lineNumber)
        {
            return b2AssertHandler(condition, fileName, lineNumber);
        }

        // static b2AllocFcn* b2_allocFcn = NULL;
        // static b2FreeFcn* b2_freeFcn = NULL;
        //
        //
        /// This allows the user to override the allocation functions. These should be
        /// set during application startup.
        // public static void b2SetAllocator(b2AllocFcn* allocFcn, b2FreeFcn* freeFcn)
        // {
        //     b2_allocFcn = allocFcn;
        //     b2_freeFcn = freeFcn;
        // }
        public static T[] b2Alloc<T>(int size) where T : new()
        {
            if (size == 0)
            {
                return null;
            }

            T[] ptr = null;
            if (typeof(T).IsValueType)
            {
                ptr = new T[size];
            }
            else
            {
                ptr = new T[size];
                for (int i = 0; i < size; i++)
                {
                    ptr[i] = new T();
                }
            }

            // This could cause some sharing issues, however Box2D rarely calls b2Alloc.
            b2AtomicFetchAddInt(ref b2_byteCount, size);

            // Allocation must be a multiple of 32 or risk a seg fault
            // https://en.cppreference.com/w/c/memory/aligned_alloc
            int size32 = ((size - 1) | 0x1F) + 1;

            // if (b2_allocFcn != null)
            // {
            //     T[] ptr = b2_allocFcn(size32, B2_ALIGNMENT);
            //     b2TracyCAlloc(ptr, size);
            //
            //     return ptr;
            // }

            b2TracyCAlloc(ptr, size);

            return ptr;
        }

        public static void b2Free<T>(T[] mem, int size)
        {
            if (mem == null)
            {
                return;
            }

            b2TracyCFree(mem);

            // if (b2_freeFcn != null)
            // {
            //     b2_freeFcn(mem);
            // }
            // else
            // {
            // }

            b2AtomicFetchAddInt(ref b2_byteCount, -size);
        }
    }
}