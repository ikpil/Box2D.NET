﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Body simulation data used for integration of position and velocity
    // Transform data used for collision and solver preparation.
    public class B2BodySim
    {
        // todo better to have transform in sim or in @base body? Try both!
        // transform for body origin
        public B2Transform transform;

        // center of mass position in world space
        public B2Vec2 center;

        // previous rotation and COM for TOI
        public B2Rot rotation0;
        public B2Vec2 center0;

        // location of center of mass relative to the body origin
        public B2Vec2 localCenter;

        public B2Vec2 force;
        public float torque;

        // inverse inertia
        public float invMass;
        public float invInertia;

        public float minExtent;
        public float maxExtent;
        public float linearDamping;
        public float angularDamping;
        public float gravityScale;

        // body data can be moved around, the id is stable (used in b2BodyId)
        public int bodyId;

        // This flag is used for debug draw
        public bool isFast;

        public bool isBullet;
        public bool enableSensorHits;
        public bool isSpeedCapped;
        public bool allowFastRotation;
        public bool enlargeAABB;

        public void Clear()
        {
            transform = new B2Transform();

            center = new B2Vec2();

            rotation0 = new B2Rot();
            center0 = new B2Vec2();

            localCenter = new B2Vec2();

            force = new B2Vec2();
            torque = 0.0f;

            invMass = 0.0f;
            invInertia = 0.0f;

            minExtent = 0.0f;
            maxExtent = 0.0f;
            linearDamping = 0.0f;
            angularDamping = 0.0f;
            gravityScale = 0.0f;

            bodyId = 0;

            isFast = false;
            isBullet = false;

            isSpeedCapped = false;
            allowFastRotation = false;
            enlargeAABB = false;
        }

        public void CopyFrom(B2BodySim other)
        {
            transform = other.transform;

            center = other.center;

            rotation0 = other.rotation0;
            center0 = other.center0;

            localCenter = other.localCenter;

            force = other.force;
            torque = other.torque;

            invMass = other.invMass;
            invInertia = other.invInertia;

            minExtent = other.minExtent;
            maxExtent = other.maxExtent;
            linearDamping = other.linearDamping;
            angularDamping = other.angularDamping;
            gravityScale = other.gravityScale;

            bodyId = other.bodyId;

            isFast = other.isFast;
            isBullet = other.isBullet;

            isSpeedCapped = other.isSpeedCapped;
            allowFastRotation = other.allowFastRotation;
            enlargeAABB = other.enlargeAABB;
        }
    }
}
