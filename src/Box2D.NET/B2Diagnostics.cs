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

        // Used to validate API inputs. Returns false if the input is rejected, so the caller can return.
        public static bool B2_CHECK_INPUT(bool condition, [CallerArgumentExpression("condition")] string message = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        {
            if (condition)
            {
                return true;
            }

            b2Log($"invalid input: {message} in {memberName}\n");
            B2_ASSERT(condition, message, fileName, lineNumber, memberName);
            return false;
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

        private static b2AssertCallbackFcn _b2AssertCallbackHandler = b2DefaultAssertFcn;

        /// Override the default assert function
        /// @param assertFcn a non-null assert callback
        public static void b2SetAssertCallback(b2AssertCallbackFcn assertCallbackFcn)
        {
            B2_ASSERT(assertCallbackFcn != null);
            _b2AssertCallbackHandler = assertCallbackFcn;
        }

        public static int b2InternalAssert(string condition, string fileName, int lineNumber)
        {
            return _b2AssertCallbackHandler(condition, fileName, lineNumber);
        }

        public static void b2DefaultLogFcn(in string message)
        {
            Console.WriteLine($"Box2D: {message}");
        }

        private static b2LogCallbackFcn _b2LogCallbackHandler = b2DefaultLogFcn;

        /// Override the default log function
        /// @param logFcn a non-null log callback
        public static void b2SetLogCallback(b2LogCallbackFcn logCallbackFcn)
        {
            B2_ASSERT(logCallbackFcn != null);
            _b2LogCallbackHandler = logCallbackFcn;
        }

        public static void b2Log(in string format)
        {
            _b2LogCallbackHandler.Invoke(format);
        }
    }
}
