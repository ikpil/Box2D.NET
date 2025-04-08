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
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Joints;

namespace Box2D.NET.Samples.Samples.Characters;

public class Mover : Sample
{
    private static int SampleMover = SampleFactory.Shared.RegisterSample("Character", "Mover", Create);

    public class ShapeUserData
    {
        public float maxPush;
        public bool clipVelocity;
    }

    private const ulong StaticBit = 0x0001;
    private const ulong MoverBit = 0x0002;
    private const ulong DynamicBit = 0x0004;
    private const ulong AllBits = ~0u;

    public enum PogoShape
    {
        PogoPoint,
        PogoCircle,
        PogoBox
    }

    public class CastResult
    {
        public B2Vec2 point;
        public B2BodyId bodyId;
        public float fraction;
        public bool hit;
    }

    private const int m_planeCapacity = 8;

    private float m_jumpSpeed = 10.0f;
    private float m_maxSpeed = 6.0f;
    private float m_minSpeed = 0.1f;
    private float m_stopSpeed = 3.0f;
    private float m_accelerate = 20.0f;
    private float m_airSteer = 0.2f;
    private float m_friction = 8.0f;
    private float m_gravity = 30.0f;
    private float m_pogoHertz = 5.0f;
    private float m_pogoDampingRatio = 0.8f;

    private int m_pogoShape = (int)PogoShape.PogoBox;

    private B2Transform m_transform;
    private B2Vec2 m_velocity;
    private B2Capsule m_capsule;
    private ShapeUserData m_friendlyShape = new ShapeUserData();
    private B2CollisionPlane[] m_planes = new B2CollisionPlane[m_planeCapacity];
    private int m_planeCount;
    private int m_totalIterations;
    private float m_pogoVelocity;
    private bool m_onGround;
    private bool m_lockCamera;

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
        result.bodyId = b2Shape_GetBody(shapeId);
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

        settings.drawJoints = false;
        m_transform = new B2Transform(new B2Vec2(2.0f, 8.0f), b2Rot_identity);
        m_velocity = new B2Vec2(0.0f, 0.0f);
        m_capsule = new B2Capsule(new B2Vec2(0.0f, -0.5f), new B2Vec2(0.0f, 0.5f), 0.3f);

