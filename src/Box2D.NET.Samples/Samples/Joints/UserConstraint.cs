using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Joints;

// This shows how you can implement a constraint outside of Box2D
class UserConstraint : Sample
{
public:
public UserConstraint( Settings settings )
    : base( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 3.0f, -1.0f };
        Draw.g_camera.m_zoom = 25.0f * 0.15f;
    }

    b2Polygon box = b2MakeBox( 1.0f, 0.5f );

    b2ShapeDef shapeDef = b2DefaultShapeDef();
    shapeDef.density = 20.0f;

    b2BodyDef bodyDef = b2DefaultBodyDef();
    bodyDef.type = b2BodyType.b2_dynamicBody;
    bodyDef.gravityScale = 1.0f;
    bodyDef.angularDamping = 0.5f;
    bodyDef.linearDamping = 0.2f;
    m_bodyId = b2CreateBody( m_worldId, &bodyDef );
    b2CreatePolygonShape( m_bodyId, &shapeDef, &box );

    m_impulses[0] = 0.0f;
    m_impulses[1] = 0.0f;
}

public override void Step(Settings settings)
{
    base.Step( settings );

    b2Transform axes = b2Transform_identity;
    Draw.g_draw.DrawTransform( axes );

    if ( settings.pause )
    {
        return;
    }

    float timeStep = settings.hertz > 0.0f ? 1.0f / settings.hertz : 0.0f;
    if ( timeStep == 0.0f )
    {
        return;
    }

    float invTimeStep = settings.hertz;

    static float hertz = 3.0f;
    static float zeta = 0.7f;
    static float maxForce = 1000.0f;
    float omega = 2.0f * B2_PI * hertz;
    float sigma = 2.0f * zeta + timeStep * omega;
    float s = timeStep * omega * sigma;
    float impulseCoefficient = 1.0f / ( 1.0f + s );
    float massCoefficient = s * impulseCoefficient;
    float biasCoefficient = omega / sigma;

    b2Vec2 localAnchors[2] = { { 1.0f, -0.5f }, { 1.0f, 0.5f } };
    float mass = b2Body_GetMass( m_bodyId );
    float invMass = mass < 0.0001f ? 0.0f : 1.0f / mass;
    float inertiaTensor = b2Body_GetRotationalInertia( m_bodyId );
    float invI = inertiaTensor < 0.0001f ? 0.0f : 1.0f / inertiaTensor;

    b2Vec2 vB = b2Body_GetLinearVelocity( m_bodyId );
    float omegaB = b2Body_GetAngularVelocity( m_bodyId );
    b2Vec2 pB = b2Body_GetWorldCenterOfMass( m_bodyId );

    for ( int i = 0; i < 2; ++i )
    {
        b2Vec2 anchorA = { 3.0f, 0.0f };
        b2Vec2 anchorB = b2Body_GetWorldPoint( m_bodyId, localAnchors[i] );

        b2Vec2 deltaAnchor = b2Sub( anchorB, anchorA );

        float slackLength = 1.0f;
        float length = b2Length( deltaAnchor );
        float C = length - slackLength;
        if ( C < 0.0f || length < 0.001f )
        {
            Draw.g_draw.DrawSegment( anchorA, anchorB, b2HexColor.b2_colorLightCyan );
            m_impulses[i] = 0.0f;
            continue;
        }

        Draw.g_draw.DrawSegment( anchorA, anchorB, b2HexColor.b2_colorViolet );
        b2Vec2 axis = b2Normalize( deltaAnchor );

        b2Vec2 rB = b2Sub( anchorB, pB );
        float Jb = b2Cross( rB, axis );
        float K = invMass + Jb * invI * Jb;
        float invK = K < 0.0001f ? 0.0f : 1.0f / K;

        float Cdot = b2Dot( vB, axis ) + Jb * omegaB;
        float impulse = -massCoefficient * invK * ( Cdot + biasCoefficient * C );
        float appliedImpulse = b2ClampFloat( impulse, -maxForce * timeStep, 0.0f );

        vB = b2MulAdd( vB, invMass * appliedImpulse, axis );
        omegaB += appliedImpulse * invI * Jb;

        m_impulses[i] = appliedImpulse;
    }

    b2Body_SetLinearVelocity( m_bodyId, vB );
    b2Body_SetAngularVelocity( m_bodyId, omegaB );

    Draw.g_draw.DrawString( 5, m_textLine, "forces = %g, %g", m_impulses[0] * invTimeStep, m_impulses[1] * invTimeStep );
    m_textLine += m_textIncrement;
}

static Sample Create( Settings settings )
{
    return new UserConstraint( settings );
}

b2BodyId m_bodyId;
float m_impulses[2];
};

static int sampleUserConstraintIndex = RegisterSample( "Joints", "User Constraint", UserConstraint::Create );
