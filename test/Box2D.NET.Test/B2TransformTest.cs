// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Test;

/// <summary>
/// Tests for B2Transform, which represents a 2D rigid body transformation.
/// A transform combines a position (translation) and a rotation.
/// 
/// Key concepts:
/// 1. A transform maps points from local space to world space
/// 2. The inverse transform maps points from world space back to local space
/// 3. Transforms can be combined (multiplied) to chain transformations
/// 4. The identity transform represents no transformation
/// </summary>
public class B2TransformTest
{
    [Test]
    public void Test_B2Transform_Identity()
    {
        // The identity transform represents no transformation
        // - Position is at origin (0,0)
        // - Rotation is zero (cos=1, sin=0)
        var identity = b2Transform_identity;
        
        Assert.That(identity.p.X, Is.EqualTo(0.0f).Within(FLT_EPSILON), "Identity transform should be at origin");
        Assert.That(identity.p.Y, Is.EqualTo(0.0f).Within(FLT_EPSILON), "Identity transform should be at origin");
        Assert.That(identity.q.c, Is.EqualTo(1.0f).Within(FLT_EPSILON), "Identity rotation should be zero (cos=1)");
        Assert.That(identity.q.s, Is.EqualTo(0.0f).Within(FLT_EPSILON), "Identity rotation should be zero (sin=0)");
    }

    [Test]
    public void Test_B2Transform_LocalToWorld()
    {
        // Create a transform that:
        // 1. Rotates 90 degrees counter-clockwise
        // 2. Translates by (1,2)
        var transform = new B2Transform(
            new B2Vec2(1.0f, 2.0f),  // Translation
            b2MakeRot(B2_PI / 2)     // 90 degree rotation
        );

        // Test transforming a point from local to world space
        var localPoint = new B2Vec2(1.0f, 0.0f);  // Point at (1,0) in local space
        var worldPoint = b2TransformPoint(transform, localPoint);

        // Expected result:
        // 1. First rotate (1,0) by 90 degrees -> (0,1)
        // 2. Then translate by (1,2) -> (1,3)
        Assert.That(worldPoint.X, Is.EqualTo(1.0f).Within(0.0001f), "X coordinate should be translated by 1");
        Assert.That(worldPoint.Y, Is.EqualTo(3.0f).Within(0.0001f), "Y coordinate should be rotated and translated");
    }

    [Test]
    public void Test_B2Transform_WorldToLocal()
    {
        // Create the same transform as above
        var transform = new B2Transform(
            new B2Vec2(1.0f, 2.0f),
            b2MakeRot(B2_PI / 2)
        );

        // Test transforming a point from world to local space
        var worldPoint = new B2Vec2(1.0f, 3.0f);  // Point at (1,3) in world space
        var localPoint = b2InvTransformPoint(transform, worldPoint);

        // Expected result:
        // 1. First subtract translation (1,2) -> (0,1)
        // 2. Then rotate back by -90 degrees -> (1,0)
        Assert.That(localPoint.X, Is.EqualTo(1.0f).Within(0.0001f), "X coordinate should be rotated back to 1");
        Assert.That(localPoint.Y, Is.EqualTo(0.0f).Within(0.0001f), "Y coordinate should be rotated back to 0");
    }

    [Test]
    public void Test_B2Transform_Combine()
    {
        // Create two transforms:
        // 1. First transform: rotate 45 degrees and translate by (1,0)
        // 2. Second transform: rotate 45 degrees and translate by (0,1)
        var transform1 = new B2Transform(
            new B2Vec2(1.0f, 0.0f),
            b2MakeRot(B2_PI / 4)
        );
        var transform2 = new B2Transform(
            new B2Vec2(0.0f, 1.0f),
            b2MakeRot(B2_PI / 4)
        );

        // Combine the transforms
        var combined = b2MulTransforms(transform1, transform2);

        // Test transforming a point through the combined transform
        var localPoint = new B2Vec2(1.0f, 0.0f);
        var worldPoint = b2TransformPoint(combined, localPoint);

        // Expected result:
        // 1. First apply transform2: rotate 45 degrees and translate by (0,1)
        //    - (1,0) rotated 45 degrees = (0.7071, 0.7071)
        //    - Then translate by (0,1) = (0.7071, 1.7071)
        // 2. Then apply transform1: rotate 45 degrees and translate by (1,0)
        //    - (0.7071, 1.7071) rotated 45 degrees = (-0.7071, 1.7071)
        //    - Then translate by (1,0) = (0.2929, 1.7071)
        Assert.That(worldPoint.X, Is.EqualTo(0.2929f).Within(0.0001f), "Combined transform should apply both translations and rotations");
        Assert.That(worldPoint.Y, Is.EqualTo(1.7071f).Within(0.0001f), "Combined transform should apply both translations and rotations");
    }

    [Test]
    public void Test_B2Transform_VectorRotation()
    {
        // Create a transform that rotates 90 degrees
        var transform = new B2Transform(
            new B2Vec2(1.0f, 2.0f),  // Translation doesn't affect vectors
            b2MakeRot(B2_PI / 2)
        );

        // Test rotating a vector (direction only, no position)
        var localVector = new B2Vec2(1.0f, 0.0f);  // Vector pointing right
        var worldVector = b2RotateVector(transform.q, localVector);

        // Expected result:
        // Rotate (1,0) by 90 degrees -> (0,1)
        Assert.That(worldVector.X, Is.EqualTo(0.0f).Within(0.0001f), "Vector should be rotated 90 degrees");
        Assert.That(worldVector.Y, Is.EqualTo(1.0f).Within(0.0001f), "Vector should be rotated 90 degrees");
    }

    [Test]
    public void Test_B2Transform_Validation()
    {
        // Test valid transform
        var validTransform = new B2Transform(
            new B2Vec2(1.0f, 2.0f),
            b2MakeRot(B2_PI / 4)
        );
        Assert.That(b2IsValidVec2(validTransform.p) && b2IsValidRotation(validTransform.q), 
            Is.True, "Transform with valid position and rotation should be valid");

        // Test invalid position (NaN)
        var invalidPosTransform = new B2Transform(
            new B2Vec2(float.NaN, 2.0f),
            b2MakeRot(B2_PI / 4)
        );
        Assert.That(b2IsValidVec2(invalidPosTransform.p) && b2IsValidRotation(invalidPosTransform.q), 
            Is.False, "Transform with NaN position should be invalid");

        // Test invalid rotation (not normalized)
        var invalidRotTransform = new B2Transform(
            new B2Vec2(1.0f, 2.0f),
            new B2Rot(2.0f, 0.0f)  // Not normalized
        );
        Assert.That(b2IsValidVec2(invalidRotTransform.p) && b2IsValidRotation(invalidRotTransform.q), 
            Is.False, "Transform with non-normalized rotation should be invalid");
    }
} 
