using NUnit.Framework;
using static Box2D.NET.B2AABBs;

namespace Box2D.NET.Test;

public class B2AABBTests
{
    [Test]
    public void Test_b2Perimeter()
    {
        // zero
        {
            B2AABB aabb = new B2AABB();
            aabb.lowerBound = new B2Vec2(1.0f, 1.0f);
            aabb.upperBound = new B2Vec2(1.0f, 1.0f);

            float result = b2Perimeter(aabb);
            Assert.That(result, Is.EqualTo(0.0f));
        }

        {
            B2AABB aabb = new B2AABB();
            aabb.lowerBound = new B2Vec2(0.0f, 0.0f);
            aabb.upperBound = new B2Vec2(3.0f, 4.0f);

            float result = b2Perimeter(aabb);

            Assert.That(result, Is.EqualTo(14.0f));
        }

        {
            B2AABB aabb = new B2AABB();
            aabb.lowerBound = new B2Vec2(-2.0f, -2.0f);
            aabb.upperBound = new B2Vec2(2.0f, 3.0f);

            float result = b2Perimeter(aabb);
            Assert.That(result, Is.EqualTo(18.0f));
        }

        {
            B2AABB aabb = new B2AABB();
            aabb.lowerBound = new B2Vec2(0.0f, 0.0f);
            aabb.upperBound = new B2Vec2(float.MaxValue, float.MaxValue);

            float result = b2Perimeter(aabb);

            Assert.That(result, Is.EqualTo(float.PositiveInfinity));
        }

        {
            B2AABB aabb = new B2AABB();
            aabb.lowerBound = new B2Vec2(0.0f, 0.0f);
            aabb.upperBound = new B2Vec2(float.MinValue, float.MinValue);

            float result = b2Perimeter(aabb);

            Assert.That(result, Is.EqualTo(4 * float.MinValue));
        }
    }

    [Test]
    public void Test_b2EnlargeAABB()
    {
        // enlarge
        {
            var a = new B2AABB();
            a.lowerBound = new B2Vec2(0, 0);
            a.upperBound = new B2Vec2(1, 1);

            var b = new B2AABB();
            b.lowerBound = new B2Vec2(-1, -1);
            b.upperBound = new B2Vec2(2, 2);

            var result = b2EnlargeAABB(ref a, b);

            Assert.That(result);
            Assert.That(a.lowerBound, Is.EqualTo(new B2Vec2(-1, -1)));
            Assert.That(a.upperBound, Is.EqualTo(new B2Vec2(2, 2)));
        }
        
        // no enlarge
        {
            var a = new B2AABB();
            a.lowerBound = new B2Vec2(0, 0);
            a.upperBound = new B2Vec2(5, 5);

            var b = new B2AABB();
            b.lowerBound = new B2Vec2(3, 2);
            b.upperBound = new B2Vec2(4, 5);

            var result = b2EnlargeAABB(ref a, b);

            Assert.That(!result);
            Assert.That(a.lowerBound, Is.EqualTo(new B2Vec2(0, 0)));
            Assert.That(a.upperBound, Is.EqualTo(new B2Vec2(5, 5)));
        }
    }
}