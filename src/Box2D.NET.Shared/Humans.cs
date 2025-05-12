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
using static Box2D.NET.B2Cores;

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

            human.scale = scale;

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
                
                bone.bodyId = b2CreateBody(worldId, ref bodyDef);

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)pantColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.02f * s), new B2Vec2(0.0f, 0.02f * s), 0.095f * s);
                b2CreateCapsuleShape(bone.bodyId, ref shapeDef, ref capsule);
            }

            // torso
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_torso];
                bone.parentIndex = (int)BoneId.bone_hip;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 1.2f * s), position);
                bodyDef.linearDamping = 0.0f;
                bodyDef.name = "torso";
                
                // bodyDef.type = b2_staticBody;
                bone.bodyId = b2CreateBody(worldId, ref bodyDef);
                bone.frictionScale = 0.5f;
                bodyDef.type = B2BodyType.b2_dynamicBody;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)shirtColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.135f * s), new B2Vec2(0.0f, 0.135f * s), 0.09f * s);
                b2CreateCapsuleShape(bone.bodyId, ref shapeDef, ref capsule);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 1.0f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
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

                bone.jointId = b2CreateRevoluteJoint(worldId, ref jointDef);
            }

            // head
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_head];
                bone.parentIndex = (int)BoneId.bone_torso;

                bodyDef.position = b2Add(new B2Vec2(0.0f * s, 1.475f * s), position);
                bodyDef.linearDamping = 0.1f;
                bodyDef.name = "head";

                bone.bodyId = b2CreateBody(worldId, ref bodyDef);
                bone.frictionScale = 0.25f;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)skinColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.038f * s), new B2Vec2(0.0f, 0.039f * s), 0.075f * s);
                b2CreateCapsuleShape(bone.bodyId, ref shapeDef, ref capsule);

                //// neck
                // capsule = { { 0.0f, -0.12f * s }, { 0.0f, -0.08f * s }, 0.05f * s };
                // b2CreateCapsuleShape( bone.bodyId, &shapeDef, &capsule );

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 1.4f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
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

                bone.jointId = b2CreateRevoluteJoint(worldId, ref jointDef);
            }

            // upper left leg
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_upperLeftLeg];
                bone.parentIndex = (int)BoneId.bone_hip;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 0.775f * s), position);
                bodyDef.linearDamping = 0.0f;
                bodyDef.name = "upper_left_leg";
                
                bone.bodyId = b2CreateBody(worldId, ref bodyDef);
                bone.frictionScale = 1.0f;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)pantColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.125f * s), new B2Vec2(0.0f, 0.125f * s), 0.06f * s);
                b2CreateCapsuleShape(bone.bodyId, ref shapeDef, ref capsule);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 0.9f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
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

                bone.jointId = b2CreateRevoluteJoint(worldId, ref jointDef);
            }

            Span<B2Vec2> points = stackalloc B2Vec2[4];
            points[0] = new B2Vec2(-0.03f * s, -0.185f * s);
            points[1] = new B2Vec2(0.11f * s, -0.185f * s);
            points[2] = new B2Vec2(0.11f * s, -0.16f * s);
            points[3] = new B2Vec2(-0.03f * s, -0.14f * s);

            B2Hull footHull = b2ComputeHull(points, 4);
            B2Polygon footPolygon = b2MakePolygon(ref footHull, 0.015f * s);

            // lower left leg
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_lowerLeftLeg];
                bone.parentIndex = (int)BoneId.bone_upperLeftLeg;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 0.475f * s), position);
                bodyDef.linearDamping = 0.0f;
                bodyDef.name = "lower_left_leg";
                
                bone.bodyId = b2CreateBody(worldId, ref bodyDef);
                bone.frictionScale = 0.5f;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)pantColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.155f * s), new B2Vec2(0.0f, 0.125f * s), 0.045f * s);
                b2CreateCapsuleShape(bone.bodyId, ref shapeDef, ref capsule);

                // b2Polygon box = b2MakeOffsetBox(0.1f * s, 0.03f * s, {0.05f * s, -0.175f * s}, 0.0f);
                // b2CreatePolygonShape(bone.bodyId, &shapeDef, &box);

                // capsule = { { -0.02f * s, -0.175f * s }, { 0.13f * s, -0.175f * s }, 0.03f * s };
                // b2CreateCapsuleShape( bone.bodyId, &footShapeDef, &capsule );

                b2CreatePolygonShape(bone.bodyId, ref footShapeDef, ref footPolygon);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 0.625f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
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

                bone.jointId = b2CreateRevoluteJoint(worldId, ref jointDef);
            }

            // upper right leg
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_upperRightLeg];
                bone.parentIndex = (int)BoneId.bone_hip;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 0.775f * s), position);
                bodyDef.linearDamping = 0.0f;
                bodyDef.name = "upper_right_leg";
                
                bone.bodyId = b2CreateBody(worldId, ref bodyDef);
                bone.frictionScale = 1.0f;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)pantColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.125f * s), new B2Vec2(0.0f, 0.125f * s), 0.06f * s);
                b2CreateCapsuleShape(bone.bodyId, ref shapeDef, ref capsule);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 0.9f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
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

                bone.jointId = b2CreateRevoluteJoint(worldId, ref jointDef);
            }

            // lower right leg
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_lowerRightLeg];
                bone.parentIndex = (int)BoneId.bone_upperRightLeg;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 0.475f * s), position);
                bodyDef.linearDamping = 0.0f;
                bodyDef.name = "lower_right_leg";
                
                bone.bodyId = b2CreateBody(worldId, ref bodyDef);
                bone.frictionScale = 0.5f;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)pantColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.155f * s), new B2Vec2(0.0f, 0.125f * s), 0.045f * s);
                b2CreateCapsuleShape(bone.bodyId, ref shapeDef, ref capsule);

                // b2Polygon box = b2MakeOffsetBox(0.1f * s, 0.03f * s, {0.05f * s, -0.175f * s}, 0.0f);
                // b2CreatePolygonShape(bone.bodyId, &shapeDef, &box);

                // capsule = { { -0.02f * s, -0.175f * s }, { 0.13f * s, -0.175f * s }, 0.03f * s };
                // b2CreateCapsuleShape( bone.bodyId, &footShapeDef, &capsule );

                b2CreatePolygonShape(bone.bodyId, ref footShapeDef, ref footPolygon);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 0.625f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
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

                bone.jointId = b2CreateRevoluteJoint(worldId, ref jointDef);
            }

            // upper left arm
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_upperLeftArm];
                bone.parentIndex = (int)BoneId.bone_torso;
                bone.frictionScale = 0.5f;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 1.225f * s), position);
                bodyDef.linearDamping = 0.0f;
                bodyDef.name = "upper_left_arm";
                
                bone.bodyId = b2CreateBody(worldId, ref bodyDef);

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)shirtColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.125f * s), new B2Vec2(0.0f, 0.125f * s), 0.035f * s);
                b2CreateCapsuleShape(bone.bodyId, ref shapeDef, ref capsule);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 1.35f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
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

                bone.jointId = b2CreateRevoluteJoint(worldId, ref jointDef);
            }

            // lower left arm
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_lowerLeftArm];
                bone.parentIndex = (int)BoneId.bone_upperLeftArm;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 0.975f * s), position);
                bodyDef.linearDamping = 0.1f;
                bodyDef.name = "lower_left_arm";
                
                bone.bodyId = b2CreateBody(worldId, ref bodyDef);
                bone.frictionScale = 0.1f;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)skinColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.125f * s), new B2Vec2(0.0f, 0.125f * s), 0.03f * s);
                b2CreateCapsuleShape(bone.bodyId, ref shapeDef, ref capsule);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 1.1f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
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

                bone.jointId = b2CreateRevoluteJoint(worldId, ref jointDef);
            }

            // upper right arm
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_upperRightArm];
                bone.parentIndex = (int)BoneId.bone_torso;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 1.225f * s), position);
                bodyDef.linearDamping = 0.0f;
                bodyDef.name = "upper_right_arm";
                
                bone.bodyId = b2CreateBody(worldId, ref bodyDef);
                bone.frictionScale = 0.5f;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)shirtColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.125f * s), new B2Vec2(0.0f, 0.125f * s), 0.035f * s);
                b2CreateCapsuleShape(bone.bodyId, ref shapeDef, ref capsule);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 1.35f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
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

                bone.jointId = b2CreateRevoluteJoint(worldId, ref jointDef);
            }

            // lower right arm
            {
                ref Bone bone = ref human.bones[(int)BoneId.bone_lowerRightArm];
                bone.parentIndex = (int)BoneId.bone_upperRightArm;

                bodyDef.position = b2Add(new B2Vec2(0.0f, 0.975f * s), position);
                bodyDef.linearDamping = 0.1f;
                bodyDef.name = "lower_right_arm";
                
                bone.bodyId = b2CreateBody(worldId, ref bodyDef);
                bone.frictionScale = 0.1f;

                if (colorize)
                {
                    shapeDef.material.customColor = (uint)skinColor;
                }

                B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.125f * s), new B2Vec2(0.0f, 0.125f * s), 0.03f * s);
                b2CreateCapsuleShape(bone.bodyId, ref shapeDef, ref capsule);

                B2Vec2 pivot = b2Add(new B2Vec2(0.0f, 1.1f * s), position);
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
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

                bone.jointId = b2CreateRevoluteJoint(worldId, ref jointDef);
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

                b2DestroyJoint(human.bones[i].jointId);
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
    }
}