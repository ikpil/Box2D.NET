// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;

namespace Box2D.NET
{
    public static class B2FixedArrayExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ref B2FixedArray1<T> array) where T : unmanaged
        {
            return array.AsSpanUnsafe();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ref B2FixedArray2<T> array) where T : unmanaged
        {
            return array.AsSpanUnsafe();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ref B2FixedArray3<T> array) where T : unmanaged
        {
            return array.AsSpanUnsafe();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ref B2FixedArray4<T> array) where T : unmanaged
        {
            return array.AsSpanUnsafe();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ref B2FixedArray7<T> array) where T : unmanaged
        {
            return array.AsSpanUnsafe();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ref B2FixedArray8<T> array) where T : unmanaged
        {
            return array.AsSpanUnsafe();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ref B2FixedArray11<T> array) where T : unmanaged
        {
            return array.AsSpanUnsafe();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ref B2FixedArray12<T> array) where T : unmanaged
        {
            return array.AsSpanUnsafe();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ref B2FixedArray16<T> array) where T : unmanaged
        {
            return array.AsSpanUnsafe();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ref B2FixedArray24<T> array) where T : unmanaged
        {
            return array.AsSpanUnsafe();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ref B2FixedArray32<T> array) where T : unmanaged
        {
            return array.AsSpanUnsafe();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ref B2FixedArray64<T> array) where T : unmanaged
        {
            return array.AsSpanUnsafe();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this ref B2FixedArray1024<T> array) where T : unmanaged
        {
            return array.AsSpanUnsafe();
        }
    }
}
