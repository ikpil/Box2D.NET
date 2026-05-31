// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public enum B2SchedulerTaskStatus
    {
        b2_schedulerFree = 0,
        b2_schedulerPending = 1,
        b2_schedulerClaimed = 2,
        b2_schedulerComplete = 3,
    }
}
