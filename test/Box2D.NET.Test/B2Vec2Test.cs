using NUnit.Framework;

namespace Box2D.NET.Test;

public class B2Vec2Test
{
    [Test]
    public void Test_B2Vec2_Constructor()
    {
        // Case 1: Basic initialization
        var vec = new B2Vec2(1.0f, 2.0f);
        Assert.That(vec.X, Is.EqualTo(1.0f));
        Assert.That(vec.Y, Is.EqualTo(2.0f));

        // Case 2: Zero initialization
        var zeroVec = new B2Vec2(0.0f, 0.0f);
        Assert.That(zeroVec.X, Is.EqualTo(0.0f));
        Assert.That(zeroVec.Y, Is.EqualTo(0.0f));

        // Case 3: Negative values
        var negVec = new B2Vec2(-1.0f, -2.0f);
        Assert.That(negVec.X, Is.EqualTo(-1.0f));
        Assert.That(negVec.Y, Is.EqualTo(-2.0f));
    }

    [Test]
    public void Test_B2Vec2_UnaryNegate()
    {
        // Case 1: Positive to negative
        var vec = new B2Vec2(1.0f, 2.0f);
        var result = -vec;
        Assert.That(result.X, Is.EqualTo(-1.0f));
        Assert.That(result.Y, Is.EqualTo(-2.0f));

        // Case 2: Negative to positive
        var negVec = new B2Vec2(-1.0f, -2.0f);
        var result2 = -negVec;
        Assert.That(result2.X, Is.EqualTo(1.0f));
        Assert.That(result2.Y, Is.EqualTo(2.0f));

        // Case 3: Zero remains zero
        var zeroVec = new B2Vec2(0.0f, 0.0f);
        var result3 = -zeroVec;
        Assert.That(result3.X, Is.EqualTo(0.0f));
        Assert.That(result3.Y, Is.EqualTo(0.0f));
    }

    [Test]
    public void Test_B2Vec2_Addition()
    {
        // Case 1: Basic addition
        var vec1 = new B2Vec2(1.0f, 2.0f);
        var vec2 = new B2Vec2(3.0f, 4.0f);
        var result = vec1 + vec2;
        Assert.That(result.X, Is.EqualTo(4.0f));
        Assert.That(result.Y, Is.EqualTo(6.0f));

        // Case 2: Addition with negative values
        var vec3 = new B2Vec2(-1.0f, -2.0f);
        var vec4 = new B2Vec2(3.0f, 4.0f);
        var result2 = vec3 + vec4;
        Assert.That(result2.X, Is.EqualTo(2.0f));
        Assert.That(result2.Y, Is.EqualTo(2.0f));

        // Case 3: Addition with zero
        var vec5 = new B2Vec2(1.0f, 2.0f);
        var zeroVec = new B2Vec2(0.0f, 0.0f);
        var result3 = vec5 + zeroVec;
        Assert.That(result3.X, Is.EqualTo(1.0f));
        Assert.That(result3.Y, Is.EqualTo(2.0f));
    }

    [Test]
    public void Test_B2Vec2_Subtraction()
    {
        // Case 1: Basic subtraction
        var vec1 = new B2Vec2(3.0f, 4.0f);
        var vec2 = new B2Vec2(1.0f, 2.0f);
        var result = vec1 - vec2;
        Assert.That(result.X, Is.EqualTo(2.0f));
        Assert.That(result.Y, Is.EqualTo(2.0f));

        // Case 2: Subtraction with negative values
        var vec3 = new B2Vec2(-1.0f, -2.0f);
        var vec4 = new B2Vec2(3.0f, 4.0f);
        var result2 = vec3 - vec4;
        Assert.That(result2.X, Is.EqualTo(-4.0f));
        Assert.That(result2.Y, Is.EqualTo(-6.0f));

        // Case 3: Subtraction with zero
        var vec5 = new B2Vec2(1.0f, 2.0f);
        var zeroVec = new B2Vec2(0.0f, 0.0f);
        var result3 = vec5 - zeroVec;
        Assert.That(result3.X, Is.EqualTo(1.0f));
        Assert.That(result3.Y, Is.EqualTo(2.0f));
    }

    [Test]
    public void Test_B2Vec2_ScalarMultiplication()
    {
        // Case 1: Basic multiplication
        var vec = new B2Vec2(1.0f, 2.0f);
        float scalar = 2.0f;
        var result1 = scalar * vec;
        var result2 = vec * scalar;
        Assert.That(result1.X, Is.EqualTo(2.0f));
        Assert.That(result1.Y, Is.EqualTo(4.0f));
        Assert.That(result2.X, Is.EqualTo(2.0f));
        Assert.That(result2.Y, Is.EqualTo(4.0f));

        // Case 2: Multiplication with negative scalar
        var vec2 = new B2Vec2(1.0f, 2.0f);
        float negScalar = -2.0f;
        var result3 = negScalar * vec2;
        Assert.That(result3.X, Is.EqualTo(-2.0f));
        Assert.That(result3.Y, Is.EqualTo(-4.0f));

        // Case 3: Multiplication with zero
        var vec3 = new B2Vec2(1.0f, 2.0f);
        float zeroScalar = 0.0f;
        var result4 = zeroScalar * vec3;
        Assert.That(result4.X, Is.EqualTo(0.0f));
        Assert.That(result4.Y, Is.EqualTo(0.0f));
    }

