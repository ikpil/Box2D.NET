using System.Diagnostics;
using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using ImGuiNET;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;

namespace Box2D.NET.Samples.Samples.Events;

struct BodyUserData
{
    int index;
};

class ContactEvent : Sample
{
public:
enum
{
    e_count = 20
};

explicit ContactEvent( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 0.0f };
        Draw.g_camera.m_zoom = 25.0f * 1.75f;
    }

    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

        b2Vec2 points[] = { { 40.0f, -40.0f }, { -40.0f, -40.0f }, { -40.0f, 40.0f }, { 40.0f, 40.0f } };

        b2ChainDef chainDef = b2DefaultChainDef();
        chainDef.count = 4;
        chainDef.points = points;
        chainDef.isLoop = true;

        b2CreateChain( groundId, &chainDef );
    }

    // Player
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.gravityScale = 0.0f;
        bodyDef.linearDamping = 0.5f;
        bodyDef.angularDamping = 0.5f;
        bodyDef.isBullet = true;
        m_playerId = b2CreateBody( m_worldId, &bodyDef );

        b2Circle circle = { { 0.0f, 0.0f }, 1.0f };
        b2ShapeDef shapeDef = b2DefaultShapeDef();

        // Enable contact events for the player shape
        shapeDef.enableContactEvents = true;

        m_coreShapeId = b2CreateCircleShape( m_playerId, &shapeDef, &circle );
    }

    for ( int i = 0; i < e_count; ++i )
    {
        m_debrisIds[i] = b2_nullBodyId;
        m_bodyUserData[i].index = i;
    }

    m_wait = 0.5f;
    m_force = 200.0f;
}

void SpawnDebris()
{
    int index = -1;
    for ( int i = 0; i < e_count; ++i )
    {
        if ( B2_IS_NULL( m_debrisIds[i] ) )
        {
            index = i;
            break;
        }
    }

    if ( index == -1 )
    {
        return;
    }

    // Debris
    b2BodyDef bodyDef = b2DefaultBodyDef();
    bodyDef.type = b2BodyType.b2_dynamicBody;
    bodyDef.position = { RandomFloatRange( -38.0f, 38.0f ), RandomFloatRange( -38.0f, 38.0f ) };
    bodyDef.rotation = b2MakeRot( RandomFloatRange( -B2_PI, B2_PI ) );
    bodyDef.linearVelocity = { RandomFloatRange( -5.0f, 5.0f ), RandomFloatRange( -5.0f, 5.0f ) };
    bodyDef.angularVelocity = RandomFloatRange( -1.0f, 1.0f );
    bodyDef.gravityScale = 0.0f;
    bodyDef.userData = m_bodyUserData + index;
    m_debrisIds[index] = b2CreateBody( m_worldId, &bodyDef );

    b2ShapeDef shapeDef = b2DefaultShapeDef();
    shapeDef.restitution = 0.8f;

    // No events when debris hits debris
    shapeDef.enableContactEvents = false;

    if ( ( index + 1 ) % 3 == 0 )
    {
        b2Circle circle = { { 0.0f, 0.0f }, 0.5f };
        b2CreateCircleShape( m_debrisIds[index], &shapeDef, &circle );
    }
    else if ( ( index + 1 ) % 2 == 0 )
    {
        b2Capsule capsule = { { 0.0f, -0.25f }, { 0.0f, 0.25f }, 0.25f };
        b2CreateCapsuleShape( m_debrisIds[index], &shapeDef, &capsule );
    }
    else
    {
        b2Polygon box = b2MakeBox( 0.4f, 0.6f );
        b2CreatePolygonShape( m_debrisIds[index], &shapeDef, &box );
    }
}

void UpdateUI() override
{
    float height = 60.0f;
    ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( new Vector2( 240.0f, height ) );

    ImGui.Begin( "Contact Event", nullptr, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize );

    ImGui.SliderFloat( "force", &m_force, 100.0f, 500.0f, "%.1f" );

    ImGui.End();
}

