using Box2D.NET.Primitives;

namespace Box2D.NET.Samples.Primitives;

public class OverlapResult
{
    public b2Vec2[] points = new b2Vec2[32];
    public int count;
}