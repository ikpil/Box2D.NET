using Box2D.NET.Primitives;

namespace Box2D.NET.Samples.Primitives;

public struct ContactPoint
{
    public b2ShapeId shapeIdA;
    public b2ShapeId shapeIdB;
    public b2Vec2 normal;
    public b2Vec2 position;
    public bool persisted;
    public float normalImpulse;
    public float tangentImpulse;
    public float separation;
    public int constraintIndex;
    public int color;
};
