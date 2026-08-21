// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Static metadata describing a recording, resolved once when the player opens the file.
    public struct B2RecPlayerInfo
    {
        public int frameCount;     // total recorded steps
        public int workerCount;    // worker count recorded in the world def
        public float timeStep;     // dt of the recorded steps
        public int subStepCount;   // recorded sub-steps
        public uint buildHash;     // engine build that produced the file, 0 if unstamped
        public ulong wallClock;    // unix time the recording was made
    }
}
