// SPDX-FileCopyrightText: 2022 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Numerics;
using Box2D.NET.Samples.Helpers;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Movers;

namespace Box2D.NET.Samples.Samples.Characters;

public class Mover : Sample
{
    private static int SampleMover = SampleFactory.Shared.RegisterSample("Character", "Mover", Create);

    public class ShapeUserData
    {
        public float maxPush;
        public bool clipVelocity;
    }

    public class CastResult
    {
        public B2Vec2 point;
        public float fraction;
        public bool hit;
    }

    private const ulong StaticBit = 0x0001;
    private const ulong MoverBit = 0x0002;
    private const ulong DynamicBit = 0x0004;
    private const ulong AllBits = ~0u;

    private const int m_planeCapacity = 8;
    private const float m_jumpSpeed = 10.0f;
    private const float m_maxSpeed = 4.0f;
    private const float m_minSpeed = 0.01f;
    private const float m_stopSpeed = 1.0f;
    private const float m_accelerate = 20.0f;
    private const float m_friction = 2.0f;
    private const float m_gravity = 15.0f;

    private B2Transform m_transform;
    private B2Vec2 m_velocity;
    private B2Capsule m_capsule;
    private ShapeUserData m_friendlyShape = new ShapeUserData();
    private B2CollisionPlane[] m_planes = new B2CollisionPlane[m_planeCapacity];
    private int m_planeCount;
    private int m_totalIterations;
    private float m_pogoVelocity;
    private bool m_onGround;

