// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    public struct b2JointPair
    {
        public b2Joint joint;
        public b2JointSim jointSim;

        public b2JointPair(b2Joint joint, b2JointSim jointSim)
        {
            this.joint = joint;
            this.jointSim = jointSim;
        }
    }
}
