// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Constants;

namespace Box2D.NET.Test;

public class B2ArrayTests
{
    public class DummyObject<T>
    {
        public T Value;

        public DummyObject()
        {
            Value = default(T);
        }

        public DummyObject(T value)
        {
            Value = value;
        }
    }

    public struct DummyStruct
    {
        public int Key;
        public float Value;

        public DummyStruct(int key, float value)
        {
            Key = key;
            Value = value;
        }
    }


    [Test]
    public void Test_b2Array_Destroy_Should_ClearDataAndResetCountAndCapacity()
    {
        // Create array
        var array = b2Array_Create<int>(10);
        array.count = 5; // simulate added elements

        // Destroy it
        b2Array_Destroy(ref array);

        // Validate reset
        Assert.That(array.data, Is.Null);
        Assert.That(array.count, Is.EqualTo(0));
        Assert.That(array.capacity, Is.EqualTo(0));
    }

    [Test]
    public void Test_b2Array_Create_Should_InitializeArrayCorrectly_ForAllTypes()
    {
        // Test with value type (int)
        var intArray = b2Array_Create<int>(5);
        Assert.That(intArray.capacity, Is.EqualTo(5));
        Assert.That(intArray.count, Is.EqualTo(0));
        Assert.That(intArray.data, Is.Not.Null);
        for (int i = 0; i < 5; ++i)
        {
            Assert.That(intArray.data[i], Is.EqualTo(0)); // default(int)
        }

        // Test with reference type (DummyObject)
        var objArray = b2Array_Create<DummyObject<int>>(3);
        Assert.That(objArray.capacity, Is.EqualTo(3));
        Assert.That(objArray.count, Is.EqualTo(0));
        Assert.That(objArray.data, Is.Not.Null);
        for (int i = 0; i < 3; ++i)
        {
            Assert.That(objArray.data[i], Is.Not.Null); // should be new DummyObject()
            Assert.That(objArray.data[i].Value, Is.EqualTo(0));
        }

        // Test with zero capacity
        var emptyArray = b2Array_Create<float>(0);
        Assert.That(emptyArray.capacity, Is.EqualTo(0));
        Assert.That(emptyArray.count, Is.EqualTo(0));
        Assert.That(emptyArray.data, Is.Null); // should remain null
    }

    [Test]
    public void Test_b2Array_Reserve_Should_ExpandOnlyIfNewCapacityIsGreater()
    {
        // Create array with initial capacity
        var array = b2Array_Create<int>(8);
        array.count = 5;
        var originalData = array.data;

        // Reserve with smaller value — should do nothing
        b2Array_Reserve(ref array, 4);

        // Validate that nothing changed
        Assert.That(array.capacity, Is.EqualTo(8));
        Assert.That(array.count, Is.EqualTo(5));
        Assert.That(array.data, Is.SameAs(originalData));

        // Reserve with larger value — should grow
        b2Array_Reserve(ref array, 16);

        // Validate updated capacity and data
        Assert.That(array.capacity, Is.EqualTo(16));
        Assert.That(array.count, Is.EqualTo(5));
        Assert.That(array.data, Is.Not.Null);
        Assert.That(array.data.Length, Is.GreaterThanOrEqualTo(16));
        Assert.That(array.data, Is.Not.SameAs(originalData));
    }

    [Test]
    public void Test_b2Array_Clear_Should_ResetCountWithoutTouchingDataOrCapacity()
    {
        // Create and simulate filled array
        var array = b2Array_Create<int>(6);
        array.count = 4;
        array.data[0] = 1;
        array.data[1] = 2;

        var originalData = array.data;

        // Clear it
        b2Array_Clear(ref array);

        // Validate
        Assert.That(array.count, Is.EqualTo(0));
        Assert.That(array.capacity, Is.EqualTo(6));
        Assert.That(array.data, Is.SameAs(originalData));
        Assert.That(array.data[0], Is.EqualTo(1)); // data not zeroed
        Assert.That(array.data[1], Is.EqualTo(2));
    }


    [Test]
    public void Test_b2Array_ByteCount()
    {
        // Test for ValueType (struct)
        {
            // Create array of value type (KeyValuePair<int, float>)
            var array = b2Array_Create<DummyStruct>(5);

            // Validate byte count
            int byteCount = b2Array_ByteCount(ref array);
            Assert.That(byteCount, Is.EqualTo(8 * 5));
        }

        // Test for ReferenceType (object)
        {
            // Create array of reference type (object)
            var array = b2Array_Create<object>(3);

            // For reference type, the byte count should return -1
            int byteCount = b2Array_ByteCount(ref array);

            // Validate that byte count returns -1 for reference types
            Assert.That(byteCount, Is.EqualTo(-1));
        }

        // Test for PrimitiveType (int)
        {
            // Create array of primitive type (int)
            var array = b2Array_Create<int>(10);

            // Size of int is 4 bytes
            int expectedByteCount = 10 * Marshal.SizeOf<int>();

            // Validate byte count
            int byteCount = b2Array_ByteCount(ref array);
            Assert.That(byteCount, Is.EqualTo(expectedByteCount));
        }
    }

