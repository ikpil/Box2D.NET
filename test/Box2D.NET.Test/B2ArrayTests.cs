// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Constants;

namespace Box2D.NET.Test;

public class B2ArrayTests
{
    private struct Foo
    {
        public int a;
        public float b;
    }

    private struct Bar
    {
        public B2Array<int> a;
    }

    private struct Owner
    {
        public int index;
    }

    private struct Entity
    {
        public int bodyIndex;
        public int id;
    }

    private struct Body
    {
        public int entityIndex;
        public float mass;
    }

    [Test]
    public void TestCreateDestroy()
    {
        B2Array<int> a = b2Array_Create<int>();
        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestAccess()
    {
        B2Array<int> a = b2Array_Create<int>();
        b2Array_Push(ref a, 42);
        ref int element = ref b2Array_Get(ref a, 0);
        Assert.That(element, Is.EqualTo(42));
        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestIteration()
    {
        B2Array<int> a = new B2Array<int>();
        b2Array_Push(ref a, 1);
        b2Array_Push(ref a, 2);
        b2Array_Push(ref a, 3);

        int sum = 0;
        for (int i = 0; i < a.count; ++i)
        {
            sum += a.data[i];
        }

        Assert.That(sum, Is.EqualTo(6));
        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestArrayOfStruct()
    {
        B2Array<Foo> a = b2Array_Create<Foo>();
        b2Array_Push(ref a, new Foo { a = 1, b = 5.0f });
        b2Array_Push(ref a, new Foo { a = 2, b = 6.0f });
        b2Array_Push(ref a, new Foo { a = 3, b = 7.0f });

        int sum1 = 0;
        float sum2 = 0.0f;
        for (int i = 0; i < a.count; ++i)
        {
            sum1 += a.data[i].a;
            sum2 += a.data[i].b;
        }

        Assert.That(sum1, Is.EqualTo(6));
        Assert.That(sum2, Is.EqualTo(18.0f));

        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestStructWithArray()
    {
        Bar a = new Bar();
        a.a = b2Array_Create<int>();
        b2Array_Push(ref a.a, 1);
        b2Array_Push(ref a.a, 2);
        b2Array_Push(ref a.a, 3);

        int sum1 = 0;
        for (int i = 0; i < a.a.count; ++i)
        {
            sum1 += a.a.data[i];
        }

        Assert.That(sum1, Is.EqualTo(6));

        b2Array_Destroy(ref a.a);
    }

    [Test]
    public void TestArrayEmplace()
    {
        B2Array<ulong> a = new B2Array<ulong>();

        for (int i = 0; i < 100; ++i)
        {
            ref ulong j = ref b2Array_Emplace(ref a);
            j = (ulong)i;
        }

        ulong sum = 0;
        for (int i = 0; i < a.count; ++i)
        {
            sum += a.data[i];
        }

        Assert.That(sum, Is.EqualTo(100ul * 99ul / 2ul));

        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestArrayRemove()
    {
        B2Array<short> a = new B2Array<short>();

        int n = 10;
        b2Array_Reserve(ref a, n);
        Assert.That(a.capacity == n && a.count == 0, Is.True);

        for (short i = 0; i < n; ++i)
        {
            b2Array_Push(ref a, i);
        }

        Assert.That(a.count, Is.EqualTo(n));

        int sum = 0;
        for (int i = 0; i < n; ++i)
        {
            int temp = b2Array_RemoveSwap(ref a, 0);
            sum += temp;
        }

        Assert.That(sum, Is.EqualTo(n * (n - 1) / 2 - 1));

        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestArrayPop()
    {
        B2Array<byte> a = new B2Array<byte>();

        int n = 100;
        b2Array_Resize(ref a, n);
        Assert.That(a.capacity == n && a.count == n, Is.True);

        for (byte i = 0; i < n; ++i)
        {
            b2Array_Push(ref a, i);
        }

        int sum = 0;
        for (int i = 0; i < n; ++i)
        {
            sum += b2Array_Pop(ref a);
        }

        Assert.That(sum, Is.EqualTo(100 * 99 / 2));

        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestEmptyArrayProperties()
    {
        B2Array<int> a = b2Array_Create<int>();
        Assert.That(a.count, Is.EqualTo(0));
        b2Array_Destroy(ref a);
        Assert.That(a.count, Is.EqualTo(0));
        Assert.That(a.capacity, Is.EqualTo(0));
        Assert.That(a.data, Is.Null);
    }

    [Test]
    public void TestArrayReserveNoop()
    {
        B2Array<int> a = new B2Array<int>();
        b2Array_Reserve(ref a, 16);
        Assert.That(a.capacity, Is.GreaterThanOrEqualTo(16));
        int oldCapacity = a.capacity;
        // Reserve with smaller or equal capacity should be a no-op
        b2Array_Reserve(ref a, 8);
        Assert.That(a.capacity, Is.EqualTo(oldCapacity));
        b2Array_Reserve(ref a, oldCapacity);
        Assert.That(a.capacity, Is.EqualTo(oldCapacity));
        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestArrayResizeDown()
    {
        B2Array<int> a = new B2Array<int>();
        for (int i = 0; i < 10; ++i)
        {
            b2Array_Push(ref a, i * 10);
        }

        Assert.That(a.count, Is.EqualTo(10));

        b2Array_Resize(ref a, 5);
        Assert.That(a.count, Is.EqualTo(5));

        // First 5 elements should be unchanged
        for (int i = 0; i < 5; ++i)
        {
            Assert.That(b2Array_Get(ref a, i), Is.EqualTo(i * 10));
        }

        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestArrayResizeUp()
    {
        B2Array<int> a = new B2Array<int>();

        b2Array_Resize(ref a, 10);
        Assert.That(a.count, Is.EqualTo(10));
        Assert.That(a.capacity, Is.GreaterThanOrEqualTo(10));

        // Write values into resized slots
        for (int i = 0; i < 10; ++i)
        {
            a.data[i] = i + 1;
        }

        // Resize larger, original values preserved
        b2Array_Resize(ref a, 20);
        Assert.That(a.count, Is.EqualTo(20));
        for (int i = 0; i < 10; ++i)
        {
            Assert.That(a.data[i], Is.EqualTo(i + 1));
        }

        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestArrayPopOrder()
    {
        B2Array<int> a = new B2Array<int>();
        b2Array_Push(ref a, 10);
        b2Array_Push(ref a, 20);
        b2Array_Push(ref a, 30);

        // Pop returns the last element (LIFO)
        Assert.That(b2Array_Pop(ref a), Is.EqualTo(30));
        Assert.That(a.count, Is.EqualTo(2));
        Assert.That(b2Array_Pop(ref a), Is.EqualTo(20));
        Assert.That(a.count, Is.EqualTo(1));
        Assert.That(b2Array_Pop(ref a), Is.EqualTo(10));
        Assert.That(a.count, Is.EqualTo(0));

        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestArrayRemoveSwapContents()
    {
        B2Array<int> a = new B2Array<int>();
        b2Array_Push(ref a, 100);
        b2Array_Push(ref a, 200);
        b2Array_Push(ref a, 300);
        b2Array_Push(ref a, 400);

        // Remove middle element: last element swaps into its place
        b2Array_RemoveSwap(ref a, 1);
        Assert.That(a.count, Is.EqualTo(3));
        Assert.That(b2Array_Get(ref a, 0), Is.EqualTo(100));
        Assert.That(b2Array_Get(ref a, 1), Is.EqualTo(400)); // swapped from end
        Assert.That(b2Array_Get(ref a, 2), Is.EqualTo(300));

        // Remove last element: no swap needed
        b2Array_RemoveSwap(ref a, 2);
        Assert.That(a.count, Is.EqualTo(2));
        Assert.That(b2Array_Get(ref a, 0), Is.EqualTo(100));
        Assert.That(b2Array_Get(ref a, 1), Is.EqualTo(400));

        // Remove first element
        b2Array_RemoveSwap(ref a, 0);
        Assert.That(a.count, Is.EqualTo(1));
        Assert.That(b2Array_Get(ref a, 0), Is.EqualTo(400));

        // Remove sole element
        b2Array_RemoveSwap(ref a, 0);
        Assert.That(a.count, Is.EqualTo(0));

        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestArrayRemoveSwapAllFromEnd()
    {
        B2Array<int> a = new B2Array<int>();
        b2Array_Push(ref a, 1);
        b2Array_Push(ref a, 2);
        b2Array_Push(ref a, 3);

        // Always remove the last element (no swap path)
        b2Array_RemoveSwap(ref a, a.count - 1);
        Assert.That(a.count, Is.EqualTo(2));
        Assert.That(b2Array_Get(ref a, 0), Is.EqualTo(1));
        Assert.That(b2Array_Get(ref a, 1), Is.EqualTo(2));

        b2Array_RemoveSwap(ref a, a.count - 1);
        Assert.That(a.count, Is.EqualTo(1));
        Assert.That(b2Array_Get(ref a, 0), Is.EqualTo(1));

        b2Array_RemoveSwap(ref a, a.count - 1);
        Assert.That(a.count, Is.EqualTo(0));

        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestArrayGrowthIntegrity()
    {
        B2Array<int> a = new B2Array<int>();

        // Push many elements to trigger multiple reallocations
        for (int i = 0; i < 1000; ++i)
        {
            b2Array_Push(ref a, i);
        }

        Assert.That(a.count, Is.EqualTo(1000));
        Assert.That(a.capacity, Is.GreaterThanOrEqualTo(1000));

        // Verify every element survived the reallocations
        for (int i = 0; i < 1000; ++i)
        {
            Assert.That(b2Array_Get(ref a, i), Is.EqualTo(i));
        }

        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestArrayInterleavedPushPop()
    {
        B2Array<int> a = new B2Array<int>();

        b2Array_Push(ref a, 1);
        b2Array_Push(ref a, 2);
        Assert.That(b2Array_Pop(ref a), Is.EqualTo(2));
        Assert.That(a.count, Is.EqualTo(1));

        b2Array_Push(ref a, 3);
        b2Array_Push(ref a, 4);
        Assert.That(a.count, Is.EqualTo(3));
        Assert.That(b2Array_Pop(ref a), Is.EqualTo(4));
        Assert.That(b2Array_Pop(ref a), Is.EqualTo(3));
        Assert.That(b2Array_Pop(ref a), Is.EqualTo(1));
        Assert.That(a.count, Is.EqualTo(0));

        // Re-use after emptying
        b2Array_Push(ref a, 99);
        Assert.That(a.count, Is.EqualTo(1));
        Assert.That(b2Array_Get(ref a, 0), Is.EqualTo(99));

        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestArrayEmplaceStruct()
    {
        B2Array<Foo> a = new B2Array<Foo>();

        for (int i = 0; i < 50; ++i)
        {
            ref Foo f = ref b2Array_Emplace(ref a);
            f.a = i;
            f.b = (float)i * 2.0f;
        }

        Assert.That(a.count, Is.EqualTo(50));

        for (int i = 0; i < 50; ++i)
        {
            ref Foo f = ref b2Array_Get(ref a, i);
            Assert.That(f.a, Is.EqualTo(i));
            Assert.That(f.b, Is.EqualTo((float)i * 2.0f));
        }

        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestArrayPushAfterReserve()
    {
        B2Array<int> a = new B2Array<int>();

        // Reserve doesn't change count
        b2Array_Reserve(ref a, 50);
        Assert.That(a.count, Is.EqualTo(0));
        Assert.That(a.capacity, Is.GreaterThanOrEqualTo(50));

        // Push within reserved capacity (no reallocation expected)
        for (int i = 0; i < 50; ++i)
        {
            b2Array_Push(ref a, i * 3);
        }

        Assert.That(a.count, Is.EqualTo(50));
        for (int i = 0; i < 50; ++i)
        {
            Assert.That(b2Array_Get(ref a, i), Is.EqualTo(i * 3));
        }

        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestArraySingleElement()
    {
        B2Array<Foo> a = new B2Array<Foo>();

        ref Foo f = ref b2Array_Emplace(ref a);
        f.a = 7;
        f.b = 3.14f;

        Assert.That(a.count, Is.EqualTo(1));
        Assert.That(b2Array_Get(ref a, 0).a, Is.EqualTo(7));
        Assert.That(b2Array_Get(ref a, 0).b, Is.EqualTo(3.14f));

        Foo popped = b2Array_Pop(ref a);
        Assert.That(popped.a, Is.EqualTo(7));
        Assert.That(popped.b, Is.EqualTo(3.14f));
        Assert.That(a.count, Is.EqualTo(0));

        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestArrayCreateN()
    {
        B2Array<int> a = b2Array_Create<int>(16);
        Assert.That(a.count, Is.EqualTo(0));
        Assert.That(a.capacity, Is.EqualTo(16));
        Assert.That(a.data, Is.Not.Null);

        // Verify it behaves like a normal array after creation
        for (int i = 0; i < 16; ++i)
        {
            b2Array_Push(ref a, i * 5);
        }

        Assert.That(a.count, Is.EqualTo(16));
        for (int i = 0; i < 16; ++i)
        {
            Assert.That(b2Array_Get(ref a, i), Is.EqualTo(i * 5));
        }

        b2Array_Destroy(ref a);
    }

    [Test]
    public void TestDeclaredArrayTypes()
    {
        B2Array<Owner> owners = b2Array_Create<Owner>();
        B2Array<Entity> entities = b2Array_Create<Entity>();
        B2Array<Body> bodies = b2Array_Create<Body>();

        b2Array_Push(ref owners, new Owner { index = 1 });
        b2Array_Push(ref entities, new Entity { bodyIndex = 2, id = 3 });
        b2Array_Push(ref bodies, new Body { entityIndex = 4, mass = 5.0f });

        Assert.That(b2Array_Get(ref owners, 0).index, Is.EqualTo(1));
        Assert.That(b2Array_Get(ref entities, 0).bodyIndex, Is.EqualTo(2));
        Assert.That(b2Array_Get(ref entities, 0).id, Is.EqualTo(3));
        Assert.That(b2Array_Get(ref bodies, 0).entityIndex, Is.EqualTo(4));
        Assert.That(b2Array_Get(ref bodies, 0).mass, Is.EqualTo(5.0f));

        b2Array_Destroy(ref owners);
        b2Array_Destroy(ref entities);
        b2Array_Destroy(ref bodies);
    }
}