    [Test]
    public void Test_B2Vec2_Equality()
    {
        // Case 1: Exact equality
        var vec1 = new B2Vec2(1.0f, 2.0f);
        var vec2 = new B2Vec2(1.0f, 2.0f);
        Assert.That(vec1 == vec2, Is.True);
        Assert.That(vec1 != vec2, Is.False);

        // Case 2: Within epsilon
        var vec3 = new B2Vec2(1.0f + B2MathFunction.FLT_EPSILON * 0.5f, 2.0f);
        var vec4 = new B2Vec2(1.0f, 2.0f);
        Assert.That(vec3 == vec4, Is.True);
        Assert.That(vec3 != vec4, Is.False);

        // Case 3: Beyond epsilon
        var vec5 = new B2Vec2(1.0f + B2MathFunction.FLT_EPSILON * 2.0f, 2.0f);
        var vec6 = new B2Vec2(1.0f, 2.0f);
        Assert.That(vec5 == vec6, Is.False);
        Assert.That(vec5 != vec6, Is.True);

        // Case 4: NaN handling
        var vec7 = new B2Vec2(float.NaN, 2.0f);
        var vec8 = new B2Vec2(1.0f, 2.0f);
        Assert.That(vec7 == vec8, Is.False);
        Assert.That(vec7 != vec8, Is.True);

        // Case 5: Infinity handling
        var vec9 = new B2Vec2(float.PositiveInfinity, 2.0f);
        var vec10 = new B2Vec2(float.PositiveInfinity, 2.0f);
        Assert.That(vec9 == vec10, Is.True);  // Infinity == Infinity in C/C++
        Assert.That(vec9 != vec10, Is.False);

        // Case 5.1: Different infinity signs
        var vec9_1 = new B2Vec2(float.PositiveInfinity, 2.0f);
        var vec10_1 = new B2Vec2(float.NegativeInfinity, 2.0f);
        Assert.That(vec9_1 == vec10_1, Is.False);  // +Infinity != -Infinity
        Assert.That(vec9_1 != vec10_1, Is.True);

        // Case 6: Zero comparison
        var vec11 = new B2Vec2(0.0f, 0.0f);
        var vec12 = new B2Vec2(0.0f, 0.0f);
        Assert.That(vec11 == vec12, Is.True);
        Assert.That(vec11 != vec12, Is.False);

        // Case 7: Very small numbers
        var vec13 = new B2Vec2(float.Epsilon, float.Epsilon);
        var vec14 = new B2Vec2(0.0f, 0.0f);
        Assert.That(vec13 == vec14, Is.False);
        Assert.That(vec13 != vec14, Is.True);

        // Case 8: Self comparison
        var vec15 = new B2Vec2(1.0f, 2.0f);
        Assert.That(vec15 == vec15, Is.True);
        Assert.That(vec15 != vec15, Is.False);
    }

    [Test]
    public void Test_B2Vec2_Equality_EdgeCases()
    {
        // Case 1: Maximum float values
        var vec1 = new B2Vec2(float.MaxValue, float.MaxValue);
        var vec2 = new B2Vec2(float.MaxValue, float.MaxValue);
        Assert.That(vec1 == vec2, Is.True);
        Assert.That(vec1 != vec2, Is.False);

        // Case 2: Minimum float values
        var vec3 = new B2Vec2(float.MinValue, float.MinValue);
        var vec4 = new B2Vec2(float.MinValue, float.MinValue);
        Assert.That(vec3 == vec4, Is.True);
        Assert.That(vec3 != vec4, Is.False);

        // Case 3: Mixed signs
        var vec5 = new B2Vec2(-1.0f, 1.0f);
        var vec6 = new B2Vec2(-1.0f, 1.0f);
        Assert.That(vec5 == vec6, Is.True);
        Assert.That(vec5 != vec6, Is.False);

        // Case 4: Very small numbers (using float.Epsilon)
        var vec7 = new B2Vec2(float.Epsilon, float.Epsilon);
        var vec8 = new B2Vec2(0.0f, 0.0f);
        Assert.That(vec7 == vec8, Is.False);  // float.Epsilon은 0이 아님
        Assert.That(vec7 != vec8, Is.True);
    }
}