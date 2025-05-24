using Box2D.NET;
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
} 