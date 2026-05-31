// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Threading;

namespace Box2D.NET
{
    public delegate void b2ThreadFunction(object context);

    public static class B2Threads
    {
        // macOS pthread_setname_np takes only the name; it always names the calling thread.
        public static void b2SetCurrentThreadName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            Thread.CurrentThread.Name = name;
        }

        public static void b2ThreadStart(object param)
        {
            B2Thread t = (B2Thread)param;
            b2SetCurrentThreadName(t.name);
            t.function(t.context);
        }

        // Name may be NULL, otherwise it is copied.
        public static B2Thread b2CreateThread(b2ThreadFunction function, object context, string name)
        {
            var t = new B2Thread();
            t.function = function;
            t.context = context;
            if (!string.IsNullOrEmpty(name))
            {
                t.name = name;
            }
            else
            {
                t.name = string.Empty;
            }

            t.thread = new Thread(b2ThreadStart)
            {
                IsBackground = true,
            };
            t.thread.Start(t);
            return t;
        }

        public static void b2JoinThread(B2Thread t)
        {
            t.thread.Join();
            t.thread = null;
            t.name = string.Empty;
            t.function = null;
            t.context = null;
        }
    }
}
