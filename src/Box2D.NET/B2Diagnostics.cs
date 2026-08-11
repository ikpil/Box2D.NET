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
        public static void B2_UNUSED<T1, T2>(in Span<T1> a, in T2 b)
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
        [Conditional("B2_ENABLE_ASSERT")]
        public static void B2_ASSERT(bool condition, [CallerArgumentExpression("condition")] string message = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        {
            if (condition)
                return;

            string conditionText = string.IsNullOrEmpty(message) ? memberName : message;
            int result = b2InternalAssert(conditionText, fileName, lineNumber);
            if (result != 0)
            {
                // A managed exception is the portable equivalent of the native debugger breakpoint.
                throw new InvalidOperationException($"{conditionText} {memberName}() {fileName}:{lineNumber}");
            }
        }

        [Conditional("DEBUG")]
        public static void B2_VALIDATE(bool condition, string message = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        {
            if (condition)
                return;

            throw new InvalidOperationException($"{message} {memberName}() {fileName}:{lineNumber}");
        }

        public static int b2DefaultAssertFcn(string condition, string fileName, int lineNumber)
        {
            Console.Error.Write($"BOX2D ASSERTION: {condition}, {fileName}, line {lineNumber}\n");
            Console.Error.Flush();

            // return non-zero to break to debugger
            return 1;
        }

        private static b2AssertFcn b2AssertHandler = b2DefaultAssertFcn;

        /// Override the default assert function
        /// @param assertFcn a non-null assert callback
        public static void b2SetAssertFcn(b2AssertFcn assertFcn)
        {
            B2_ASSERT(assertFcn != null);
            b2AssertHandler = assertFcn;
        }

        public static int b2InternalAssert(string condition, string fileName, int lineNumber)
        {
            return b2AssertHandler(condition, fileName, lineNumber);
        }

        static void b2DefaultLogFcn(in string message)
        {
            Console.WriteLine($"Box2D: {message}");
        }

        private static b2LogFcn b2LogHandler = b2DefaultLogFcn;

        /// Override the default log function
        /// @param logFcn a non-null log callback
        public static void b2SetLogFcn(b2LogFcn logFcn)
        {
            B2_ASSERT(logFcn != null);
            b2LogHandler = logFcn;
        }

        public static void b2Log(in string format)
        {
            b2LogHandler.Invoke(format);
        }
    }
}
