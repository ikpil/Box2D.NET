// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using Box2D.NET.Primitives;
using Box2D.NET.Shared.Primitives;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.id;
using static Box2D.NET.body;
using static Box2D.NET.geometry;
using static Box2D.NET.shape;
using static Box2D.NET.joint;
using static Box2D.NET.revolute_joint;
using static Box2D.NET.Shared.random;
using static Box2D.NET.hull;

namespace Box2D.NET.Shared
{
    public static class human
    {
        public static void CreateHuman(Human human, b2WorldId worldId, b2Vec2 position, float scale, float frictionTorque, float hertz, float dampingRatio,
            int groupIndex, object userData, bool colorize)
        {
            Debug.Assert(human.isSpawned == false);

            for (int i = 0; i < (int)BoneId.boneId_count; ++i)
            {
                human.bones[i].bodyId = b2_nullBodyId;
                human.bones[i].jointId = b2_nullJointId;
                human.bones[i].frictionScale = 1.0f;
                human.bones[i].parentIndex = -1;
            }

            human.scale = scale;

            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.sleepThreshold = 0.1f;
            bodyDef.userData = userData;

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.2f;
            shapeDef.filter.groupIndex = -groupIndex;
            shapeDef.filter.categoryBits = 2;
            shapeDef.filter.maskBits = (1 | 2);

            b2ShapeDef footShapeDef = shapeDef;
            footShapeDef.friction = 0.05f;

            // feet don't collide with ragdolls
            footShapeDef.filter.categoryBits = 2;
            footShapeDef.filter.maskBits = 1;

            if (colorize)
            {
                footShapeDef.customColor = (uint)b2HexColor.b2_colorSaddleBrown;
            }

            float s = scale;
            float maxTorque = frictionTorque * s;
            bool enableMotor = true;
            bool enableLimit = true;
            float drawSize = 0.05f;

            b2HexColor shirtColor = b2HexColor.b2_colorMediumTurquoise;
            b2HexColor pantColor = b2HexColor.b2_colorDodgerBlue;

            b2HexColor[] skinColors =
            {
                b2HexColor.b2_colorNavajoWhite, b2HexColor.b2_colorLightYellow, b2HexColor.b2_colorPeru, b2HexColor.b2_colorTan
            };

            b2HexColor skinColor = skinColors[groupIndex % 4];

            // hip
            {
                ref Bone bone = ref human.bones[(int)BoneId.boneId_hip];
                bone.parentIndex = -1;

                bodyDef.position = b2Add(new b2Vec2(0.0f, 0.95f * s), position);
                bodyDef.linearDamping = 0.0f;
                bone.bodyId = b2CreateBody(worldId, bodyDef);

                if (colorize)
                {
                    shapeDef.customColor = (uint)pantColor;
                }

                b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, -0.02f * s), new b2Vec2(0.0f, 0.02f * s), 0.095f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);
            }

            // torso
            {
                ref Bone bone = ref human.bones[(int)BoneId.boneId_torso];
                bone.parentIndex = (int)BoneId.boneId_hip;

                bodyDef.position = b2Add(new b2Vec2(0.0f, 1.2f * s), position);
                bodyDef.linearDamping = 0.0f;
                // bodyDef.type = b2_staticBody;
                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 0.5f;
                bodyDef.type = b2BodyType.b2_dynamicBody;

                if (colorize)
                {
                    shapeDef.customColor = (uint)shirtColor;
                }

                b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, -0.135f * s), new b2Vec2(0.0f, 0.135f * s), 0.09f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                b2Vec2 pivot = b2Add(new b2Vec2(0.0f, 1.0f * s), position);
                b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.bodyIdB = bone.bodyId;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.25f * B2_PI;
                jointDef.upperAngle = 0.0f;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.drawSize = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            // head
            {
                ref Bone bone = ref human.bones[(int)BoneId.boneId_head];
                bone.parentIndex = (int)BoneId.boneId_torso;

                bodyDef.position = b2Add(new b2Vec2(0.0f * s, 1.475f * s), position);
                bodyDef.linearDamping = 0.1f;

                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 0.25f;

                if (colorize)
                {
                    shapeDef.customColor = (uint)skinColor;
                }

                b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, -0.038f * s), new b2Vec2(0.0f, 0.039f * s), 0.075f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                //// neck
                // capsule = { { 0.0f, -0.12f * s }, { 0.0f, -0.08f * s }, 0.05f * s };
                // b2CreateCapsuleShape( bone.bodyId, &shapeDef, &capsule );

