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

    [Test]
    public void Test_b2AABB_Overlaps()
    {
        // Case 1: Partial overlap -> Should return true
        var a1 = new B2AABB();
        a1.lowerBound = new B2Vec2(0, 0);
        a1.upperBound = new B2Vec2(5, 5);
        var b1 = new B2AABB();
        b1.lowerBound = new B2Vec2(3, 3);
        b1.upperBound = new B2Vec2(7, 7);
        Assert.That(b2AABB_Overlaps(a1, b1), Is.True, "Partial overlap should return true.");

        // Case 2: Full containment -> Should return true
        var a2 = new B2AABB();
        a2.lowerBound = new B2Vec2(0, 0);
        a2.upperBound = new B2Vec2(5, 5);
        var b2 = new B2AABB();
        b2.lowerBound = new B2Vec2(1, 1);
        b2.upperBound = new B2Vec2(4, 4);
        Assert.That(b2AABB_Overlaps(a2, b2), Is.True, "Full containment should return true.");

        // Case 3: Identical AABBs -> Should return true
        var a3 = new B2AABB();
        a3.lowerBound = new B2Vec2(2, 2);
        a3.upperBound = new B2Vec2(6, 6);
        var b3 = new B2AABB();
        b3.lowerBound = new B2Vec2(2, 2);
        b3.upperBound = new B2Vec2(6, 6);
        Assert.That(b2AABB_Overlaps(a3, b3), Is.True, "Identical AABBs should return true.");

        // Case 4: Distant AABBs -> Should return false
        var a4 = new B2AABB();
        a4.lowerBound = new B2Vec2(0, 0);
        a4.upperBound = new B2Vec2(2, 2);
        var b4 = new B2AABB();
        b4.lowerBound = new B2Vec2(5, 5);
        b4.upperBound = new B2Vec2(6, 6);
        Assert.That(b2AABB_Overlaps(a4, b4), Is.False, "Separated AABBs should return false.");

        // Case 5: Touching at edge -> According to current implementation, should return true
        var a5 = new B2AABB();
        a5.lowerBound = new B2Vec2(0, 0);
        a5.upperBound = new B2Vec2(2, 2);
        var b5 = new B2AABB();
        b5.lowerBound = new B2Vec2(2, 0);
        b5.upperBound = new B2Vec2(4, 2);
        Assert.That(b2AABB_Overlaps(a5, b5), Is.True, "Touching edges are treated as overlap in current implementation.");

        // Case 6: Touching at corner -> According to current implementation, should return true
        var a6 = new B2AABB();
        a6.lowerBound = new B2Vec2(0, 0);
        a6.upperBound = new B2Vec2(2, 2);
        var b6 = new B2AABB();
        b6.lowerBound = new B2Vec2(2, 2);
        b6.upperBound = new B2Vec2(4, 4);
        Assert.That(b2AABB_Overlaps(a6, b6), Is.True, "Touching corners are treated as overlap in current implementation.");

        // Case 7: Symmetry test -> a and b flipped should give same result
        var a7 = new B2AABB();
        a7.lowerBound = new B2Vec2(1, 1);
        a7.upperBound = new B2Vec2(4, 4);
        var b7 = new B2AABB();
        b7.lowerBound = new B2Vec2(2, 2);
        b7.upperBound = new B2Vec2(5, 5);
        Assert.That(b2AABB_Overlaps(a7, b7), Is.EqualTo(b2AABB_Overlaps(b7, a7)), "Overlap should be symmetric.");

        // Case 8: Invalid AABB (lower > upper) -> Depends on policy (assumed false)
        var a8 = new B2AABB();
        a8.lowerBound = new B2Vec2(5, 5);
        a8.upperBound = new B2Vec2(0, 0); // Invalid
        var b8 = new B2AABB();
        b8.lowerBound = new B2Vec2(1, 1);
        b8.upperBound = new B2Vec2(3, 3);
        Assert.That(b2AABB_Overlaps(a8, b8), Is.False, "Invalid AABB should not overlap (based on policy).");
    }

    [Test]
    public void Test_b2IsValidAABB()
    {
        // Case 1: Valid AABB (upper bound greater than lower bound)
        var a1 = new B2AABB();
        a1.lowerBound = new B2Vec2(0, 0);
        a1.upperBound = new B2Vec2(5, 5);
        Assert.That(b2IsValidAABB(a1), Is.True, "Valid AABB should return true.");

        // Case 2: Upper bound equals lower bound (valid, as no negative dimension)
        var a2 = new B2AABB();
        a2.lowerBound = new B2Vec2(2, 2);
        a2.upperBound = new B2Vec2(2, 2); // Same for both bounds
        Assert.That(b2IsValidAABB(a2), Is.True, "AABB with equal upper and lower bounds should still be valid.");

        // Case 3: Lower bound is greater than upper bound (invalid)
        var a3 = new B2AABB();
        a3.lowerBound = new B2Vec2(5, 5);
        a3.upperBound = new B2Vec2(2, 2);
        Assert.That(b2IsValidAABB(a3), Is.False, "AABB where lower bound is greater than upper bound should be invalid.");

        // Case 4: Contains NaN value (invalid)
        var a4 = new B2AABB();
        a4.lowerBound = new B2Vec2(float.NaN, 0);
        a4.upperBound = new B2Vec2(5, 5);
        Assert.That(b2IsValidAABB(a4), Is.False, "AABB with NaN value should be invalid.");

        // Case 5: Contains Infinity value (invalid)
        var a5 = new B2AABB();
        a5.lowerBound = new B2Vec2(float.PositiveInfinity, 0);
        a5.upperBound = new B2Vec2(5, 5);
        Assert.That(b2IsValidAABB(a5), Is.False, "AABB with Infinity value should be invalid.");
    }
}