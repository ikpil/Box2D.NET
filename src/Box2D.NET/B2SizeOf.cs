// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace Box2D.NET
{
    public readonly struct B2SizeOf<T>
    {
        public static readonly int Size = Marshal.SizeOf<T>();
    }
}