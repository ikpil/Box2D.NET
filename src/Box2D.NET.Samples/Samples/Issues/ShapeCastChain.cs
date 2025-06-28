// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using Box2D.NET.Shared;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Samples.Samples.Issues;

public struct PhysicsHitQueryResult2D
{
    public B2ShapeId shapeId = b2_nullShapeId;
    public B2Vec2 point = b2Vec2_zero;
    public B2Vec2 normal = b2Vec2_zero;
    public B2Vec2 endPos = b2Vec2_zero;
    public float fraction = 0.0f;
    public bool startPenetrating = false;
    public bool blockingHit = false;

    public PhysicsHitQueryResult2D()
    {
    }
};

public class CastContext_Single
{
    public B2Vec2 startPos;
    public B2Vec2 translation;
    public PhysicsHitQueryResult2D result;
    public bool hit;
};

// This showed a problem with shape casts hitting the back-side of a chain shape
public class ShapeCastChain : Sample
{
    private static readonly int SampleShapeCastChain = SampleFactory.Shared.RegisterSample("Issues", "Shape Cast Chain", Create);

    private B2BodyId characterBodyId_ = b2_nullBodyId;
    private B2Polygon characterBox_;
    private B2Vec2 characterVelocity_ = b2Vec2_zero;
    private B2Vec2 hitPos = b2Vec2_zero;
    private B2Vec2 hitNormal = b2Vec2_zero;

    private static Sample Create(SampleContext context)
    {
        return new ShapeCastChain(context);
    }

    public ShapeCastChain(SampleContext context) : base(context)
    {
        // World Body & Shape
        B2BodyDef worldBodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(m_worldId, ref worldBodyDef);

        B2Vec2[] points = new B2Vec2[]
        {
            new B2Vec2(1.0f, 0.0f),
            new B2Vec2(-1.0f, 0.0f),
            new B2Vec2(-1.0f, -1.0f),
            new B2Vec2(1.0f, -1.0f),
        };

        B2ChainDef worldChainDef = b2DefaultChainDef();
        worldChainDef.userData = null;
        worldChainDef.points = points;
        worldChainDef.count = 4;
        worldChainDef.filter.categoryBits = 0x1;
        worldChainDef.filter.maskBits = 0x1;
        worldChainDef.filter.groupIndex = 0;
        worldChainDef.isLoop = true;
        worldChainDef.enableSensorEvents = false;
        b2CreateChain(groundId, ref worldChainDef);

        // "Character" Body & Shape
        B2BodyDef characterBodyDef = b2DefaultBodyDef();
        characterBodyDef.position = new B2Vec2(0.0f, 0.103f);
        characterBodyDef.rotation = b2MakeRot(0.0f);
        characterBodyDef.linearDamping = 0.0f;
        characterBodyDef.angularDamping = 0.0f;
        characterBodyDef.gravityScale = 1.0f;
        characterBodyDef.enableSleep = true;
        characterBodyDef.isAwake = false;
        characterBodyDef.motionLocks.angularZ = true;
        characterBodyDef.isEnabled = true;
        characterBodyDef.userData = null;
        characterBodyDef.type = B2BodyType.b2_kinematicBody;

        characterBodyId_ = b2CreateBody(m_worldId, ref characterBodyDef);

        B2ShapeDef characterShapeDef = b2DefaultShapeDef();

        characterShapeDef.userData = null;
        characterShapeDef.filter.categoryBits = 0x1;
        characterShapeDef.filter.maskBits = 0x1;
        characterShapeDef.filter.groupIndex = 0;
        characterShapeDef.isSensor = false;
        characterShapeDef.enableSensorEvents = false;
        characterShapeDef.enableContactEvents = false;
        characterShapeDef.enableHitEvents = false;
        characterShapeDef.enablePreSolveEvents = false;
        characterShapeDef.invokeContactCreation = false;
        characterShapeDef.updateBodyMass = false;

        characterBox_ = b2MakeBox(0.1f, 0.1f);
        b2CreatePolygonShape(characterBodyId_, ref characterShapeDef, ref characterBox_);

        context.camera.m_center = b2Vec2_zero;
    }

