// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Shapes;

public class TangentSpeed : Sample
{
    public const int m_totalCount = 200;

    int m_count = 0;
    private static readonly int SampleTangentSpeed = SampleFactory.Shared.RegisterSample("Shapes", "Tangent Speed", Create);

    private static Sample Create(Settings settings)
    {
        return new TangentSpeed(settings);
    }

    public TangentSpeed(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(60.0f, -15.0f);
            B2.g_camera.m_zoom = 38.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            //string path = "M 613.8334,185.20833 H 500.06255 L 470.95838,182.5625 444.50004,174.625 418.04171,161.39583 "
            //				   "394.2292,140.22917 h "
            //				   "-13.22916 v 44.97916 H 68.791712 V 0 h -21.16671 v 206.375 l 566.208398,-1e-5 z";

            string path = "m 613.8334,185.20833 -42.33338,0 h -37.04166 l -34.39581,0 -29.10417,-2.64583 -26.45834,-7.9375 "
                          + "-26.45833,-13.22917 -23.81251,-21.16666 h -13.22916 v 44.97916 H 68.791712 V 0 h -21.16671 v "
                          + "206.375 l 566.208398,-1e-5 z";

            B2Vec2 offset = new B2Vec2(-47.375002f, 0.25f);

            float scale = 0.2f;
            B2Vec2[] points = new B2Vec2[20];
            int count = ParsePath(path, offset, points, 20, scale, true);

            B2SurfaceMaterial[] materials = new B2SurfaceMaterial[20];
            for (int i = 0; i < 20; ++i)
            {
                materials[i] = new B2SurfaceMaterial();
                materials[i].friction = 0.6f;
            }

            materials[0].tangentSpeed = -10.0f;
            materials[0].customColor = (uint)B2HexColor.b2_colorDarkBlue;
            materials[1].tangentSpeed = -20.0f;
            materials[1].customColor = (uint)B2HexColor.b2_colorDarkCyan;
            materials[2].tangentSpeed = -30.0f;
            materials[2].customColor = (uint)B2HexColor.b2_colorDarkGoldenRod;
            materials[3].tangentSpeed = -40.0f;
            materials[3].customColor = (uint)B2HexColor.b2_colorDarkGray;
            materials[4].tangentSpeed = -50.0f;
            materials[4].customColor = (uint)B2HexColor.b2_colorDarkGreen;
            materials[5].tangentSpeed = -60.0f;
            materials[5].customColor = (uint)B2HexColor.b2_colorDarkKhaki;
            materials[6].tangentSpeed = -70.0f;
            materials[6].customColor = (uint)B2HexColor.b2_colorDarkMagenta;

            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = count;
            chainDef.isLoop = true;
            chainDef.materials = materials;
            chainDef.materialCount = count;

            b2CreateChain(groundId, ref chainDef);
        }
    }

    B2BodyId DropBall()
    {
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(110.0f, -30.0f);
        B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
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
