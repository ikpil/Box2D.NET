using Box2D.NET.Primitives;
using static Box2D.NET.id;

namespace Box2D.NET.Samples.Primitives;

public struct QueryContext
{
    public b2Vec2 point;
    public b2BodyId bodyId = b2_nullBodyId;
}