void Step( Settings& settings ) override
{
    Draw.g_draw.DrawString( 5, m_textLine, "move using WASD" );
    m_textLine += m_textIncrement;

    b2Vec2 position = b2Body_GetPosition( m_playerId );

    if ( glfwGetKey( g_mainWindow, GLFW_KEY_A ) == GLFW_PRESS )
    {
        b2Body_ApplyForce( m_playerId, { -m_force, 0.0f }, position, true );
    }

    if ( glfwGetKey( g_mainWindow, GLFW_KEY_D ) == GLFW_PRESS )
    {
        b2Body_ApplyForce( m_playerId, { m_force, 0.0f }, position, true );
    }

    if ( glfwGetKey( g_mainWindow, GLFW_KEY_W ) == GLFW_PRESS )
    {
        b2Body_ApplyForce( m_playerId, { 0.0f, m_force }, position, true );
    }

    if ( glfwGetKey( g_mainWindow, GLFW_KEY_S ) == GLFW_PRESS )
    {
        b2Body_ApplyForce( m_playerId, { 0.0f, -m_force }, position, true );
    }

    Sample::Step( settings );

    // Discover rings that touch the bottom sensor
    int debrisToAttach[e_count] = {};
    b2ShapeId shapesToDestroy[e_count] = { b2_nullShapeId };
    int attachCount = 0;
    int destroyCount = 0;

    std::vector<b2ContactData> contactData;

    // Process contact begin touch events.
    b2ContactEvents contactEvents = b2World_GetContactEvents( m_worldId );
    for ( int i = 0; i < contactEvents.beginCount; ++i )
    {
        b2ContactBeginTouchEvent event = contactEvents.beginEvents[i];
        b2BodyId bodyIdA = b2Shape_GetBody( event.shapeIdA );
        b2BodyId bodyIdB = b2Shape_GetBody( event.shapeIdB );

        // The begin touch events have the contact manifolds, but the impulses are zero. This is because the manifolds
        // are gathered before the contact solver is run.

        // We can get the final contact data from the shapes. The manifold is shared by the two shapes, so we just need the
        // contact data from one of the shapes. Choose the one with the smallest number of contacts.

        int capacityA = b2Shape_GetContactCapacity( event.shapeIdA );
        int capacityB = b2Shape_GetContactCapacity( event.shapeIdB );

        if ( capacityA < capacityB )
        {
            contactData.resize( capacityA );

            // The count may be less than the capacity
            int countA = b2Shape_GetContactData( event.shapeIdA, contactData.data(), capacityA );
            Debug.Assert( countA >= 1 );

            for ( int j = 0; j < countA; ++j )
            {
                b2ShapeId idA = contactData[j].shapeIdA;
                b2ShapeId idB = contactData[j].shapeIdB;
                if ( B2_ID_EQUALS( idA, event.shapeIdB ) || B2_ID_EQUALS( idB, event.shapeIdB ) )
                {
                    Debug.Assert( B2_ID_EQUALS( idA, event.shapeIdA ) || B2_ID_EQUALS( idB, event.shapeIdA ) );

                    b2Manifold manifold = contactData[j].manifold;
                    b2Vec2 normal = manifold.normal;
                    Debug.Assert( b2AbsFloat( b2Length( normal ) - 1.0f ) < 4.0f * FLT_EPSILON );

                    for ( int k = 0; k < manifold.pointCount; ++k )
                    {
                        b2ManifoldPoint point = manifold.points[k];
                        Draw.g_draw.DrawSegment( point.point, point.point + point.maxNormalImpulse * normal, b2_colorBlueViolet );
                        Draw.g_draw.DrawPoint( point.point, 10.0f, b2_colorWhite );
                    }
                }
            }
        }
        else
        {
            contactData.resize( capacityB );

            // The count may be less than the capacity
            int countB = b2Shape_GetContactData( event.shapeIdB, contactData.data(), capacityB );
            Debug.Assert( countB >= 1 );

            for ( int j = 0; j < countB; ++j )
            {
                b2ShapeId idA = contactData[j].shapeIdA;
                b2ShapeId idB = contactData[j].shapeIdB;

                if ( B2_ID_EQUALS( idA, event.shapeIdA ) || B2_ID_EQUALS( idB, event.shapeIdA ) )
                {
                    Debug.Assert( B2_ID_EQUALS( idA, event.shapeIdB ) || B2_ID_EQUALS( idB, event.shapeIdB ) );

                    b2Manifold manifold = contactData[j].manifold;
                    b2Vec2 normal = manifold.normal;
                    Debug.Assert( b2AbsFloat( b2Length( normal ) - 1.0f ) < 4.0f * FLT_EPSILON );

                    for ( int k = 0; k < manifold.pointCount; ++k )
                    {
                        b2ManifoldPoint point = manifold.points[k];
                        Draw.g_draw.DrawSegment( point.point, point.point + point.maxNormalImpulse * normal, b2_colorYellowGreen );
                        Draw.g_draw.DrawPoint( point.point, 10.0f, b2_colorWhite );
                    }
                }
            }
        }

        if ( B2_ID_EQUALS( bodyIdA, m_playerId ) )
        {
            BodyUserData* userDataB = static_cast<BodyUserData*>( b2Body_GetUserData( bodyIdB ) );
            if ( userDataB == nullptr )
            {
                if ( B2_ID_EQUALS( event.shapeIdA, m_coreShapeId ) == false && destroyCount < e_count )
                {
                    // player non-core shape hit the wall

                    bool found = false;
                    for ( int j = 0; j < destroyCount; ++j )
                    {
                        if ( B2_ID_EQUALS( event.shapeIdA, shapesToDestroy[j] ) )
                        {
                            found = true;
                            break;
                        }
                    }

                    // avoid double deletion
                    if ( found == false )
                    {
                        shapesToDestroy[destroyCount] = event.shapeIdA;
                        destroyCount += 1;
                    }
                }
            }
            else if ( attachCount < e_count )
            {
                debrisToAttach[attachCount] = userDataB->index;
                attachCount += 1;
            }
        }
        else
        {
            // Only expect events for the player
            Debug.Assert( B2_ID_EQUALS( bodyIdB, m_playerId ) );
            BodyUserData* userDataA = static_cast<BodyUserData*>( b2Body_GetUserData( bodyIdA ) );
            if ( userDataA == nullptr )
            {
                if ( B2_ID_EQUALS( event.shapeIdB, m_coreShapeId ) == false && destroyCount < e_count )
                {
                    // player non-core shape hit the wall

                    bool found = false;
                    for ( int j = 0; j < destroyCount; ++j )
                    {
                        if ( B2_ID_EQUALS( event.shapeIdB, shapesToDestroy[j] ) )
                        {
                            found = true;
                            break;
                        }
                    }

                    // avoid double deletion
                    if ( found == false )
                    {
                        shapesToDestroy[destroyCount] = event.shapeIdB;
                        destroyCount += 1;
                    }
                }
            }
            else if ( attachCount < e_count )
            {
                debrisToAttach[attachCount] = userDataA->index;
                attachCount += 1;
            }
        }
    }

    // Attach debris to player body
    for ( int i = 0; i < attachCount; ++i )
    {
        int index = debrisToAttach[i];
        b2BodyId debrisId = m_debrisIds[index];
        if ( B2_IS_NULL( debrisId ) )
        {
            continue;
        }

        b2Transform playerTransform = b2Body_GetTransform( m_playerId );
        b2Transform debrisTransform = b2Body_GetTransform( debrisId );
        b2Transform relativeTransform = b2InvMulTransforms( playerTransform, debrisTransform );

        int shapeCount = b2Body_GetShapeCount( debrisId );
        if ( shapeCount == 0 )
        {
            continue;
        }

        b2ShapeId shapeId;
        b2Body_GetShapes( debrisId, &shapeId, 1 );

        b2ShapeType type = b2Shape_GetType( shapeId );

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.enableContactEvents = true;

        switch ( type )
        {
            case b2_circleShape:
            {
                b2Circle circle = b2Shape_GetCircle( shapeId );
                circle.center = b2TransformPoint( relativeTransform, circle.center );

                b2CreateCircleShape( m_playerId, &shapeDef, &circle );
            }
                break;

            case b2_capsuleShape:
            {
                b2Capsule capsule = b2Shape_GetCapsule( shapeId );
                capsule.center1 = b2TransformPoint( relativeTransform, capsule.center1 );
                capsule.center2 = b2TransformPoint( relativeTransform, capsule.center2 );

                b2CreateCapsuleShape( m_playerId, &shapeDef, &capsule );
            }
                break;

            case b2_polygonShape:
            {
                b2Polygon originalPolygon = b2Shape_GetPolygon( shapeId );
                b2Polygon polygon = b2TransformPolygon( relativeTransform, &originalPolygon );

                b2CreatePolygonShape( m_playerId, &shapeDef, &polygon );
            }
                break;

            default:
                Debug.Assert( false );
        }

        b2DestroyBody( debrisId );
        m_debrisIds[index] = b2_nullBodyId;
    }

    for ( int i = 0; i < destroyCount; ++i )
    {
        bool updateMass = false;
        b2DestroyShape( shapesToDestroy[i], updateMass );
    }

    if ( destroyCount > 0 )
    {
        // Update mass just once
        b2Body_ApplyMassFromShapes( m_playerId );
    }

    if ( settings.hertz > 0.0f && settings.pause == false )
    {
        m_wait -= 1.0f / settings.hertz;
        if ( m_wait < 0.0f )
        {
            SpawnDebris();
            m_wait += 0.5f;
        }
    }
}

static Sample* Create( Settings& settings )
{
    return new ContactEvent( settings );
}

b2BodyId m_playerId;
b2ShapeId m_coreShapeId;
b2BodyId m_debrisIds[e_count];
BodyUserData m_bodyUserData[e_count];
float m_force;
float m_wait;
};

static int sampleWeeble = RegisterSample( "Events", "Contact", ContactEvent::Create );

