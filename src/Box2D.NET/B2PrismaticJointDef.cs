// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Prismatic joint definition
    /// Body B may slide along the x-axis in local frame A. Body B cannot rotate relative to body A.
    /// The joint translation is zero when the local frame origins coincide in world space.
    /// @ingroup prismatic_joint
    public struct B2PrismaticJointDef
    {
        /// Base joint definition
        public B2JointDef @base;

        /// Enable a linear spring along the prismatic joint axis
        public bool enableSpring;

        /// The spring stiffness Hertz, cycles per second
        public float hertz;

        /// The spring damping ratio, non-dimensional
        public float dampingRatio;
        
        /// The target translation for the joint in meters. The spring-damper will drive
        /// to this translation.
        public float targetTranslation;

        /// Enable/disable the joint limit
        public bool enableLimit;

        /// The lower translation limit
        public float lowerTranslation;

        /// The upper translation limit
        public float upperTranslation;

        /// Enable/disable the joint motor
        public bool enableMotor;

        /// The maximum motor force, typically in newtons
        public float maxMotorForce;

        /// The desired motor speed, typically in meters per second
        public float motorSpeed;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
