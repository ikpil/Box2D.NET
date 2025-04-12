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

    [Test]
    public void Test_b2AABB_RayCast()
    {
        // Case 1: Ray does not intersect the AABB (outside)
        var aabb1 = new B2AABB();
        aabb1.lowerBound = new B2Vec2(1, 1);
        aabb1.upperBound = new B2Vec2(3, 3);
        var startPoint1 = new B2Vec2(0, 0); // Start point outside AABB
        var endPoint1 = new B2Vec2(0, 4); // End point (ray direction)
        var result1 = b2AABB_RayCast(aabb1, startPoint1, endPoint1);
        Assert.That(result1.hit, Is.False, "Ray should not intersect the AABB.");

        // Case 2: Ray intersects AABB (inside AABB)
        var aabb2 = new B2AABB();
        aabb2.lowerBound = new B2Vec2(1, 1);
        aabb2.upperBound = new B2Vec2(3, 3);
        var startPoint2 = new B2Vec2(0, 0); // Start point outside AABB
        var endPoint2 = new B2Vec2(2, 2); // End point (ray direction)
        var result2 = b2AABB_RayCast(aabb2, startPoint2, endPoint2);
        Assert.That(result2.hit, Is.True, "Ray should intersect the AABB.");
        Assert.That(result2.fraction, Is.EqualTo(0.5f), "Intersection point should be correct.");

        // Case 3: Ray starts inside the AABB but goes outside (no intersection outside)
        var aabb3 = new B2AABB();
        aabb3.lowerBound = new B2Vec2(1, 1);
        aabb3.upperBound = new B2Vec2(3, 3);
        var startPoint3 = new B2Vec2(2, 2); // Start point inside AABB
        var endPoint3 = new B2Vec2(4, 4); // End point (ray direction goes outside)
        var result3 = b2AABB_RayCast(aabb3, startPoint3, endPoint3);
        Assert.That(result3.hit, Is.False, "Ray starting inside but going outside should not intersect.");

        // Case 4: Ray intersects AABB at boundary (exactly on the edge)
        var aabb4 = new B2AABB();
        aabb4.lowerBound = new B2Vec2(1, 1);
        aabb4.upperBound = new B2Vec2(3, 3);
        var startPoint4 = new B2Vec2(0, 1); // Start point at edge of AABB
        var endPoint4 = new B2Vec2(4, 1); // End point (ray direction, horizontal)
        var result4 = b2AABB_RayCast(aabb4, startPoint4, endPoint4);
        Assert.That(result4.hit, Is.True, "Ray should intersect at the boundary of the AABB.");
        Assert.That(result4.fraction, Is.EqualTo(0.25f), "Intersection fraction should be 0.25 at the boundary.");

        // Case 5: Ray starts inside and goes out (no intersection outside)
        var aabb5 = new B2AABB();
        aabb5.lowerBound = new B2Vec2(1, 1);
        aabb5.upperBound = new B2Vec2(3, 3);
        var startPoint5 = new B2Vec2(2, 2); // Start inside AABB
        var endPoint5 = new B2Vec2(4, 4); // End point (ray direction goes outside)
        var result5 = b2AABB_RayCast(aabb5, startPoint5, endPoint5);
        Assert.That(result5.hit, Is.False, "Ray going out of AABB should not intersect.");
    }
}