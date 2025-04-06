// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace Box2D.NET.Samples.Helpers;

public readonly struct SizeOf<T> where T : unmanaged
{
    public static readonly uint Size = (uint)Marshal.SizeOf<T>();
}

