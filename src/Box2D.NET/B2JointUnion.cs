// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace Box2D.NET
{
    [StructLayout(LayoutKind.Explicit)]
    public struct B2JointUnion
    {
        [FieldOffset(0)]
        public B2DistanceJoint distanceJoint;
        
        [FieldOffset(0)]
        public B2MotorJoint motorJoint;
        
        [FieldOffset(0)]
        public B2MouseJoint mouseJoint;
        
        [FieldOffset(0)]
        public B2RevoluteJoint revoluteJoint;
        
        [FieldOffset(0)]
        public B2PrismaticJoint prismaticJoint;
        
        [FieldOffset(0)]
        public B2WeldJoint weldJoint;
        
        [FieldOffset(0)]
        public B2WheelJoint wheelJoint;
            
    }

}
