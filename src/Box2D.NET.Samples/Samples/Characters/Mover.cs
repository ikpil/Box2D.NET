// SPDX-FileCopyrightText: 2022 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
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
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Diagnostics;

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
    private const ulong DebrisBit = 0x0008;
    private const ulong AllBits = ~0u;

    public enum PogoShape
    {
        PogoPoint,
        PogoCircle,
        PogoSegment
    }

    public class CastResult
    {
        public B2Vec2 point;
        public B2Vec2 normal;
        public B2BodyId bodyId;
        public float fraction;
        public bool hit;
    }

    private const int m_planeCapacity = 8;
    private readonly B2Vec2 m_elevatorBase = new B2Vec2(112.0f, 10.0f);
    private const float m_elevatorAmplitude = 4.0f;

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

    private int m_pogoShape = (int)PogoShape.PogoSegment;
    private B2Transform m_transform;
    private B2Vec2 m_velocity;
    private B2Capsule m_capsule;
    private B2BodyId m_elevatorId;
    private B2ShapeId m_ballId;
    private ShapeUserData m_friendlyShape = new ShapeUserData();
    private ShapeUserData m_elevatorShape = new ShapeUserData();
    private B2CollisionPlane[] m_planes = new B2CollisionPlane[m_planeCapacity];
    public int m_planeCount;
    public int m_totalIterations;
    public float m_pogoVelocity;
    public float m_time;
    public bool m_onGround;
    public bool m_jumpReleased;
    public bool m_lockCamera;

    private int m_deltaX;
    private int m_deltaY;

    private static Sample Create(SampleContext context)
    {
        return new Mover(context);
    }

    private static float CastCallback(B2ShapeId shapeId, B2Vec2 point, B2Vec2 normal, float fraction, object context)
    {
        CastResult result = (CastResult)context;
        result.point = point;
        result.normal = normal;
        result.bodyId = b2Shape_GetBody(shapeId);
        result.fraction = fraction;
        result.hit = true;
        return fraction;
    }

    public Mover(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(20.0f, 9.0f);
            m_camera.m_zoom = 10.0f;
        }

        m_context.settings.drawJoints = false;
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
            bodyDef.position = new B2Vec2(32.0f, 4.5f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            m_friendlyShape.maxPush = 0.025f;
            m_friendlyShape.clipVelocity = false;

            shapeDef.filter = new B2Filter(MoverBit, AllBits, 0);
            shapeDef.userData = m_friendlyShape;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
            b2CreateCapsuleShape(bodyId, ref shapeDef, ref m_capsule);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(7.0f, 7.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.filter = new B2Filter(DebrisBit, AllBits, 0);
            shapeDef.material.restitution = 0.7f;
            shapeDef.material.rollingResistance = 0.2f;

            B2Circle circle = new B2Circle(b2Vec2_zero, 0.3f);
            m_ballId = b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_kinematicBody;
            bodyDef.position = new B2Vec2(m_elevatorBase.X, m_elevatorBase.Y - m_elevatorAmplitude);
            m_elevatorId = b2CreateBody(m_worldId, ref bodyDef);

            m_elevatorShape = new ShapeUserData
            {
                maxPush = 0.1f,
                clipVelocity = true,
            };
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.filter = new B2Filter(DynamicBit, AllBits, 0);
            shapeDef.userData = m_elevatorShape;

            B2Polygon box = b2MakeBox(2.0f, 0.1f);
            b2CreatePolygonShape(m_elevatorId, ref shapeDef, ref box);
        }

        m_totalIterations = 0;
        m_pogoVelocity = 0.0f;
        m_onGround = false;
        m_jumpReleased = true;
        m_lockCamera = true;
        m_planeCount = 0;
        m_time = 0.0f;
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

        float noWalkSteer = 0.0f;
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
        B2Vec2 segmentOffset = new B2Vec2(0.75f * m_capsule.radius, 0.0f);
        B2Segment segment = new B2Segment(
            origin - segmentOffset,
            origin + segmentOffset
        );

        B2ShapeProxy proxy = new B2ShapeProxy();
        B2Vec2 translation;
        B2QueryFilter pogoFilter = new B2QueryFilter(MoverBit, StaticBit | DynamicBit);
        CastResult castResult = new CastResult();

        if (m_pogoShape == (int)PogoShape.PogoPoint)
        {
            proxy = b2MakeProxy(origin, 1, 0.0f);
            translation = new B2Vec2(0.0f, -rayLength);
        }
        else if (m_pogoShape == (int)PogoShape.PogoCircle)
        {
            proxy = b2MakeProxy(origin, 1, circle.radius);
            translation = new B2Vec2(0.0f, -rayLength + circle.radius);
        }
        else
        {
            proxy = b2MakeProxy(segment.point1, segment.point2, 2, 0.0f);
            translation = new B2Vec2(0.0f, -rayLength);
        }

        b2World_CastShape(m_worldId, ref proxy, translation, pogoFilter, CastCallback, castResult);

        // Avoid snapping to ground if still going up
        if (m_onGround == false)
        {
            m_onGround = castResult.hit && m_velocity.Y <= 0.01f;
        }
        else
        {
            m_onGround = castResult.hit;
        }

        if (castResult.hit == false)
        {
            m_pogoVelocity = 0.0f;

            B2Vec2 delta = translation;
            m_draw.DrawSegment(origin, origin + delta, B2HexColor.b2_colorGray);

            if (m_pogoShape == (int)PogoShape.PogoPoint)
            {
                m_draw.DrawPoint(origin + delta, 10.0f, B2HexColor.b2_colorGray);
            }
            else if (m_pogoShape == (int)PogoShape.PogoCircle)
            {
                m_draw.DrawCircle(origin + delta, circle.radius, B2HexColor.b2_colorGray);
            }
            else
            {
                m_draw.DrawSegment(segment.point1 + delta, segment.point2 + delta, B2HexColor.b2_colorGray);
            }
        }
        else
        {
            float pogoCurrentLength = castResult.fraction * rayLength;

            float zeta = m_pogoDampingRatio;
            float hertz = m_pogoHertz;
            float omega = 2.0f * B2_PI * hertz;
            float omegaH = omega * timeStep;

            m_pogoVelocity = (m_pogoVelocity - omega * omegaH * (pogoCurrentLength - pogoRestLength)) /
                             (1.0f + 2.0f * zeta * omegaH + omegaH * omegaH);

            B2Vec2 delta = castResult.fraction * translation;
            m_draw.DrawSegment(origin, origin + delta, B2HexColor.b2_colorGray);

            if (m_pogoShape == (int)PogoShape.PogoPoint)
            {
                m_draw.DrawPoint(origin + delta, 10.0f, B2HexColor.b2_colorPlum);
            }
            else if (m_pogoShape == (int)PogoShape.PogoCircle)
            {
                m_draw.DrawCircle(origin + delta, circle.radius, B2HexColor.b2_colorPlum);
            }
            else
            {
                m_draw.DrawSegment(segment.point1 + delta, segment.point2 + delta, B2HexColor.b2_colorPlum);
            }

            b2Body_ApplyForce(castResult.bodyId, new B2Vec2(0.0f, -50.0f), castResult.point, true);
        }

        B2Vec2 target = m_transform.p + timeStep * m_velocity + timeStep * m_pogoVelocity * new B2Vec2(0.0f, 1.0f);

        // Mover overlap filter
        B2QueryFilter collideFilter = new B2QueryFilter(MoverBit, StaticBit | DynamicBit | MoverBit);

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
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_camera.m_height - height - 25.0f), ImGuiCond.Once);
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
        ImGui.RadioButton("Segment", ref m_pogoShape, (int)PogoShape.PogoSegment);

        ImGui.Checkbox("Lock Camera", ref m_lockCamera);

        ImGui.End();
    }

    static bool PlaneResultFcn(B2ShapeId shapeId, ref B2PlaneResult planeResult, object context)
    {
        B2_ASSERT(planeResult.hit == true);

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
            B2_ASSERT(b2IsValidPlane(planeResult.plane));
            self.m_planes[self.m_planeCount] = new B2CollisionPlane(planeResult.plane, maxPush, 0.0f, clipVelocity);
            self.m_planeCount += 1;
        }

        return true;
    }

    static bool Kick(B2ShapeId shapeId, object context)
    {
        Mover self = (Mover)context;
        B2BodyId bodyId = b2Shape_GetBody(shapeId);
        B2BodyType type = b2Body_GetType(bodyId);

        if (type != B2BodyType.b2_dynamicBody)
        {
            return true;
        }

        B2Vec2 center = b2Body_GetWorldCenterOfMass(bodyId);
        B2Vec2 direction = b2Normalize(center - self.m_transform.p);
        B2Vec2 impulse = new B2Vec2(2.0f * direction.X, 2.0f);
        b2Body_ApplyLinearImpulseToCenter(bodyId, impulse, true);

        return true;
    }

    public override void Keyboard(Keys key)
    {
        if (key == Keys.K)
        {
            B2Vec2 point = b2TransformPoint(ref m_transform, new B2Vec2(0.0f, m_capsule.center1.Y - 3.0f * m_capsule.radius));
            B2Circle circle = new B2Circle(point, 0.5f);
            B2ShapeProxy proxy = b2MakeProxy(circle.center, 1, circle.radius);
            B2QueryFilter filter = new B2QueryFilter(MoverBit, DebrisBit);
            b2World_OverlapShape(m_worldId, ref proxy, filter, Kick, this);
            m_draw.DrawCircle(circle.center, circle.radius, B2HexColor.b2_colorGoldenRod);
        }

        base.Keyboard(key);
    }

    public override void Step()
    {
        base.Step();

        bool pause = false;
        if (m_context.settings.pause)
        {
            pause = m_context.settings.singleStep != true;
        }

        float timeStep = m_context.settings.hertz > 0.0f ? 1.0f / m_context.settings.hertz : 0.0f;
        if (pause)
        {
            timeStep = 0.0f;
        }

        if (timeStep > 0.0f)
        {
            B2Vec2 point;
            point.X = m_elevatorBase.X;
            point.Y = m_elevatorAmplitude * MathF.Cos(1.0f * m_time + B2_PI) + m_elevatorBase.Y;

            b2Body_SetTargetTransform(m_elevatorId, new B2Transform(point, b2Rot_identity), timeStep);
        }

        m_time += timeStep;

        if (pause == false)
        {
            float throttle = 0.0f;

            if (InputAction.Press == GetKey(Keys.A))
            {
                throttle -= 1.0f;
            }

            if (InputAction.Press == GetKey(Keys.D))
            {
                throttle += 1.0f;
            }

            if (InputAction.Press == GetKey(Keys.Space))
            {
                if (m_onGround == true && m_jumpReleased)
                {
                    m_velocity.Y = m_jumpSpeed;
                    m_onGround = false;
                    m_jumpReleased = false;
                }
            }
            else
            {
                m_jumpReleased = true;
            }

            SolveMove(timeStep, throttle);
        }
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
            m_draw.DrawPoint(p1, 5.0f, B2HexColor.b2_colorYellow);
            m_draw.DrawSegment(p1, p2, B2HexColor.b2_colorYellow);
        }

        {
            B2Vec2 p1 = b2TransformPoint(ref m_transform, m_capsule.center1);
            B2Vec2 p2 = b2TransformPoint(ref m_transform, m_capsule.center2);

            B2HexColor color = m_onGround ? B2HexColor.b2_colorOrange : B2HexColor.b2_colorAquamarine;
            m_draw.DrawSolidCapsule(p1, p2, m_capsule.radius, color);
            m_draw.DrawSegment(m_transform.p, m_transform.p + m_velocity, B2HexColor.b2_colorPurple);
        }

        B2Vec2 p = m_transform.p;
        DrawTextLine($"position {p.X:F2} {p.Y:F2}");
        DrawTextLine($"velocity {m_velocity.X:F2}, {m_velocity.Y:F2}");
        DrawTextLine($"iterations {m_totalIterations}");

        if (m_lockCamera)
        {
            m_camera.m_center.X = m_transform.p.X;
        }
    }
}