    public override void Step()
    {
        float timeStep = m_context.settings.hertz > 0.0f ? 1.0f / m_context.settings.hertz : 0.0f;

        bool noXInput = true;
        if (GetKey(Keys.A) == InputAction.Press)
        {
            characterVelocity_.X -= timeStep * 5.0f;
            noXInput = false;
        }

        if (GetKey(Keys.D) == InputAction.Press)
        {
            characterVelocity_.X += timeStep * 5.0f;
            noXInput = false;
        }

        bool noYInput = true;
        if (GetKey(Keys.S) == InputAction.Press)
        {
            characterVelocity_.Y -= timeStep * 5.0f;
            noYInput = false;
        }

        if (GetKey(Keys.W) == InputAction.Press)
        {
            characterVelocity_.Y += timeStep * 5.0f;
            noYInput = false;
        }

        if (noXInput)
        {
            if (b2AbsFloat(characterVelocity_.X) > 0.01f)
            {
                float decel = characterVelocity_.X > 0.0f ? 5.0f : -5.0f;
                if (b2AbsFloat(decel) < characterVelocity_.X)
                {
                    characterVelocity_.X -= decel;
                }
                else
                {
                    characterVelocity_.X = 0.0f;
                }
            }
            else
            {
                characterVelocity_.X = 0.0f;
            }
        }

        if (noYInput)
        {
            if (b2AbsFloat(characterVelocity_.Y) > 0.01f)
            {
                float decel = characterVelocity_.Y > 0.0f ? 5.0f : -5.0f;
                if (b2AbsFloat(decel) < characterVelocity_.Y)
                {
                    characterVelocity_.Y -= decel;
                }
                else
                {
                    characterVelocity_.Y = 0.0f;
                }
            }
            else
            {
                characterVelocity_.Y = 0.0f;
            }
        }

        B2Vec2 characterPos = b2Body_GetPosition(characterBodyId_);
        B2Vec2 newCharacterPos = characterPos;
        newCharacterPos.X += characterVelocity_.X * timeStep;
        newCharacterPos.Y += characterVelocity_.Y * timeStep;
        b2Body_SetTransform(characterBodyId_, newCharacterPos, b2Rot_identity);

        PhysicsHitQueryResult2D hitResult = new PhysicsHitQueryResult2D();
        B2ShapeProxy shapeProxy = b2MakeProxy(characterBox_.vertices.AsSpan(), characterBox_.count, characterBox_.radius);
        if (ShapeCastSingle(ref hitResult, characterPos, newCharacterPos, 0.0f, ref shapeProxy))
        {
            hitPos = hitResult.point;
            hitNormal = hitResult.normal;
        }


        base.Step();
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        m_draw.DrawLine(hitPos, new B2Vec2(hitPos.X + hitNormal.X, hitPos.Y + hitNormal.Y), B2HexColor.b2_colorRed);
    }

    private bool ShapeCastSingle(ref PhysicsHitQueryResult2D outResult, B2Vec2 start, B2Vec2 end, float rotation, ref B2ShapeProxy shape)
    {
        B2Transform transform = new B2Transform(
            start,
            b2MakeRot(rotation)
        );
        B2ShapeProxy transformedShape = TransformShapeProxy(ref transform, ref shape);

        B2Vec2 translation = new B2Vec2(end.X - start.X, end.Y - start.Y);
        B2QueryFilter filter = new B2QueryFilter(0x1, 0x1);
        CastContext_Single context = new CastContext_Single();
        context.startPos = start;
        context.translation = new B2Vec2(translation.X, translation.Y);
        b2World_CastShape(m_worldId, ref transformedShape, translation, filter, b2CastResult_Closest, context);

        if (context.hit)
        {
            outResult = context.result;
            return true;
        }

        return false;
    }

    private static float b2CastResult_Closest(B2ShapeId shapeId, B2Vec2 point, B2Vec2 normal, float fraction, object c)
    {
        CastContext_Single context = c as CastContext_Single;

        if (b2Dot(context!.translation, normal) >= 0.0f)
            return -1.0f;

        context.result.shapeId = shapeId;
        context.result.point = point;
        context.result.normal = normal;
        context.result.endPos = context.startPos + context.translation * fraction;
        context.result.fraction = fraction;
        context.result.blockingHit = true;
        context.hit = true;

        return fraction;
    }

    private static B2ShapeProxy TransformShapeProxy(ref B2Transform t, ref B2ShapeProxy proxy)
    {
        B2ShapeProxy ret = new B2ShapeProxy();
        ret.count = proxy.count;
        ret.radius = proxy.radius;

        for (int i = 0; i < proxy.count; ++i)
        {
            ret.points[i] = b2TransformPoint(ref t, proxy.points[i]);
        }

        return ret;
    }
}