    [Test]
    public void Test_b2Array_Push()
    {
        // Test for pushing a value when the array has sufficient capacity
        {
            // Create array of int with initial capacity of 5
            var array = b2Array_Create<int>(5);
            array.count = 3; // set count to 3 (some values are already added)

            // Push a new value into the array
            b2Array_Push(ref array, 10);

            // Validate that the new value was added
            Assert.That(array.data[array.count - 1], Is.EqualTo(10));
            Assert.That(array.count, Is.EqualTo(4)); // count should have increased by 1
        }

        // Test for pushing a value when the array needs to expand capacity
        {
            // Create array of int with initial capacity of 2 (small capacity)
            var array = b2Array_Create<int>(2);
            array.count = 2; // set count to 2 (array is full)

            // Push a new value, which should trigger a resize of the array
            b2Array_Push(ref array, 20);

            // Validate that the new value was added and the capacity increased
            Assert.That(array.data[array.count - 1], Is.EqualTo(20));
            Assert.That(array.count, Is.EqualTo(3)); // count should have increased by 1
            Assert.That(array.capacity, Is.GreaterThan(2)); // capacity should be larger than before
        }

        // Test for pushing a value into an empty array
        {
            // Create an empty array of int with initial capacity of 5
            var array = b2Array_Create<int>(5);
            array.count = 0; // no elements in the array yet

            // Push a new value into the array
            b2Array_Push(ref array, 30);

            // Validate that the new value was added
            Assert.That(array.data[array.count - 1], Is.EqualTo(30));
            Assert.That(array.count, Is.EqualTo(1)); // count should be 1 after pushing the value
        }
    }

    [Test]
    public void Test_b2Array_Add()
    {
        // Test when the array has sufficient capacity
        {
            // Create array of int with initial capacity of 5
            var array = b2Array_Create<int>(5);
            array.count = 3; // set count to 3 (some values are already added)

            // Add a new value to the array
            ref var addedValue = ref b2Array_Add(ref array);
            addedValue = 10;

            // Validate that the new value was added
            Assert.That(array.data[array.count - 1], Is.EqualTo(10));
            Assert.That(array.count, Is.EqualTo(4)); // count should have increased by 1
        }

        // Test when the array needs to expand capacity
        {
            // Create array of int with initial capacity of 2 (small capacity)
            var array = b2Array_Create<int>(2);
            array.count = 2; // set count to 2 (array is full)

            // Add a new value, which should trigger a resize of the array
            ref var addedValue = ref b2Array_Add(ref array);
            addedValue = 20;

            // Validate that the new value was added and the capacity increased
            Assert.That(array.data[array.count - 1], Is.EqualTo(20));
            Assert.That(array.count, Is.EqualTo(3)); // count should have increased by 1
            Assert.That(array.capacity, Is.GreaterThan(2)); // capacity should be larger than before
        }

        // Test when adding to an empty array
        {
            // Create an empty array of int with initial capacity of 5
            var array = b2Array_Create<int>(5);
            array.count = 0; // no elements in the array yet

            // Add a new value to the array
            ref var addedValue = ref b2Array_Add(ref array);
            addedValue = 30;

            // Validate that the new value was added
            Assert.That(array.data[array.count - 1], Is.EqualTo(30));
            Assert.That(array.count, Is.EqualTo(1)); // count should be 1 after adding the value
            Assert.That(array.capacity, Is.EqualTo(5));
        }
    }

    [Test]
    public void Test_b2Array_Get()
    {
        // Test for valid index within the array's range
        {
            // Create an array with initial capacity of 5 and set count to 3
            var array = b2Array_Create<int>(5);
            array.count = 3;

            // Set some values
            array.data[0] = 1;
            array.data[1] = 2;
            array.data[2] = 3;

            // Get value at index 1 (valid index)
            ref var value = ref b2Array_Get(ref array, 1);

            // Validate that the value is correct
            Assert.That(value, Is.EqualTo(2)); // The value at index 1 should be 2
        }

        // Test for invalid index (out of range) - Expect IndexOutOfRangeException
        {
            // Create an array with initial capacity of 3 and set count to 3
            var array = b2Array_Create<int>(3);
            array.count = 3;

            // Set some values
            array.data[0] = 10;
            array.data[1] = 20;
            array.data[2] = 30;

            // Invalid index (out of range)
            Assert.Throws<IndexOutOfRangeException>(() => b2Array_Get(ref array, 5));
        }
    }