    private int m_deltaX;
    private int m_deltaY;

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new Mover(ctx, settings);
    }

    private static float CastCallback(B2ShapeId shapeId, B2Vec2 point, B2Vec2 normal, float fraction, object context)
    {
        CastResult result = (CastResult)context;
        result.point = point;
        result.fraction = fraction;
        result.hit = true;
        return fraction;
    }

    public Mover(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(20.0f, 9.0f);
            m_context.camera.m_zoom = 10.0f;
        }

        m_transform = new B2Transform(new B2Vec2(2.0f, 8.0f), b2Rot_identity);

        m_velocity = new B2Vec2(0.0f, 0.0f);
        m_capsule = new B2Capsule(new B2Vec2(0.0f, -0.5f), new B2Vec2(0.0f, 0.5f), 0.3f);

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, 0.0f);
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            const string path =
                "M 2.6458333,201.08333 H 293.68751 l -10e-6,-55.5625 h -2.64584 l 2e-5,52.91667 h -23.8125 l -39.68751,-13.22917 "
                + "h -26.45833 l -23.8125,10.58333 H 142.875 v -5.29167 h -5.29166 v 5.29167 H 119.0625 v -2.64583 h -2.64583 v "
                + "-2.64584 h -2.64584 v -2.64583 H 111.125 v -2.64583 H 84.666668 v -2.64583 h -5.291666 v -2.64584 h -5.291667 v "
                + "-2.64583 H 68.791668 V 174.625 h -5.291666 v -2.64584 H 52.916669 L 39.6875,177.27083 H 34.395833 L "
                + "23.8125,185.20833 H 15.875 L 5.2916669,187.85416 V 153.45833 H 2.6458333 v 47.625";

            B2Vec2[] points = new B2Vec2[64];

            B2Vec2 offset = new B2Vec2(-50.0f, -200.0f);
            float scale = 0.2f;

            int count = SvgParser.ParsePath(path, offset, points, 64, scale, false);

            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = count;
            chainDef.isLoop = true;

            b2CreateChain(groundId, ref chainDef);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(32.0f, 4.0f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            m_friendlyShape.maxPush = 0.05f;
            m_friendlyShape.clipVelocity = false;

            shapeDef.filter = new B2Filter(MoverBit, AllBits, 0);
            shapeDef.userData = m_friendlyShape;
            B2BodyId body = b2CreateBody(m_worldId, ref bodyDef);
            b2CreateCapsuleShape(body, ref shapeDef, ref m_capsule);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(7.0f, 7.0f);
            B2BodyId body = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.filter = new B2Filter(DynamicBit, AllBits, 0);
            B2Circle circle = new B2Circle(b2Vec2_zero, 0.5f);
            b2CreateCircleShape(body, ref shapeDef, ref circle);
        }

        m_totalIterations = 0;
        m_pogoVelocity = 0.0f;
        m_onGround = false;

        m_deltaX = 0;
        m_deltaY = 0;
        m_planeCount = 0;
    }

    // https://github.com/id-Software/Quake/blob/master/QW/client/pmove.c#L390
    void SolveMove(float timeStep, float throttle)
    {
        // Friction
        float speed = b2Length(m_velocity);
        if (speed < m_minSpeed)
        {
            m_velocity.X = 0.0f;
            m_velocity.Y = 0.0f;
        }
        else
        {
            // Linear damping above stopSpeed and fixed reduction below stopSpeed
            float control = speed < m_stopSpeed ? m_stopSpeed : speed;

            // friction has units of 1/time
            float drop = control * m_friction * timeStep;
            float newSpeed = b2MaxFloat(0.0f, speed - drop);
            m_velocity *= newSpeed / speed;
        }

        B2Vec2 desiredVelocity = new B2Vec2(m_maxSpeed * throttle, 0.0f);
        float desiredSpeed = 0;
        B2Vec2 desiredDirection = b2GetLengthAndNormalize(ref desiredSpeed, desiredVelocity);

        if (desiredSpeed > m_maxSpeed)
        {
            desiredSpeed = m_maxSpeed;
        }

        if (m_onGround)
        {
            m_velocity.Y = 0.0f;
        }

        // Accelerate
        float currentSpeed = b2Dot(m_velocity, desiredDirection);
        float addSpeed = desiredSpeed - currentSpeed;
        if (addSpeed > 0.0f)
        {
            float accelSpeed = m_accelerate * m_maxSpeed * timeStep;
            if (accelSpeed > addSpeed)
            {
                accelSpeed = addSpeed;
            }

            m_velocity += accelSpeed * desiredDirection;
        }

        m_velocity.Y -= m_gravity * timeStep;

        float pogoRestLength = 3.0f * m_capsule.radius;
        float rayLength = pogoRestLength + m_capsule.radius;
        B2Circle circle = new B2Circle(b2TransformPoint(ref m_transform, m_capsule.center1), 0.5f * m_capsule.radius);
        B2Vec2 translation = new B2Vec2(0.0f, -rayLength + circle.radius);
        B2QueryFilter skipTeamFilter = new B2QueryFilter(1, ~2u);
        CastResult result = new CastResult();
        b2World_CastCircle(m_worldId, ref circle, translation, skipTeamFilter, CastCallback, result);

        if (result.hit == false)
        {
            m_onGround = false;
            m_pogoVelocity = 0.0f;

            m_context.draw.DrawSegment(circle.center, circle.center + translation, B2HexColor.b2_colorGray);
            m_context.draw.DrawCircle(circle.center + translation, circle.radius, B2HexColor.b2_colorGray);
        }
        else
        {
            m_onGround = true;
            float pogoCurrentLength = result.fraction * rayLength;

            float zeta = 0.7f;
            float hertz = 6.0f;
            float omega = 2.0f * B2_PI * hertz;
            float omegaH = omega * timeStep;

            m_pogoVelocity = (m_pogoVelocity - omega * omegaH * (pogoCurrentLength - pogoRestLength)) /
                             (1.0f + 2.0f * zeta * omegaH + omegaH * omegaH);

            B2Vec2 delta = result.fraction * translation;
            m_context.draw.DrawSegment(circle.center, circle.center + delta, B2HexColor.b2_colorPlum);
            m_context.draw.DrawCircle(circle.center + delta, circle.radius, B2HexColor.b2_colorPlum);
        }

        B2Vec2 target = m_transform.p + timeStep * m_velocity + timeStep * m_pogoVelocity * new B2Vec2(0.0f, 1.0f);

        // Movers collide with every thing
        B2QueryFilter collideFilter = new B2QueryFilter(MoverBit, AllBits);

        // Movers don't sweep against other movers, allows for soft collision
        B2QueryFilter castFilter = new B2QueryFilter(MoverBit, StaticBit | DynamicBit);

        m_totalIterations = 0;
        float tolerance = 0.01f;

        for (int iteration = 0; iteration < 5; ++iteration)
        {
            m_planeCount = 0;

            B2Capsule mover;
            mover.center1 = b2TransformPoint(ref m_transform, m_capsule.center1);
            mover.center2 = b2TransformPoint(ref m_transform, m_capsule.center2);
            mover.radius = m_capsule.radius;

            b2World_CollideMover(m_worldId, ref mover, collideFilter, PlaneResultFcn, this);
            B2PlaneSolverResult solverResult = b2SolvePlanes(target, m_planes, m_planeCount);

            m_totalIterations += solverResult.iterationCount;

            B2Vec2 moverTranslation = solverResult.position - m_transform.p;

            float fraction = b2World_CastMover(m_worldId, ref mover, moverTranslation, castFilter);

            B2Vec2 delta = fraction * moverTranslation;
            m_transform.p += delta;

            if (b2LengthSquared(delta) < tolerance * tolerance)
            {
                break;
            }
        }

        m_velocity = b2ClipVector(m_velocity, m_planes, m_planeCount);
    }
    
