// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Box2D.NET
{
    public static class B2Diagnostics
    {
        // Used to prevent the compiler from warning about unused variables
        [Conditional("DEBUG")]
        public static void B2_UNUSED<T1>(in T1 a)
        {
            // ...
        }

        [Conditional("DEBUG")]
        public static void B2_UNUSED<T1, T2>(in T1 a, in T2 b)
        {
            // ...
        }

        [Conditional("DEBUG")]
        public static void B2_UNUSED<T1, T2, T3>(in T1 a, in T2 b, in T3 c)
        {
            // ...
        }

        [Conditional("DEBUG")]
        public static void B2_UNUSED<T1, T2, T3, T4>(in T1 a, in T2 b, in T3 c, in T4 d)
        {
            // ...
        }

        [Conditional("DEBUG")]
        public static void B2_UNUSED<T1, T2, T3, T4, T5>(in T1 a, in T2 b, in T3 c, in T4 d, in T5 e)
        {
            // ...
        }

        [Conditional("DEBUG")]
        public static void B2_UNUSED<T1, T2, T3, T4, T5, T6>(in T1 a, in T2 b, in T3 c, in T4 d, in T5 e, in T6 f)
        {
            // ...
        }

        [Conditional("DEBUG")]
        public static void B2_ASSERT(bool condition, string message = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        {
            if (condition)
                return;

            throw new InvalidOperationException($"{message} {memberName}() {fileName}:{lineNumber}");
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
            B2_ASSERT(assertFcn != null);
            b2AssertHandler = assertFcn;
        }

        internal static int b2InternalAssertFcn(string condition, string fileName, int lineNumber)
        {
            return b2AssertHandler(condition, fileName, lineNumber);
        }
    }
}