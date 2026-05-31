// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public class B2SchedulerTask
    {
        public b2TaskCallback callback;
        public object taskContext;
        public B2AtomicInt status;
    }
}