    [Test]
    public void Test_b2Array_Resize()
    {
        // Test Resize when increasing the array size
        {
            // Create an array with initial capacity of 3
            var array = b2Array_Create<int>(3);
            array.count = 3;

            // Set some values
            array.data[0] = 1;
            array.data[1] = 2;
            array.data[2] = 3;

            // Resize the array to a larger size (5)
            b2Array_Resize(ref array, 5);

            // Validate that the new count is correct
            Assert.That(array.count, Is.EqualTo(5)); // After resizing, count should be 5

            // Validate that the previous data is still intact
            Assert.That(array.data[0], Is.EqualTo(1));
            Assert.That(array.data[1], Is.EqualTo(2));
            Assert.That(array.data[2], Is.EqualTo(3));

            // Validate that new slots are initialized
            Assert.That(array.data[3], Is.EqualTo(0)); // New slots should be default (0 for int)
            Assert.That(array.data[4], Is.EqualTo(0));
        }

        // Test Resize when decreasing the array size
        {
            // Create an array with initial capacity of 5
            var array = b2Array_Create<int>(5);
            array.count = 5;

            // Set some values
            array.data[0] = 1;
            array.data[1] = 2;
            array.data[2] = 3;
            array.data[3] = 4;
            array.data[4] = 5;

            // Resize the array to a smaller size (3)
            b2Array_Resize(ref array, 3);

            // Validate that the new count is correct
            Assert.That(array.count, Is.EqualTo(3)); // After resizing, count should be 3

            // Validate that the previous data is still intact
            Assert.That(array.data[0], Is.EqualTo(1));
            Assert.That(array.data[1], Is.EqualTo(2));
            Assert.That(array.data[2], Is.EqualTo(3));

            // Validate that data beyond the resized count is not accessible (should be out of bounds)
            Assert.Throws<IndexOutOfRangeException>(() => b2Array_Get(ref array, 3));
        }

        // Test Resize when setting the count to 0
        {
            // Create an array with initial capacity of 5
            var array = b2Array_Create<int>(5);
            array.count = 3;

            // Resize the array to 0
            b2Array_Resize(ref array, 0);

            // Validate that the count is set to 0
            Assert.That(array.count, Is.EqualTo(0)); // After resizing, count should be 0

            // Validate that the array data is not accessible
            Assert.Throws<IndexOutOfRangeException>(() => b2Array_Get(ref array, 0));
        }
    }

    [Test]
    public void Test_b2Array_Set()
    {
        // Test setting a valid index
        var array = b2Array_Create<DummyObject<int>>(5);
        array.count = 3;
        array.data[0] = new DummyObject<int>(1);
        array.data[1] = new DummyObject<int>(2);
        array.data[2] = new DummyObject<int>(3);

        var expectedValue = new DummyObject<int>(99);
        
        b2Array_Set(ref array, 1, expectedValue);
        Assert.That(array.data[1], Is.EqualTo(expectedValue)); // Verify that value at index 1 is updated to 99

        // Test invalid index (negative index)
        Assert.Throws<IndexOutOfRangeException>(() => b2Array_Set(ref array, -1, expectedValue));

        // Test invalid index (out of bounds)
        Assert.Throws<IndexOutOfRangeException>(() => b2Array_Set(ref array, 5, expectedValue));
    }
    
    [Test]
    public void Test_b2Array_RemoveSwap()
    {
        // Test valid removal and swap
        var array = b2Array_Create<int>(5);
        array.count = 5;
        array.data[0] = 1;
        array.data[1] = 2;
        array.data[2] = 3;
        array.data[3] = 4;
        array.data[4] = 5;

        int movedIndex = b2Array_RemoveSwap(ref array, 2);

        Assert.That(movedIndex, Is.EqualTo(4)); // The moved index should be the last element's index (4)
        Assert.That(array.data[2], Is.EqualTo(5)); // The value at index 2 should now be 5 (swapped with last element)
        Assert.That(array.count, Is.EqualTo(4)); // The count should decrease by 1

        // Test removing from the last index
        movedIndex = b2Array_RemoveSwap(ref array, 3);
        Assert.That(movedIndex, Is.EqualTo(B2_NULL_INDEX)); // No swap should occur when removing the last element
        Assert.That(array.count, Is.EqualTo(3)); // The count should decrease by 1

        // Test invalid index (negative index)
        Assert.Throws<IndexOutOfRangeException>(() => b2Array_RemoveSwap(ref array, -1));

        // Test invalid index (out of bounds)
        Assert.Throws<IndexOutOfRangeException>(() => b2Array_RemoveSwap(ref array, 5));
    }
    
    [Test]
    public void Test_b2Array_Pop()
    {
        // Test popping an element from a non-empty array
        var array = b2Array_Create<DummyObject<int>>(5);
        array.count = 3;
        array.data[0] = new DummyObject<int>(1);
        array.data[1] = new DummyObject<int>(2);
        array.data[2] = new DummyObject<int>(3);

        DummyObject<int> poppedValue = b2Array_Pop(ref array);

        Assert.That(poppedValue.Value, Is.EqualTo(3)); // The popped value should be 3 (last element)
        Assert.That(array.count, Is.EqualTo(2)); // The count should decrease by 1
        Assert.That(array.data[2].Value, Is.EqualTo(0)); // The last element should be reset to default (0 for int)

        // Test popping from an empty array (should throw an exception)
        var emptyArray = b2Array_Create<int>(5);
        emptyArray.count = 0;

        Assert.Throws<IndexOutOfRangeException>(() => b2Array_Pop(ref emptyArray));
    }
}