        B2BodyId groundId1;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, 0.0f);
            groundId1 = b2CreateBody(m_worldId, ref bodyDef);

            const string path =
                "M 2.6458333,201.08333 H 293.68751 v -47.625 h -2.64584 l -10.58333,7.9375 -13.22916,7.9375 -13.24648,5.29167 "
                + "-31.73269,7.9375 -21.16667,2.64583 -23.8125,10.58333 H 142.875 v -5.29167 h -5.29166 v 5.29167 H 119.0625 v "
                + "-2.64583 h -2.64583 v -2.64584 h -2.64584 v -2.64583 H 111.125 v -2.64583 H 84.666668 v -2.64583 h -5.291666 v "
                + "-2.64584 h -5.291667 v -2.64583 H 68.791668 V 174.625 h -5.291666 v -2.64584 H 52.916669 L 39.6875,177.27083 H "
                + "34.395833 L 23.8125,185.20833 H 15.875 L 5.2916669,187.85416 V 153.45833 H 2.6458333 v 47.625";


            B2Vec2[] points = new B2Vec2[64];

            B2Vec2 offset = new B2Vec2(-50.0f, -200.0f);
            float scale = 0.2f;

            int count = SvgParser.ParsePath(path, offset, points, 64, scale, false);

            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = count;
            chainDef.isLoop = true;

            b2CreateChain(groundId1, ref chainDef);
        }

        B2BodyId groundId2;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(98.0f, 0.0f);
            groundId2 = b2CreateBody(m_worldId, ref bodyDef);

            const string path =
                "M 2.6458333,201.08333 H 293.68751 l 0,-23.8125 h -23.8125 l 21.16667,21.16667 h -23.8125 l -39.68751,-13.22917 "
                + "-26.45833,7.9375 -23.8125,2.64583 h -13.22917 l -0.0575,2.64584 h -5.29166 v -2.64583 l -7.86855,-1e-5 "
                + "-0.0114,-2.64583 h -2.64583 l -2.64583,2.64584 h -7.9375 l -2.64584,2.64583 -2.58891,-2.64584 h -13.28609 v "
                + "-2.64583 h -2.64583 v -2.64584 l -5.29167,1e-5 v -2.64583 h -2.64583 v -2.64583 l -5.29167,-1e-5 v -2.64583 h "
                + "-2.64583 v -2.64584 h -5.291667 v -2.64583 H 92.60417 V 174.625 h -5.291667 v -2.64584 l -34.395835,1e-5 "
                + "-7.9375,-2.64584 -7.9375,-2.64583 -5.291667,-5.29167 H 21.166667 L 13.229167,158.75 5.2916668,153.45833 H "
                + "2.6458334 l -10e-8,47.625";

            B2Vec2[] points = new B2Vec2[64];

            B2Vec2 offset = new B2Vec2(0.0f, -200.0f);
            float scale = 0.2f;

            int count = SvgParser.ParsePath(path, offset, points, 64, scale, false);

            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = count;
            chainDef.isLoop = true;

            b2CreateChain(groundId2, ref chainDef);
        }

        {
            B2Polygon box = b2MakeBox(0.5f, 0.125f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();

            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.maxMotorTorque = 10.0f;
            jointDef.enableMotor = true;
            jointDef.hertz = 3.0f;
            jointDef.dampingRatio = 0.8f;
            jointDef.enableSpring = true;

            float xBase = 48.7f;
            float yBase = 9.2f;
            int count = 50;
            B2BodyId prevBodyId = groundId1;
            for (int i = 0; i < count; ++i)
            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(xBase + 0.5f + 1.0f * i, yBase);
                bodyDef.angularDamping = 0.2f;
                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

                B2Vec2 pivot = new B2Vec2(xBase + 1.0f * i, yBase);
                jointDef.bodyIdA = prevBodyId;
                jointDef.bodyIdB = bodyId;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                b2CreateRevoluteJoint(m_worldId, ref jointDef);

                prevBodyId = bodyId;
            }

            {
                B2Vec2 pivot = new B2Vec2(xBase + 1.0f * count, yBase);
                jointDef.bodyIdA = prevBodyId;
                jointDef.bodyIdB = groundId2;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                b2CreateRevoluteJoint(m_worldId, ref jointDef);
            }
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(32.0f, 4.0f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            m_friendlyShape.maxPush = 0.025f;
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
        m_lockCamera = true;
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
        else if (m_onGround)
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
            float steer = m_onGround ? 1.0f : m_airSteer;
            float accelSpeed = steer * m_accelerate * m_maxSpeed * timeStep;
            if (accelSpeed > addSpeed)
            {
                accelSpeed = addSpeed;
            }

            m_velocity += accelSpeed * desiredDirection;
        }

        m_velocity.Y -= m_gravity * timeStep;

        float pogoRestLength = 3.0f * m_capsule.radius;
        float rayLength = pogoRestLength + m_capsule.radius;
        B2Vec2 origin = b2TransformPoint(ref m_transform, m_capsule.center1);
        B2Circle circle = new B2Circle(origin, 0.5f * m_capsule.radius);
        float boxHalfWidth = 0.75f * m_capsule.radius;
        float boxHalfHeight = 0.05f * m_capsule.radius;
        B2Polygon box = b2MakeOffsetBox(boxHalfWidth, boxHalfHeight, origin, b2Rot_identity);
        B2Vec2 translation;
        B2QueryFilter skipTeamFilter = new B2QueryFilter(1, ~2u);
        CastResult result = new CastResult();

        if (m_pogoShape == (int)PogoShape.PogoPoint)
        {
            translation = new B2Vec2(0.0f, -rayLength);
            b2World_CastRay(m_worldId, origin, translation, skipTeamFilter, CastCallback, result);
        }
        else if (m_pogoShape == (int)PogoShape.PogoCircle)
        {
            translation = new B2Vec2(0.0f, -rayLength + circle.radius);
            b2World_CastCircle(m_worldId, ref circle, translation, skipTeamFilter, CastCallback, result);
        }
        else
        {
            translation = new B2Vec2(0.0f, -rayLength + boxHalfHeight);
            b2World_CastPolygon(m_worldId, ref box, translation, skipTeamFilter, CastCallback, result);
        }

        if (result.hit == false)
        {
            m_onGround = false;
            m_pogoVelocity = 0.0f;

            B2Vec2 delta = translation;
            m_context.draw.DrawSegment(origin, origin + delta, B2HexColor.b2_colorGray);

            if (m_pogoShape == (int)PogoShape.PogoPoint)
            {
                m_context.draw.DrawPoint(origin + delta, 10.0f, B2HexColor.b2_colorGray);
            }
            else if (m_pogoShape == (int)PogoShape.PogoCircle)
            {
                m_context.draw.DrawCircle(origin + delta, circle.radius, B2HexColor.b2_colorGray);
            }
            else
            {
                B2Transform xf = new B2Transform(delta, b2Rot_identity);
                m_context.draw.DrawSolidPolygon(ref xf, box.vertices.AsSpan(), box.count, 0.0f, B2HexColor.b2_colorGray);
            }
        }
        else
        {
            m_onGround = true;
            float pogoCurrentLength = result.fraction * rayLength;

            float zeta = m_pogoDampingRatio;
            float hertz = m_pogoHertz;
            float omega = 2.0f * B2_PI * hertz;
            float omegaH = omega * timeStep;

            m_pogoVelocity = (m_pogoVelocity - omega * omegaH * (pogoCurrentLength - pogoRestLength)) /
                             (1.0f + 2.0f * zeta * omegaH + omegaH * omegaH);

            B2Vec2 delta = result.fraction * translation;
            m_context.draw.DrawSegment(origin, origin + delta, B2HexColor.b2_colorGray);

            if (m_pogoShape == (int)PogoShape.PogoPoint)
            {
                m_context.draw.DrawPoint(origin + delta, 10.0f, B2HexColor.b2_colorPlum);
            }
            else if (m_pogoShape == (int)PogoShape.PogoCircle)
            {
                m_context.draw.DrawCircle(origin + delta, circle.radius, B2HexColor.b2_colorPlum);
            }
            else
            {
                B2Transform xf = new B2Transform(delta, b2Rot_identity);
                m_context.draw.DrawSolidPolygon(ref xf, box.vertices.AsSpan(), box.count, 0.0f, B2HexColor.b2_colorPlum);
            }

            b2Body_ApplyForce(result.bodyId, new B2Vec2(0.0f, -50.0f), result.point, true);
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

    public override void UpdateGui()
    {
        float height = 350.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 25.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(340.0f, height));

        ImGui.Begin("Mover", 0);

        ImGui.PushItemWidth(240.0f);

        ImGui.SliderFloat("Jump Speed", ref m_jumpSpeed, 0.0f, 40.0f, "%.0f");
        ImGui.SliderFloat("Min Speed", ref m_minSpeed, 0.0f, 1.0f, "%.2f");
        ImGui.SliderFloat("Max Speed", ref m_maxSpeed, 0.0f, 20.0f, "%.0f");
        ImGui.SliderFloat("Stop Speed", ref m_stopSpeed, 0.0f, 10.0f, "%.1f");
        ImGui.SliderFloat("Accelerate", ref m_accelerate, 0.0f, 100.0f, "%.0f");
        ImGui.SliderFloat("Friction", ref m_friction, 0.0f, 10.0f, "%.1f");
        ImGui.SliderFloat("Gravity", ref m_gravity, 0.0f, 100.0f, "%.1f");
        ImGui.SliderFloat("Air Steer", ref m_airSteer, 0.0f, 1.0f, "%.2f");
        ImGui.SliderFloat("Pogo Hertz", ref m_pogoHertz, 0.0f, 30.0f, "%.0f");
        ImGui.SliderFloat("Pogo Damping", ref m_pogoDampingRatio, 0.0f, 4.0f, "%.1f");

        ImGui.PopItemWidth();

        ImGui.Separator();

        ImGui.Text("Pogo Shape");
        ImGui.RadioButton("Point", ref m_pogoShape, (int)PogoShape.PogoPoint);
        ImGui.SameLine();
        ImGui.RadioButton("Circle", ref m_pogoShape, (int)PogoShape.PogoCircle);
        ImGui.SameLine();
        ImGui.RadioButton("Box", ref m_pogoShape, (int)PogoShape.PogoBox);

        ImGui.Checkbox("Lock Camera", ref m_lockCamera);

        ImGui.End();
    }

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

        if (m_lockCamera)
        {
            m_context.camera.m_center.X = m_transform.p.X;
        }
    }
}