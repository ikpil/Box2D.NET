// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Shapes;

public class ChainShape : Sample
{
    private static readonly int SampleChainShape = SampleFactory.Shared.RegisterSample("Shapes", "Chain Shape", Create);

    enum ShapeType
    {
        e_circleShape = 0,
        e_capsuleShape,
        e_boxShape
    };

    private B2BodyId m_groundId;
    private B2BodyId m_bodyId;
    private B2ChainId m_chainId;
    private ShapeType m_shapeType;
    private B2ShapeId m_shapeId;
    private float m_restitution;
    private float m_friction;

    private static Sample Create(SampleContext context)
    {
        return new ChainShape(context);
    }

    public ChainShape(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 0.0f);
            m_context.camera.m_zoom = 25.0f * 1.75f;
        }

        m_groundId = b2_nullBodyId;
        m_bodyId = b2_nullBodyId;
        m_chainId = b2_nullChainId;
        m_shapeId = b2_nullShapeId;
        m_shapeType = ShapeType.e_circleShape;
        m_restitution = 0.0f;
        m_friction = 0.2f;

        CreateScene();
        Launch();
    }

    void CreateScene()
    {
        if (B2_IS_NON_NULL(m_groundId))
        {
            b2DestroyBody(m_groundId);
        }

        // https://betravis.github.io/shape-tools/path-to-polygon/
        // B2Vec2 points[] = {{-20.58325, 14.54175}, {-21.90625, 15.8645},		 {-24.552, 17.1875},
        //				   {-27.198, 11.89575},	  {-29.84375, 15.8645},		 {-29.84375, 21.15625},
        //				   {-25.875, 23.802},	  {-20.58325, 25.125},		 {-25.875, 29.09375},
        //				   {-20.58325, 31.7395},  {-11.0089998, 23.2290001}, {-8.67700005, 21.15625},
        //				   {-6.03125, 21.15625},  {-7.35424995, 29.09375},	 {-3.38549995, 29.09375},
        //				   {1.90625, 30.41675},	  {5.875, 17.1875},			 {11.16675, 25.125},
        //				   {9.84375, 29.09375},	  {13.8125, 31.7395},		 {21.75, 30.41675},
        //				   {28.3644981, 26.448},  {25.71875, 18.5105},		 {24.3957481, 13.21875},
        //				   {17.78125, 11.89575},  {15.1355, 7.92700005},	 {5.875, 9.25},
        //				   {1.90625, 11.89575},	  {-3.25, 11.89575},		 {-3.25, 9.9375},
        //				   {-4.70825005, 9.25},	  {-8.67700005, 9.25},		 {-11.323, 11.89575},
        //				   {-13.96875, 11.89575}, {-15.29175, 14.54175},	 {-19.2605, 14.54175}};

        B2Vec2[] points = new B2Vec2[]
        {
            new B2Vec2(-56.885498f, 12.8985004f), new B2Vec2(-56.885498f, 16.2057495f), new B2Vec2(56.885498f, 16.2057495f), new B2Vec2(56.885498f, -16.2057514f),
            new B2Vec2(51.5935059f, -16.2057514f), new B2Vec2(43.6559982f, -10.9139996f), new B2Vec2(35.7184982f, -10.9139996f), new B2Vec2(27.7809982f, -10.9139996f),
            new B2Vec2(21.1664963f, -14.2212505f), new B2Vec2(11.9059982f, -16.2057514f), new B2Vec2(0, -16.2057514f), new B2Vec2(-10.5835037f, -14.8827496f),
            new B2Vec2(-17.1980019f, -13.5597477f), new B2Vec2(-21.1665001f, -12.2370014f), new B2Vec2(-25.1355019f, -9.5909977f), new B2Vec2(-31.75f, -3.63799858f),
            new B2Vec2(-38.3644981f, 6.2840004f), new B2Vec2(-42.3334999f, 9.59125137f), new B2Vec2(-47.625f, 11.5755005f), new B2Vec2(-56.885498f, 12.8985004f),
        };

        int count = points.Length;

        // float scale = 0.25f;
        // B2Vec2 lower = {float.MaxValue, float.MaxValue};
        // B2Vec2 upper = {-float.MaxValue, -float.MaxValue};
        // for (int i = 0; i < count; ++i)
        //{
        //	points[i].x = 2.0f * scale * points[i].x;
        //	points[i].y = -scale * points[i].y;

        //	lower = b2Min(lower, points[i]);
        //	upper = b2Max(upper, points[i]);
        //}

        // B2Vec2 center = b2MulSV(0.5f, b2Add(lower, upper));
        // for (int i = 0; i < count; ++i)
        //{
        //	points[i] = b2Sub(points[i], center);
        // }

        // for (int i = 0; i < count / 2; ++i)
        //{
        //	B2Vec2 temp = points[i];
        //	points[i] = points[count - 1 - i];
        //	points[count - 1 - i] = temp;
        // }

        // Logger.Information("{");
        // for (int i = 0; i < count; ++i)
        //{
        //	Logger.Information("{%.9g, %.9g},", points[i].x, points[i].y);
        // }
        // Logger.Information("};\n");

        B2SurfaceMaterial material = new B2SurfaceMaterial();
        material.friction = 0.2f;
        material.customColor = (uint)B2HexColor.b2_colorSteelBlue;
        material.userMaterialId = 42;

        B2ChainDef chainDef = b2DefaultChainDef();
        chainDef.points = points;
        chainDef.count = count;
        chainDef.materials = [material];
        chainDef.materialCount = 1;
        chainDef.isLoop = true;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        m_groundId = b2CreateBody(m_worldId, ref bodyDef);

        m_chainId = b2CreateChain(m_groundId, ref chainDef);
    }

    void Launch()
    {
        if (B2_IS_NON_NULL(m_bodyId))
        {
            b2DestroyBody(m_bodyId);
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(-55.0f, 13.5f);
        m_bodyId = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.material.friction = m_friction;
        shapeDef.material.restitution = m_restitution;

        if (m_shapeType == ShapeType.e_circleShape)
        {
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
            m_shapeId = b2CreateCircleShape(m_bodyId, ref shapeDef, ref circle);
        }
        else if (m_shapeType == ShapeType.e_capsuleShape)
        {
            B2Capsule capsule = new B2Capsule(new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), 0.25f);
            m_shapeId = b2CreateCapsuleShape(m_bodyId, ref shapeDef, ref capsule);
        }
        else
        {
            float h = 0.5f;
            B2Polygon box = b2MakeBox(h, h);
            m_shapeId = b2CreatePolygonShape(m_bodyId, ref shapeDef, ref box);
        }

        // b2_toiCalls = 0;
        // b2_toiHitCount = 0;

        m_stepCount = 0;
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        m_context.draw.DrawSegment(b2Vec2_zero, new B2Vec2(0.5f, 0.0f), B2HexColor.b2_colorRed);
        m_context.draw.DrawSegment(b2Vec2_zero, new B2Vec2(0.0f, 0.5f), B2HexColor.b2_colorGreen);

        // DrawTextLine($"toi calls, hits = {b2_toiCalls}, {b2_toiHitCount}");

        float height = 155.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Chain Shape", ImGuiWindowFlags.NoResize);

        string[] shapeTypes = { "Circle", "Capsule", "Box" };
        int shapeType = (int)m_shapeType;
        if (ImGui.Combo("Shape", ref shapeType, shapeTypes, shapeTypes.Length))
        {
            m_shapeType = (ShapeType)shapeType;
            Launch();
        }

        if (ImGui.SliderFloat("Friction", ref m_friction, 0.0f, 1.0f, "%.2f"))
        {
            b2Shape_SetFriction(m_shapeId, m_friction);
            b2Chain_SetFriction(m_chainId, m_friction);
        }

        if (ImGui.SliderFloat("Restitution", ref m_restitution, 0.0f, 2.0f, "%.1f"))
        {
            b2Shape_SetRestitution(m_shapeId, m_restitution);
        }

        if (ImGui.Button("Launch"))
        {
            Launch();
        }

        ImGui.End();
    }
}