                b2Vec2 pivot = b2Add(new b2Vec2(0.0f, 1.4f * s), position);
                b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.bodyIdB = bone.bodyId;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.3f * B2_PI;
                jointDef.upperAngle = 0.1f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.drawSize = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            // upper left leg
            {
                ref Bone bone = ref human.bones[(int)BoneId.boneId_upperLeftLeg];
                bone.parentIndex = (int)BoneId.boneId_hip;

                bodyDef.position = b2Add(new b2Vec2(0.0f, 0.775f * s), position);
                bodyDef.linearDamping = 0.0f;
                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 1.0f;

                if (colorize)
                {
                    shapeDef.customColor = (uint)pantColor;
                }

                b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, -0.125f * s), new b2Vec2(0.0f, 0.125f * s), 0.06f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                b2Vec2 pivot = b2Add(new b2Vec2(0.0f, 0.9f * s), position);
                b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.bodyIdB = bone.bodyId;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.05f * B2_PI;
                jointDef.upperAngle = 0.4f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.drawSize = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            Span<b2Vec2> points = stackalloc b2Vec2[4];
            points[0] = new b2Vec2(-0.03f * s, -0.185f * s);
            points[1] = new b2Vec2(0.11f * s, -0.185f * s);
            points[2] = new b2Vec2(0.11f * s, -0.16f * s);
            points[3] = new b2Vec2(-0.03f * s, -0.14f * s);

            b2Hull footHull = b2ComputeHull(points, 4);
            b2Polygon footPolygon = b2MakePolygon(footHull, 0.015f * s);

            // lower left leg
            {
                ref Bone bone = ref human.bones[(int)BoneId.boneId_lowerLeftLeg];
                bone.parentIndex = (int)BoneId.boneId_upperLeftLeg;

                bodyDef.position = b2Add(new b2Vec2(0.0f, 0.475f * s), position);
                bodyDef.linearDamping = 0.0f;
                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 0.5f;

                if (colorize)
                {
                    shapeDef.customColor = (uint)pantColor;
                }

                b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, -0.155f * s), new b2Vec2(0.0f, 0.125f * s), 0.045f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                // b2Polygon box = b2MakeOffsetBox(0.1f * s, 0.03f * s, {0.05f * s, -0.175f * s}, 0.0f);
                // b2CreatePolygonShape(bone.bodyId, &shapeDef, &box);

                // capsule = { { -0.02f * s, -0.175f * s }, { 0.13f * s, -0.175f * s }, 0.03f * s };
                // b2CreateCapsuleShape( bone.bodyId, &footShapeDef, &capsule );

                b2CreatePolygonShape(bone.bodyId, footShapeDef, footPolygon);

                b2Vec2 pivot = b2Add(new b2Vec2(0.0f, 0.625f * s), position);
                b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.bodyIdB = bone.bodyId;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.5f * B2_PI;
                jointDef.upperAngle = -0.02f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.drawSize = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            // upper right leg
            {
                ref Bone bone = ref human.bones[(int)BoneId.boneId_upperRightLeg];
                bone.parentIndex = (int)BoneId.boneId_hip;

                bodyDef.position = b2Add(new b2Vec2(0.0f, 0.775f * s), position);
                bodyDef.linearDamping = 0.0f;
                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 1.0f;

                if (colorize)
                {
                    shapeDef.customColor = (uint)pantColor;
                }

                b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, -0.125f * s), new b2Vec2(0.0f, 0.125f * s), 0.06f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                b2Vec2 pivot = b2Add(new b2Vec2(0.0f, 0.9f * s), position);
                b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.bodyIdB = bone.bodyId;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.05f * B2_PI;
                jointDef.upperAngle = 0.4f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.drawSize = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            // lower right leg
            {
                ref Bone bone = ref human.bones[(int)BoneId.boneId_lowerRightLeg];
                bone.parentIndex = (int)BoneId.boneId_upperRightLeg;

                bodyDef.position = b2Add(new b2Vec2(0.0f, 0.475f * s), position);
                bodyDef.linearDamping = 0.0f;
                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 0.5f;

                if (colorize)
                {
                    shapeDef.customColor = (uint)pantColor;
                }

                b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, -0.155f * s), new b2Vec2(0.0f, 0.125f * s), 0.045f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                // b2Polygon box = b2MakeOffsetBox(0.1f * s, 0.03f * s, {0.05f * s, -0.175f * s}, 0.0f);
                // b2CreatePolygonShape(bone.bodyId, &shapeDef, &box);

