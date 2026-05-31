// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Threading;

namespace Box2D.NET
{
    public class B2Thread
    {
        public Thread thread;
        public string name;
        public b2ThreadFunction function;
        public object context;
    }
}
