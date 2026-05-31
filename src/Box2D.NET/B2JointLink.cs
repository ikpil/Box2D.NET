// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Cached joint data stored in the island for fast contiguous iteration.
    public class B2JointLink
    {
        public int jointId;
        public int bodyIdA;
        public int bodyIdB;

        public B2JointLink ToCopy()
        {
            var copy = new B2JointLink();
            copy.jointId = jointId;
            copy.bodyIdA = bodyIdA;
            copy.bodyIdB = bodyIdB;
            
            return copy;
        }
    }
}
