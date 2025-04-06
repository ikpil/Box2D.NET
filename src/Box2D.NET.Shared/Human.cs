﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;

namespace Box2D.NET.Shared
{
    public struct Human
    {
        public B2FixedArray11<Bone> bones;
        public float scale;
        public bool isSpawned;

        public void Clear()
        {
            Debug.Assert((int)BoneId.bone_count == B2FixedArray11<Bone>.Size);

            for (int i = 0; i < bones.Length; ++i)
            {
                bones[i] = new Bone();
            }

            scale = 0.0f;
            isSpawned = false;
        }
    }
}