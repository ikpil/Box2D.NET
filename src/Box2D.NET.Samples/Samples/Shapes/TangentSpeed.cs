// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Shapes;

public class TangentSpeed : Sample
{
    public const int m_totalCount = 200;

    int m_count = 0;
    static int sampleTangentSpeed = RegisterSample("Shapes", "Tangent Speed", Create);

    static Sample Create(Settings settings)
    {
        return new TangentSpeed(settings);
    }

    public TangentSpeed(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(60.0f, -15.0f);
            Draw.g_camera.m_zoom = 38.0f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            //string path = "M 613.8334,185.20833 H 500.06255 L 470.95838,182.5625 444.50004,174.625 418.04171,161.39583 "
            //				   "394.2292,140.22917 h "
            //				   "-13.22916 v 44.97916 H 68.791712 V 0 h -21.16671 v 206.375 l 566.208398,-1e-5 z";

            string path = "m 613.8334,185.20833 -42.33338,0 h -37.04166 l -34.39581,0 -29.10417,-2.64583 -26.45834,-7.9375 "
                          + "-26.45833,-13.22917 -23.81251,-21.16666 h -13.22916 v 44.97916 H 68.791712 V 0 h -21.16671 v "
                          + "206.375 l 566.208398,-1e-5 z";

            b2Vec2 offset = new b2Vec2(-47.375002f, 0.25f);

            float scale = 0.2f;
            b2Vec2[] points = new b2Vec2[20];
            int count = ParsePath(path, offset, points, 20, scale, true);

            b2SurfaceMaterial[] materials = new b2SurfaceMaterial[20];
            for (int i = 0; i < 20; ++i)
            {
                materials[i] = new b2SurfaceMaterial();
                materials[i].friction = 0.6f;
            }

            materials[0].tangentSpeed = -10.0f;
            materials[0].customColor = (uint)b2HexColor.b2_colorDarkBlue;
            materials[1].tangentSpeed = -20.0f;
            materials[1].customColor = (uint)b2HexColor.b2_colorDarkCyan;
            materials[2].tangentSpeed = -30.0f;
            materials[2].customColor = (uint)b2HexColor.b2_colorDarkGoldenRod;
            materials[3].tangentSpeed = -40.0f;
            materials[3].customColor = (uint)b2HexColor.b2_colorDarkGray;
            materials[4].tangentSpeed = -50.0f;
            materials[4].customColor = (uint)b2HexColor.b2_colorDarkGreen;
            materials[5].tangentSpeed = -60.0f;
            materials[5].customColor = (uint)b2HexColor.b2_colorDarkKhaki;
            materials[6].tangentSpeed = -70.0f;
            materials[6].customColor = (uint)b2HexColor.b2_colorDarkMagenta;

            b2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = count;
            chainDef.isLoop = true;
            chainDef.materials = materials;
            chainDef.materialCount = count;

            b2CreateChain(groundId, chainDef);
        }
    }

    b2BodyId DropBall()
    {
        b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.0f), 0.5f);

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = new b2Vec2(110.0f, -30.0f);
        b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.rollingResistance = 0.3f;
        b2CreateCircleShape(bodyId, shapeDef, circle);
        return bodyId;
    }

    public override void Step(Settings settings)
    {
        if (m_stepCount % 25 == 0 && m_count < m_totalCount && settings.pause == false)
        {
            DropBall();
            m_count += 1;
        }

        base.Step(settings);
    }
}
