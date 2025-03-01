// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Output parameters for b2TimeOfImpact.
    public ref struct B2TOIOutput
    {
        public B2TOIState state; // The type of result
        public float fraction; // The sweep time of the collision
    }
}
