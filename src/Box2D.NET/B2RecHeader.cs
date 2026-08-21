// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // File header, fixed 32 bytes, little-endian
    internal struct B2RecHeader
    {
        internal uint magic;          // 'B2RC' = 0x43523242
        internal ushort versionMajor; // 1
        internal ushort versionMinor; // 0
        internal uint buildHash;      // short git hash, 0 if unstamped
        internal byte simdWidth;      // B2_SIMD_WIDTH, informational
        internal byte pointerWidth;   // sizeof(void*), gates POD-def memcpy
        internal byte bigEndian;      // 0 on all supported targets
        internal byte reserved0;
        internal ulong wallClockUnix; // informational
        internal ulong reserved;      // uint8_t reserved[8]
    }
}
