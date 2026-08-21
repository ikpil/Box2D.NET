// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Reader state threaded through the replay loop and all dispatch functions
    // A class preserves the C pointer semantics while managed reader helpers advance shared state.
    internal sealed class B2RecReader
    {
        internal byte[] data;
        internal int size;
        internal int cursor;
        internal B2WorldId replayWorldId; // world created during replay; valid after CreateWorld record
        internal int workerCount; // 0 = use recorded count
        internal bool ok; // false on read overrun or id mismatch, a fatal stop
        internal bool diverged; // a StateHash failed to reproduce, non-fatal so a viewer can keep playing

        // Scratch for variable-length defs (chain points/materials), grown on demand and
        // freed with the player. Cloned by the create call so only valid during one dispatch.
        internal B2Vec2[] chainPoints;
        internal int chainPointCap;
        internal B2SurfaceMaterial[] chainMaterials;
        internal int chainMaterialCap;

        // Scratch for recorded query hits; grown on demand, freed with the player
        internal B2RecRecordedHit[] hits;
        internal int hitCap;

        internal B2RecPlayer owner; // player that owns this reader
    }
}
