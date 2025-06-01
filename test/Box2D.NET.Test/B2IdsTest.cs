// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using static Box2D.NET.B2Ids;

namespace Box2D.NET.Test;

/// <summary>
/// Tests for B2Ids class which manages Box2D.NET's ID system.
/// 
/// Key concepts:
/// 1. IDs are opaque handles to internal Box2D objects
/// 2. IDs contain index, world reference, and generation number
/// 3. IDs can be null (index1 = 0)
/// 4. IDs can be stored/loaded as ulong for serialization
/// </summary>
public class B2IdsTest
{
    [Test]
    public void Test_B2Ids_NullIds()
    {
        // Test that null IDs are properly initialized
        Assert.That(B2_IS_NULL(b2_nullWorldId), Is.True, "World null ID should be null");
        Assert.That(B2_IS_NULL(b2_nullBodyId), Is.True, "Body null ID should be null");
        Assert.That(B2_IS_NULL(b2_nullShapeId), Is.True, "Shape null ID should be null");
        Assert.That(B2_IS_NULL(b2_nullChainId), Is.True, "Chain null ID should be null");
        Assert.That(B2_IS_NULL(b2_nullJointId), Is.True, "Joint null ID should be null");
    }

    [Test]
    public void Test_B2Ids_NonNullIds()
    {
        // Test that non-null IDs are properly detected
        var worldId = new B2WorldId(1, 1);
        var bodyId = new B2BodyId(1, 1, 1);
        var shapeId = new B2ShapeId(1, 1, 1);
        var chainId = new B2ChainId(1, 1, 1);
        var jointId = new B2JointId(1, 1, 1);

        Assert.That(B2_IS_NON_NULL(worldId), Is.True, "World ID should be non-null");
        Assert.That(B2_IS_NON_NULL(bodyId), Is.True, "Body ID should be non-null");
        Assert.That(B2_IS_NON_NULL(shapeId), Is.True, "Shape ID should be non-null");
        Assert.That(B2_IS_NON_NULL(chainId), Is.True, "Chain ID should be non-null");
        Assert.That(B2_IS_NON_NULL(jointId), Is.True, "Joint ID should be non-null");
    }

    [Test]
    public void Test_B2Ids_BitPacking()
    {
        // Test that each field is stored in the correct bit position
        const int testIndex = 0x12345678;
        const ushort testWorld = 0xABCD;
        const ushort testGeneration = 0xEF01;

        var bodyId = new B2BodyId(testIndex, testWorld, testGeneration);
        ulong stored = b2StoreBodyId(bodyId);

        // Verify each field's position
        int loadedIndex = (int)(stored >> 32);
        ushort loadedWorld = (ushort)(stored >> 16);
        ushort loadedGeneration = (ushort)stored;

        Assert.That(loadedIndex, Is.EqualTo(testIndex), "index1 should be stored in highest 32 bits");
        Assert.That(loadedWorld, Is.EqualTo(testWorld), "world0 should be stored in middle 16 bits");
        Assert.That(loadedGeneration, Is.EqualTo(testGeneration), "generation should be stored in lowest 16 bits");

        // Test with maximum values
        var maxId = new B2BodyId(int.MaxValue, ushort.MaxValue, ushort.MaxValue);
        ulong maxStored = b2StoreBodyId(maxId);
        var loadedMax = b2LoadBodyId(maxStored);

        Assert.That(loadedMax.index1, Is.EqualTo(int.MaxValue), "Maximum index1 should be preserved");
        Assert.That(loadedMax.world0, Is.EqualTo(ushort.MaxValue), "Maximum world0 should be preserved");
        Assert.That(loadedMax.generation, Is.EqualTo(ushort.MaxValue), "Maximum generation should be preserved");
    }