                // capsule = { { -0.02f * s, -0.175f * s }, { 0.13f * s, -0.175f * s }, 0.03f * s };
                // b2CreateCapsuleShape( bone.bodyId, &footShapeDef, &capsule );

                b2CreatePolygonShape(bone.bodyId, footShapeDef, footPolygon);

                b2Vec2 pivot = b2Add(new b2Vec2(0.0f, 0.625f * s), position);
                b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.bodyIdB = bone.bodyId;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.5f * B2_PI;
                jointDef.upperAngle = -0.02f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.drawSize = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            // upper left arm
            {
                ref Bone bone = ref human.bones[(int)BoneId.boneId_upperLeftArm];
                bone.parentIndex = (int)BoneId.boneId_torso;
                bone.frictionScale = 0.5f;

                bodyDef.position = b2Add(new b2Vec2(0.0f, 1.225f * s), position);
                bodyDef.linearDamping = 0.0f;
                bone.bodyId = b2CreateBody(worldId, bodyDef);

                if (colorize)
                {
                    shapeDef.customColor = (uint)shirtColor;
                }

                b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, -0.125f * s), new b2Vec2(0.0f, 0.125f * s), 0.035f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                b2Vec2 pivot = b2Add(new b2Vec2(0.0f, 1.35f * s), position);
                b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.bodyIdB = bone.bodyId;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.1f * B2_PI;
                jointDef.upperAngle = 0.8f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.drawSize = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            // lower left arm
            {
                ref Bone bone = ref human.bones[(int)BoneId.boneId_lowerLeftArm];
                bone.parentIndex = (int)BoneId.boneId_upperLeftArm;

                bodyDef.position = b2Add(new b2Vec2(0.0f, 0.975f * s), position);
                bodyDef.linearDamping = 0.1f;
                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 0.1f;

                if (colorize)
                {
                    shapeDef.customColor = (uint)skinColor;
                }

                b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, -0.125f * s), new b2Vec2(0.0f, 0.125f * s), 0.03f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                b2Vec2 pivot = b2Add(new b2Vec2(0.0f, 1.1f * s), position);
                b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.bodyIdB = bone.bodyId;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                jointDef.referenceAngle = 0.25f * B2_PI;
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.2f * B2_PI;
                jointDef.upperAngle = 0.3f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.drawSize = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            // upper right arm
            {
                ref Bone bone = ref human.bones[(int)BoneId.boneId_upperRightArm];
                bone.parentIndex = (int)BoneId.boneId_torso;

                bodyDef.position = b2Add(new b2Vec2(0.0f, 1.225f * s), position);
                bodyDef.linearDamping = 0.0f;
                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 0.5f;

                if (colorize)
                {
                    shapeDef.customColor = (uint)shirtColor;
                }

                b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, -0.125f * s), new b2Vec2(0.0f, 0.125f * s), 0.035f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                b2Vec2 pivot = b2Add(new b2Vec2(0.0f, 1.35f * s), position);
                b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.bodyIdB = bone.bodyId;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.1f * B2_PI;
                jointDef.upperAngle = 0.8f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.drawSize = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            // lower right arm
            {
                ref Bone bone = ref human.bones[(int)BoneId.boneId_lowerRightArm];
                bone.parentIndex = (int)BoneId.boneId_upperRightArm;

                bodyDef.position = b2Add(new b2Vec2(0.0f, 0.975f * s), position);
                bodyDef.linearDamping = 0.1f;
                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 0.1f;

                if (colorize)
                {
                    shapeDef.customColor = (uint)skinColor;
                }

                b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, -0.125f * s), new b2Vec2(0.0f, 0.125f * s), 0.03f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                b2Vec2 pivot = b2Add(new b2Vec2(0.0f, 1.1f * s), position);
                b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.bodyIdB = bone.bodyId;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                jointDef.referenceAngle = 0.25f * B2_PI;
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.2f * B2_PI;
                jointDef.upperAngle = 0.3f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.drawSize = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            human.isSpawned = true;
        }

        public static void DestroyHuman(Human human)
        {
            Debug.Assert(human.isSpawned == true);

            for (int i = 0; i < (int)BoneId.boneId_count; ++i)
            {
                if (B2_IS_NULL(human.bones[i].jointId))
                {
                    continue;
                }

                b2DestroyJoint(human.bones[i].jointId);
                human.bones[i].jointId = b2_nullJointId;
            }

            for (int i = 0; i < (int)BoneId.boneId_count; ++i)
            {
                if (B2_IS_NULL(human.bones[i].bodyId))
                {
                    continue;
                }

                b2DestroyBody(human.bones[i].bodyId);
                human.bones[i].bodyId = b2_nullBodyId;
            }

            human.isSpawned = false;
        }

        public static void Human_SetVelocity(Human human, b2Vec2 velocity)
        {
            for (int i = 0; i < (int)BoneId.boneId_count; ++i)
            {
                b2BodyId bodyId = human.bones[i].bodyId;

                if (B2_IS_NULL(bodyId))
                {
                    continue;
                }

                b2Body_SetLinearVelocity(bodyId, velocity);
            }
        }

        public static void Human_ApplyRandomAngularImpulse(Human human, float magnitude)
        {
            Debug.Assert(human.isSpawned == true);
            float impulse = RandomFloatRange(-magnitude, magnitude);
            b2Body_ApplyAngularImpulse(human.bones[(int)BoneId.boneId_torso].bodyId, impulse, true);
        }

        public static void Human_SetJointFrictionTorque(Human human, float torque)
        {
            Debug.Assert(human.isSpawned == true);
            if (torque == 0.0f)
            {
                for (int i = 1; i < (int)BoneId.boneId_count; ++i)
                {
                    b2RevoluteJoint_EnableMotor(human.bones[i].jointId, false);
                }
            }
            else
            {
                for (int i = 1; i < (int)BoneId.boneId_count; ++i)
                {
                    b2RevoluteJoint_EnableMotor(human.bones[i].jointId, true);
                    float scale = human.scale * human.bones[i].frictionScale;
                    b2RevoluteJoint_SetMaxMotorTorque(human.bones[i].jointId, scale * torque);
                }
            }
        }

        public static void Human_SetJointSpringHertz(Human human, float hertz)
        {
            Debug.Assert(human.isSpawned == true);
            if (hertz == 0.0f)
            {
                for (int i = 1; i < (int)BoneId.boneId_count; ++i)
                {
                    b2RevoluteJoint_EnableSpring(human.bones[i].jointId, false);
                }
            }
            else
            {
                for (int i = 1; i < (int)BoneId.boneId_count; ++i)
                {
                    b2RevoluteJoint_EnableSpring(human.bones[i].jointId, true);
                    b2RevoluteJoint_SetSpringHertz(human.bones[i].jointId, hertz);
                }
            }
        }

        public static void Human_SetJointDampingRatio(Human human, float dampingRatio)
        {
            Debug.Assert(human.isSpawned == true);
            for (int i = 1; i < (int)BoneId.boneId_count; ++i)
            {
                b2RevoluteJoint_SetSpringDampingRatio(human.bones[i].jointId, dampingRatio);
            }
        }
    }
}
