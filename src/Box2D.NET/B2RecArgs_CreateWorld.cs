// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Codegen pass 1a: arg structs, generated in recording.c, declared here for call sites.
    // These are typedef'd in recording.c before the write helpers, but must be visible
    // in body.c and shape.c which use B2_REC. We generate them via the X-macro here.
    // IMPORTANT: this block must not expand ARG or B2_REC_OP as functions.
    internal struct B2RecArgs_CreateWorld
    {
        internal B2WorldDef def;
    }
}