#if FALSE
    public override void UpdateGui()
    {
        ImGui.SetNextWindowPos(new Vector2(10.0f, 600.0f));
        ImGui.SetNextWindowSize(new Vector2(240.0f, 80.0f));
        ImGui.Begin("Mover", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        ImGui.End();
    }
#endif

    static bool PlaneResultFcn(B2ShapeId shapeId, ref B2PlaneResult planeResult, object context)
    {
        Debug.Assert(planeResult.hit == true);

        Mover self = (Mover)context;
        float maxPush = float.MaxValue;
        bool clipVelocity = true;
        ShapeUserData userData = (ShapeUserData)b2Shape_GetUserData(shapeId);
        if (userData != null)
        {
            maxPush = userData.maxPush;
            clipVelocity = userData.clipVelocity;
        }

        if (self.m_planeCount < m_planeCapacity)
        {
            Debug.Assert(b2IsValidPlane(planeResult.plane));
            self.m_planes[self.m_planeCount] = new B2CollisionPlane(planeResult.plane, maxPush, 0.0f, clipVelocity);
            self.m_planeCount += 1;
        }

        return true;
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        float throttle = 0.0f;

        if (InputAction.Press == GetKey(Keys.A))
        {
            throttle -= 1.0f;
        }

        if (InputAction.Press == GetKey(Keys.D))
        {
            throttle += 1.0f;
        }

        if (InputAction.Press == GetKey(Keys.Space) && m_onGround == true)
        {
            m_velocity.Y = m_jumpSpeed;
            m_onGround = false;
        }

        float timeStep = settings.hertz > 0.0f ? 1.0f / settings.hertz : 0.0f;

        // throttle = { 0.0f, 0.0f, -1.0f };

        SolveMove(timeStep, throttle);
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        int count = m_planeCount;
        for (int i = 0; i < count; ++i)
        {
            B2Plane plane = m_planes[i].plane;
            B2Vec2 p1 = m_transform.p + (plane.offset - m_capsule.radius) * plane.normal;
            B2Vec2 p2 = p1 + 0.1f * plane.normal;
            m_context.draw.DrawPoint(p1, 5.0f, B2HexColor.b2_colorYellow);
            m_context.draw.DrawSegment(p1, p2, B2HexColor.b2_colorYellow);
        }

        {
            B2Vec2 p1 = b2TransformPoint(ref m_transform, m_capsule.center1);
            B2Vec2 p2 = b2TransformPoint(ref m_transform, m_capsule.center2);
            m_context.draw.DrawSolidCapsule(p1, p2, m_capsule.radius, B2HexColor.b2_colorOrange);
            m_context.draw.DrawSegment(m_transform.p, m_transform.p + m_velocity, B2HexColor.b2_colorPurple);
        }

        B2Vec2 p = m_transform.p;
        DrawTextLine($"position {p.X:F2} {p.Y:F2}");
        DrawTextLine($"velocity {m_velocity.X:F2}, {m_velocity.Y:F2}");
        DrawTextLine($"iterations {m_totalIterations}");
        DrawTextLine($"deltaX = {m_deltaX}, deltaY = {m_deltaY}");

        m_context.camera.m_center.X = m_transform.p.X;
    }
}
