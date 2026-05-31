// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using Box2D.NET.Test.Primitives;
using static Box2D.NET.B2Arrays;

namespace Box2D.NET.Test;

public class B2StackTests
{
    [Test]
    public void Test_b2StackArray_CreatePushReserveDestroy()
    {
        var stack = B2StackArrays.b2StackArray_Create<int>(3);
        var stackData = stack.stackData;

        B2StackArrays.b2StackArray_Push(ref stack, 1);
        B2StackArrays.b2StackArray_Push(ref stack, 2);
        B2StackArrays.b2StackArray_Push(ref stack, 3);

        Assert.That(stack.data, Is.SameAs(stackData));
        Assert.That(stack.count, Is.EqualTo(3));
        Assert.That(stack.capacity, Is.EqualTo(3));

        B2StackArrays.b2StackArray_Push(ref stack, 4);

        Assert.That(stack.data, Is.Not.SameAs(stackData));
        Assert.That(stack.count, Is.EqualTo(4));
        Assert.That(stack.capacity, Is.EqualTo(6));
        Assert.That(stack.data[0], Is.EqualTo(1));
        Assert.That(stack.data[3], Is.EqualTo(4));

        ref int value = ref B2StackArrays.b2StackArray_Get(ref stack, 2);
        Assert.That(value, Is.EqualTo(3));

        B2StackArrays.b2StackArray_Destroy(ref stack);

        Assert.That(stack.stackData, Is.Null);
        Assert.That(stack.data, Is.Null);
        Assert.That(stack.count, Is.EqualTo(0));
        Assert.That(stack.capacity, Is.EqualTo(0));
    }

    [Test]
    public void Test_b2StackArray_RemoveUpdate()
    {
        var a = B2StackArrays.b2StackArray_Create<int>(8);
        var owners = b2Array_Create<DummyClass>();

        int n = 21;
        for (int i = 0; i < n; ++i)
        {
            ref DummyClass dummyClass = ref b2Array_Add(ref owners);
            dummyClass.index = i;
            B2StackArrays.b2StackArray_Push(ref a, i);
        }

        B2StackArrays.b2RemoveUpdate(ref a, ref owners, 0, x => x.index, (x, idx) => x.index = idx);
        b2Array_Get(ref owners, 0).index = -1;
        B2StackArrays.b2RemoveUpdate(ref a, ref owners, 3, x => x.index, (x, idx) => x.index = idx);
        b2Array_Get(ref owners, 3).index = -1;
        B2StackArrays.b2RemoveUpdate(ref a, ref owners, 8, x => x.index, (x, idx) => x.index = idx);
        b2Array_Get(ref owners, 8).index = -1;
        B2StackArrays.b2RemoveUpdate(ref a, ref owners, 5, x => x.index, (x, idx) => x.index = idx);
        b2Array_Get(ref owners, 5).index = -1;
        B2StackArrays.b2RemoveUpdate(ref a, ref owners, owners.count - 1, x => x.index, (x, idx) => x.index = idx);
        b2Array_Get(ref owners, owners.count - 1).index = -1;

        int count = 0;
        for (int i = 0; i < owners.count; ++i)
        {
            DummyClass dummyClass = b2Array_Get(ref owners, i);
            if (dummyClass.index == -1)
            {
                continue;
            }

            int indexA = B2StackArrays.b2StackArray_Get(ref a, dummyClass.index);
            Assert.That(indexA, Is.EqualTo(i));
            count += 1;
        }

        Assert.That(count, Is.EqualTo(a.count));

        B2StackArrays.b2StackArray_Destroy(ref a);
        b2Array_Destroy(ref owners);
    }

    [Test]
    public void Test_b2StackArray_RemoveUpdateAll()
    {
        var a = B2StackArrays.b2StackArray_Create<int>(8);
        var owners = b2Array_Create<DummyClass>();

        int n = 5;
        for (int i = 0; i < n; ++i)
        {
            ref DummyClass dummyClass = ref b2Array_Add(ref owners);
            dummyClass.index = i;
            B2StackArrays.b2StackArray_Push(ref a, i);
        }

        // Remove all elements one by one
        for (int i = 0; i < n; ++i)
        {
            // Find a valid owner to remove
            int removeIdx = -1;
            for (int j = 0; j < owners.count; ++j)
            {
                if (b2Array_Get(ref owners, j).index != -1)
                {
                    removeIdx = j;
                    break;
                }
            }

            if (removeIdx == -1)
            {
                break;
            }

            B2StackArrays.b2RemoveUpdate(ref a, ref owners, removeIdx, x => x.index, (x, idx) => x.index = idx);
            b2Array_Get(ref owners, removeIdx).index = -1;
        }

        Assert.That(a.count, Is.EqualTo(0));

        B2StackArrays.b2StackArray_Destroy(ref a);
        b2Array_Destroy(ref owners);
    }
}