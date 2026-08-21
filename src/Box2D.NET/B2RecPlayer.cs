// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Opaque handle for incremental playback of a recording.
    public sealed class B2RecPlayer
    {
        // Incremental player. Owns the file image and drives replay one step at a time.
        internal byte[] data; // file image, owned here
        internal int size;
        internal int headerEnd; // first payload offset
        internal uint buildHash; // engine build that produced the file, from the header
        internal ulong wallClock; // unix time the recording was made, from the header
        internal int frame; // steps dispatched so far
        internal int frameCount; // total recorded steps, counted once at open
        internal int recordedWorkerCount; // worker count from the recorded world def
        internal float recordedDt; // dt of the first recorded step
        internal int recordedSubStepCount; // sub-steps of the first recorded step
        internal int divergeFrame; // first step that diverged, -1 until then
        internal bool atEnd; // a StepFrame ran out of records without reaching a step
        internal B2RecReader rdr; // cursor and replay world, threaded into every dispatcher

        // Per-frame query store, reset at the top of each StepFrame
        internal B2RecDrawQuery[] frameQueries;
        internal int frameQueryCount;
        internal int frameQueryCap;
        internal B2RecRecordedHit[] frameHits;
        internal int frameHitCount;
        internal int frameHitCap;

        // Live bodies in creation order, tracked from create/destroy ops to drive the viewer outliner.
        // Destroyed slots hold b2_nullBodyId so ordinals stay stable. Rebuilt deterministically on replay.
        internal B2BodyId[] bodyIds;
        internal int bodyIdCount;
        internal int bodyIdCap;
    }
}