    [Test]
    public void Test_B2Ids_StoreLoad()
    {
        // Test storing and loading IDs
        ulong testValue = 0x0123456789ABCDEFul;

        // Test BodyId
        var bodyId = b2LoadBodyId(testValue);
        ulong storedBodyId = b2StoreBodyId(bodyId);
        Assert.That(storedBodyId, Is.EqualTo(testValue), "Body ID should be preserved after store/load");

        // Test ShapeId
        var shapeId = b2LoadShapeId(testValue);
        ulong storedShapeId = b2StoreShapeId(shapeId);
        Assert.That(storedShapeId, Is.EqualTo(testValue), "Shape ID should be preserved after store/load");

        // Test ChainId
        var chainId = b2LoadChainId(testValue);
        ulong storedChainId = b2StoreChainId(chainId);
        Assert.That(storedChainId, Is.EqualTo(testValue), "Chain ID should be preserved after store/load");

        // Test JointId
        var jointId = b2LoadJointId(testValue);
        ulong storedJointId = b2StoreJointId(jointId);
        Assert.That(storedJointId, Is.EqualTo(testValue), "Joint ID should be preserved after store/load");
    }

    [Test]
    public void Test_B2Ids_Equality()
    {
        // Test ID equality comparison
        var bodyId1 = new B2BodyId(1, 1, 1);
        var bodyId2 = new B2BodyId(1, 1, 1);
        var bodyId3 = new B2BodyId(2, 1, 1);

        Assert.That(B2_ID_EQUALS(bodyId1, bodyId2), Is.True, "Identical IDs should be equal");
        Assert.That(B2_ID_EQUALS(bodyId1, bodyId3), Is.False, "Different IDs should not be equal");

        var shapeId1 = new B2ShapeId(1, 1, 1);
        var shapeId2 = new B2ShapeId(1, 1, 1);
        var shapeId3 = new B2ShapeId(2, 1, 1);

        Assert.That(B2_ID_EQUALS(shapeId1, shapeId2), Is.True, "Identical shape IDs should be equal");
        Assert.That(B2_ID_EQUALS(shapeId1, shapeId3), Is.False, "Different shape IDs should not be equal");
    }

    [Test]
    public void Test_B2Ids_BodyIdComparison()
    {
        // Test B2BodyId comparison operators
        var id1 = new B2BodyId(1, 1, 1);
        var id2 = new B2BodyId(2, 1, 1);
        var id3 = new B2BodyId(1, 2, 1);
        var id4 = new B2BodyId(1, 1, 2);
        var id5 = new B2BodyId(1, 1, 1);

        // Test less than
        Assert.That(id1 < id2, Is.True, "ID with smaller index1 should be less");
        Assert.That(id1 < id3, Is.True, "ID with smaller world0 should be less");
        Assert.That(id1 < id4, Is.True, "ID with smaller generation should be less");

        // Test greater than
        Assert.That(id2 > id1, Is.True, "ID with larger index1 should be greater");
        Assert.That(id3 > id1, Is.True, "ID with larger world0 should be greater");
        Assert.That(id4 > id1, Is.True, "ID with larger generation should be greater");

        // Test equality
        Assert.That(id1 == id5, Is.True, "Identical IDs should be equal");
        Assert.That(id1 != id2, Is.True, "Different IDs should not be equal");
    }

    [Test]
    public void Test_B2Ids_Generation()
    {
        // Test that generation numbers are preserved for all ID types
        const ushort testGeneration = 123;

        // Test BodyId
        var bodyId = new B2BodyId(1, 1, testGeneration);
        ulong storedBody = b2StoreBodyId(bodyId);
        var loadedBody = b2LoadBodyId(storedBody);
        Assert.That(loadedBody.generation, Is.EqualTo(testGeneration), "Body ID generation number should be preserved");

        // Test ShapeId
        var shapeId = new B2ShapeId(1, 1, testGeneration);
        ulong storedShape = b2StoreShapeId(shapeId);
        var loadedShape = b2LoadShapeId(storedShape);
        Assert.That(loadedShape.generation, Is.EqualTo(testGeneration), "Shape ID generation number should be preserved");

        // Test ChainId
        var chainId = new B2ChainId(1, 1, testGeneration);
        ulong storedChain = b2StoreChainId(chainId);
        var loadedChain = b2LoadChainId(storedChain);
        Assert.That(loadedChain.generation, Is.EqualTo(testGeneration), "Chain ID generation number should be preserved");

        // Test JointId
        var jointId = new B2JointId(1, 1, testGeneration);
        ulong storedJoint = b2StoreJointId(jointId);
        var loadedJoint = b2LoadJointId(storedJoint);
        Assert.That(loadedJoint.generation, Is.EqualTo(testGeneration), "Joint ID generation number should be preserved");
    }
} 
