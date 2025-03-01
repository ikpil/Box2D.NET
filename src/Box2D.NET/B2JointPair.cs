// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2JointPair
    {
        public B2Joint joint;
        public B2JointSim jointSim;

        public B2JointPair(B2Joint joint, B2JointSim jointSim)
        {
            this.joint = joint;
            this.jointSim = jointSim;
        }
    }
}
