// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2RevoluteJoints;
using static Box2D.NET.Shared.RandomSupports;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Shared
{
    public static class Humans
    {
        public static void CreateHuman(ref Human human, B2WorldId worldId, B2Vec2 position, float scale, float frictionTorque, float hertz, float dampingRatio,
            int groupIndex, object userData, bool colorize)
        {
            B2_ASSERT(human.isSpawned == false);

            for (int i = 0; i < (int)BoneId.bone_count; ++i)
            {
                human.bones[i].bodyId = b2_nullBodyId;
                human.bones[i].jointId = b2_nullJointId;
                human.bones[i].frictionScale = 1.0f;
                human.bones[i].parentIndex = -1;
            }

            human.originalScale = scale;
            human.scale = scale;
            human.frictionTorque = frictionTorque;

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.sleepThreshold = 0.1f;
            bodyDef.userData = userData;

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.friction = 0.2f;
            shapeDef.filter.groupIndex = -groupIndex;
            shapeDef.filter.categoryBits = 2;
            shapeDef.filter.maskBits = (1 | 2);

            B2ShapeDef footShapeDef = shapeDef;
            footShapeDef.material.friction = 0.05f;

            // feet don't collide with ragdolls
            footShapeDef.filter.categoryBits = 2;
            footShapeDef.filter.maskBits = 1;

            if (colorize)
            {
                footShapeDef.material.customColor = (uint)B2HexColor.b2_colorSaddleBrown;
            }

            float s = scale;
            float maxTorque = frictionTorque * s;
            bool enableMotor = true;
            bool enableLimit = true;
            float drawSize = 0.05f;

            B2HexColor shirtColor = B2HexColor.b2_colorMediumTurquoise;
            B2HexColor pantColor = B2HexColor.b2_colorDodgerBlue;

            B2HexColor[] skinColors =
            {
                B2HexColor.b2_colorNavajoWhite, B2HexColor.b2_colorLightYellow, B2HexColor.b2_colorPeru, B2HexColor.b2_colorTan
            };

            B2HexColor skinColor = skinColors[groupIndex % 4];

            // hip
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_hip];
                bone.parentIndex = -1;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 0.95f * s), position);
                bodyDef.linearDamping = 0.0f;
                bodyDef.name = "hip";

                bone.bodyId = b2CreateBody(worldId, bodyDef);

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)pantColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.02f * s), new B2Vec2(0.0f, 0.02f * s), 0.095f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);
            }

            // torso
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_torso];
                bone.parentIndex = (int)BoneId.bone_hip;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 1.2f * s), position);
                bodyDef.linearDamping = 0.0f;
                bodyDef.name = "torso";

                // bodyDef.type = b2_staticBody;
                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 0.5f;
                bodyDef.type = B2BodyType.b2_dynamicBody;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)shirtColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.135f * s), new B2Vec2(0.0f, 0.135f * s), 0.09f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 1.0f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.@base.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.@base.bodyIdB = bone.bodyId;
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.25f * B2_PI;
                jointDef.upperAngle = 0.0f;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.@base.drawScale = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            // head
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_head];
                bone.parentIndex = (int)BoneId.bone_torso;

                bodyDef.position = b2Add(new B2Vec2(0.0f * s, 1.475f * s), position);
                bodyDef.linearDamping = 0.1f;
                bodyDef.name = "head";

                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 0.25f;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)skinColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.038f * s), new B2Vec2(0.0f, 0.039f * s), 0.075f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                //// neck
                // capsule = { { 0.0f, -0.12f * s }, { 0.0f, -0.08f * s }, 0.05f * s };
                // b2CreateCapsuleShape( bone.bodyId, &shapeDef, &capsule );

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 1.4f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.@base.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.@base.bodyIdB = bone.bodyId;
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.3f * B2_PI;
                jointDef.upperAngle = 0.1f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.@base.drawScale = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            // upper left leg
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_upperLeftLeg];
                bone.parentIndex = (int)BoneId.bone_hip;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 0.775f * s), position);
                bodyDef.linearDamping = 0.0f;
                bodyDef.name = "upper_left_leg";

                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 1.0f;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)pantColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.125f * s), new B2Vec2(0.0f, 0.125f * s), 0.06f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 0.9f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.@base.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.@base.bodyIdB = bone.bodyId;
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.05f * B2_PI;
                jointDef.upperAngle = 0.4f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.@base.drawScale = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            Span<B2Vec2> points = stackalloc B2Vec2[4];
            points[0] = new B2Vec2(-0.03f * s, -0.185f * s);
            points[1] = new B2Vec2(0.11f * s, -0.185f * s);
            points[2] = new B2Vec2(0.11f * s, -0.16f * s);
            points[3] = new B2Vec2(-0.03f * s, -0.14f * s);

            B2Hull footHull = b2ComputeHull(points, 4);
            B2Polygon footPolygon = b2MakePolygon(footHull, 0.015f * s);

            // lower left leg
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_lowerLeftLeg];
                bone.parentIndex = (int)BoneId.bone_upperLeftLeg;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 0.475f * s), position);
                bodyDef.linearDamping = 0.0f;
                bodyDef.name = "lower_left_leg";

                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 0.5f;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)pantColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.155f * s), new B2Vec2(0.0f, 0.125f * s), 0.045f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                // b2Polygon box = b2MakeOffsetBox(0.1f * s, 0.03f * s, {0.05f * s, -0.175f * s}, 0.0f);
                // b2CreatePolygonShape(bone.bodyId, &shapeDef, &box);

                // capsule = { { -0.02f * s, -0.175f * s }, { 0.13f * s, -0.175f * s }, 0.03f * s };
                // b2CreateCapsuleShape( bone.bodyId, &footShapeDef, &capsule );

                b2CreatePolygonShape(bone.bodyId, footShapeDef, footPolygon);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 0.625f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.@base.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.@base.bodyIdB = bone.bodyId;
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.5f * B2_PI;
                jointDef.upperAngle = -0.02f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.@base.drawScale = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            // upper right leg
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_upperRightLeg];
                bone.parentIndex = (int)BoneId.bone_hip;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 0.775f * s), position);
                bodyDef.linearDamping = 0.0f;
                bodyDef.name = "upper_right_leg";

                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 1.0f;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)pantColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.125f * s), new B2Vec2(0.0f, 0.125f * s), 0.06f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 0.9f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.@base.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.@base.bodyIdB = bone.bodyId;
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.05f * B2_PI;
                jointDef.upperAngle = 0.4f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.@base.drawScale = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            // lower right leg
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_lowerRightLeg];
                bone.parentIndex = (int)BoneId.bone_upperRightLeg;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 0.475f * s), position);
                bodyDef.linearDamping = 0.0f;
                bodyDef.name = "lower_right_leg";

                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 0.5f;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)pantColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.155f * s), new B2Vec2(0.0f, 0.125f * s), 0.045f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                // b2Polygon box = b2MakeOffsetBox(0.1f * s, 0.03f * s, {0.05f * s, -0.175f * s}, 0.0f);
                // b2CreatePolygonShape(bone.bodyId, &shapeDef, &box);

                // capsule = { { -0.02f * s, -0.175f * s }, { 0.13f * s, -0.175f * s }, 0.03f * s };
                // b2CreateCapsuleShape( bone.bodyId, &footShapeDef, &capsule );

                b2CreatePolygonShape(bone.bodyId, footShapeDef, footPolygon);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 0.625f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.@base.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.@base.bodyIdB = bone.bodyId;
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.5f * B2_PI;
                jointDef.upperAngle = -0.02f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.@base.drawScale = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            // upper left arm
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_upperLeftArm];
                bone.parentIndex = (int)BoneId.bone_torso;
                bone.frictionScale = 0.5f;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 1.225f * s), position);
                bodyDef.linearDamping = 0.0f;
                bodyDef.name = "upper_left_arm";

                bone.bodyId = b2CreateBody(worldId, bodyDef);

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)shirtColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.125f * s), new B2Vec2(0.0f, 0.125f * s), 0.035f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 1.35f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.@base.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.@base.bodyIdB = bone.bodyId;
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.1f * B2_PI;
                jointDef.upperAngle = 0.8f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.@base.drawScale = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            // lower left arm
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_lowerLeftArm];
                bone.parentIndex = (int)BoneId.bone_upperLeftArm;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 0.975f * s), position);
                bodyDef.linearDamping = 0.1f;
                bodyDef.name = "lower_left_arm";

                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 0.1f;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)skinColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.125f * s), new B2Vec2(0.0f, 0.125f * s), 0.03f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 1.1f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.@base.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.@base.bodyIdB = bone.bodyId;
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
                jointDef.@base.localFrameA.q = b2MakeRot(0.25f * B2_PI);
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.2f * B2_PI;
                jointDef.upperAngle = 0.3f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.@base.drawScale = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            // upper right arm
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_upperRightArm];
                bone.parentIndex = (int)BoneId.bone_torso;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 1.225f * s), position);
                bodyDef.linearDamping = 0.0f;
                bodyDef.name = "upper_right_arm";

                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 0.5f;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)shirtColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.125f * s), new B2Vec2(0.0f, 0.125f * s), 0.035f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 1.35f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.@base.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.@base.bodyIdB = bone.bodyId;
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.1f * B2_PI;
                jointDef.upperAngle = 0.8f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.@base.drawScale = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            // lower right arm
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_lowerRightArm];
                bone.parentIndex = (int)BoneId.bone_upperRightArm;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 0.975f * s), position);
                bodyDef.linearDamping = 0.1f;
                bodyDef.name = "lower_right_arm";

                bone.bodyId = b2CreateBody(worldId, bodyDef);
                bone.frictionScale = 0.1f;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)skinColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.125f * s), new B2Vec2(0.0f, 0.125f * s), 0.03f * s);
                b2CreateCapsuleShape(bone.bodyId, shapeDef, capsule);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 1.1f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.@base.bodyIdA = human.bones[bone.parentIndex].bodyId;
                jointDef.@base.bodyIdB = bone.bodyId;
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
                jointDef.@base.localFrameA.q = b2MakeRot(0.25f * B2_PI);
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
                jointDef.enableLimit = enableLimit;
                jointDef.lowerAngle = -0.2f * B2_PI;
                jointDef.upperAngle = 0.3f * B2_PI;
                jointDef.enableMotor = enableMotor;
                jointDef.maxMotorTorque = bone.frictionScale * maxTorque;
                jointDef.enableSpring = hertz > 0.0f;
                jointDef.hertz = hertz;
                jointDef.dampingRatio = dampingRatio;
                jointDef.@base.drawScale = drawSize;

                bone.jointId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            human.isSpawned = true;
        }

        public static void DestroyHuman(ref Human human)
        {
            B2_ASSERT(human.isSpawned == true);

            for (int i = 0; i < (int)BoneId.bone_count; ++i)
            {
                if (B2_IS_NULL(human.bones[i].jointId))
                {
                    continue;
                }

                b2DestroyJoint(human.bones[i].jointId, false);
                human.bones[i].jointId = b2_nullJointId;
            }

            for (int i = 0; i < (int)BoneId.bone_count; ++i)
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

        public static void Human_SetVelocity(ref Human human, B2Vec2 velocity)
        {
            for (int i = 0; i < (int)BoneId.bone_count; ++i)
            {
                B2BodyId bodyId = human.bones[i].bodyId;

                if (B2_IS_NULL(bodyId))
                {
                    continue;
                }

                b2Body_SetLinearVelocity(bodyId, velocity);
            }
        }

        public static void Human_ApplyRandomAngularImpulse(ref Human human, float magnitude)
        {
            B2_ASSERT(human.isSpawned == true);
            float impulse = RandomFloatRange(-magnitude, magnitude);
            b2Body_ApplyAngularImpulse(human.bones[(int)BoneId.bone_torso].bodyId, impulse, true);
        }

        public static void Human_SetJointFrictionTorque(ref Human human, float torque)
        {
            B2_ASSERT(human.isSpawned == true);
            if (torque == 0.0f)
            {
                for (int i = 1; i < (int)BoneId.bone_count; ++i)
                {
                    b2RevoluteJoint_EnableMotor(human.bones[i].jointId, false);
                }
            }
            else
            {
                for (int i = 1; i < (int)BoneId.bone_count; ++i)
                {
                    b2RevoluteJoint_EnableMotor(human.bones[i].jointId, true);
                    float scale = human.scale * human.bones[i].frictionScale;
                    b2RevoluteJoint_SetMaxMotorTorque(human.bones[i].jointId, scale * torque);
                }
            }
        }

        public static void Human_SetJointSpringHertz(ref Human human, float hertz)
        {
            B2_ASSERT(human.isSpawned == true);
            if (hertz == 0.0f)
            {
                for (int i = 1; i < (int)BoneId.bone_count; ++i)
                {
                    b2RevoluteJoint_EnableSpring(human.bones[i].jointId, false);
                }
            }
            else
            {
                for (int i = 1; i < (int)BoneId.bone_count; ++i)
                {
                    b2RevoluteJoint_EnableSpring(human.bones[i].jointId, true);
                    b2RevoluteJoint_SetSpringHertz(human.bones[i].jointId, hertz);
                }
            }
        }

        public static void Human_SetJointDampingRatio(ref Human human, float dampingRatio)
        {
            B2_ASSERT(human.isSpawned == true);
            for (int i = 1; i < (int)BoneId.bone_count; ++i)
            {
                b2RevoluteJoint_SetSpringDampingRatio(human.bones[i].jointId, dampingRatio);
            }
        }

        public static void Human_EnableSensorEvents(ref Human human, bool enable)
        {
            B2_ASSERT(human.isSpawned == true);
            B2BodyId bodyId = human.bones[(int)BoneId.bone_torso].bodyId;

            B2FixedArray1<B2ShapeId> shapeIdBuffer = new B2FixedArray1<B2ShapeId>();
            Span<B2ShapeId> shapeId = shapeIdBuffer.AsSpan();
            int count = b2Body_GetShapes(bodyId, shapeId, 1);
            if (count == 1)
            {
                b2Shape_EnableSensorEvents(shapeId[0], enable);
            }
        }

        public static void Human_SetScale(ref Human human, float scale)
        {
            B2_ASSERT(human.isSpawned == true);
            B2_ASSERT(0.01f < scale && scale < 100.0f);
            B2_ASSERT(0.0f < human.scale);

            float ratio = scale / human.scale;

            // Torque scales by pow(length, 4) due to mass change and length change. However, gravity is also a factor
            // so I'm using pow(length, 3)
            float originalRatio = scale / human.originalScale;
            float frictionTorque = (originalRatio * originalRatio * originalRatio) * human.frictionTorque;

            B2Vec2 origin = b2Body_GetPosition(human.bones[0].bodyId);

            for (int boneIndex = 0; boneIndex < (int)BoneId.bone_count; ++boneIndex)
            {
                ref Bone bone = ref human.bones[boneIndex];

                if (boneIndex > 0)
                {
                    B2Transform transform = b2Body_GetTransform(bone.bodyId);
                    transform.p = b2MulAdd(origin, ratio, b2Sub(transform.p, origin));
                    b2Body_SetTransform(bone.bodyId, transform.p, transform.q);

                    B2Transform localFrameA = b2Joint_GetLocalFrameA(bone.jointId);
                    B2Transform localFrameB = b2Joint_GetLocalFrameB(bone.jointId);
                    localFrameA.p = b2MulSV(ratio, localFrameA.p);
                    localFrameB.p = b2MulSV(ratio, localFrameB.p);
                    b2Joint_SetLocalFrameA(bone.jointId, localFrameA);
                    b2Joint_SetLocalFrameB(bone.jointId, localFrameB);

                    B2JointType type = b2Joint_GetType(bone.jointId);
                    if (type == B2JointType.b2_revoluteJoint)
                    {
                        b2RevoluteJoint_SetMaxMotorTorque(bone.jointId, bone.frictionScale * frictionTorque);
                    }
                }

                B2ShapeId[] shapeIds = new B2ShapeId[2];
                int shapeCount = b2Body_GetShapes(bone.bodyId, shapeIds, 2);
                for (int shapeIndex = 0; shapeIndex < shapeCount; ++shapeIndex)
                {
                    B2ShapeType type = b2Shape_GetType(shapeIds[shapeIndex]);
                    if (type == B2ShapeType.b2_capsuleShape)
                    {
                        B2Capsule capsule = b2Shape_GetCapsule(shapeIds[shapeIndex]);
                        capsule.center1 = b2MulSV(ratio, capsule.center1);
                        capsule.center2 = b2MulSV(ratio, capsule.center2);
                        capsule.radius *= ratio;
                        b2Shape_SetCapsule(shapeIds[shapeIndex], ref capsule);
                    }
                    else if (type == B2ShapeType.b2_polygonShape)
                    {
                        B2Polygon polygon = b2Shape_GetPolygon(shapeIds[shapeIndex]);
                        for (int pointIndex = 0; pointIndex < polygon.count; ++pointIndex)
                        {
                            polygon.vertices[pointIndex] = b2MulSV(ratio, polygon.vertices[pointIndex]);
                        }

                        polygon.centroid = b2MulSV(ratio, polygon.centroid);
                        polygon.radius *= ratio;

                        b2Shape_SetPolygon(shapeIds[shapeIndex], ref polygon);
                    }
                }

                b2Body_ApplyMassFromShapes(bone.bodyId);
            }

            human.scale = scale;
        }
    }
}