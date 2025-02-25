using Box2D.NET.Core;
using Box2D.NET.Primitives;

namespace Box2D.NET.Samples.Primitives;

// Context for ray cast callbacks. Do what you want with this.
public class RayCastContext
{
    public UnsafeArray3<b2Vec2> points;
    public UnsafeArray3<b2Vec2> normals;
    public UnsafeArray3<float> fractions;
    public int count;
};