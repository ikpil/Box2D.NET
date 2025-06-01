// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using static Box2D.NET.B2ArenaAllocators;

namespace Box2D.NET.Test;

public class B2ArenaAllocatorTypedTests
{
    [Test]
    public void Constructor_InitializesCorrectly()
    {
        B2ArenaAllocatorTyped<int> alloc = b2CreateArenaAllocator<int>(10);
        Assert.That(alloc.capacity, Is.EqualTo(10));
        Assert.That(alloc.data.Array, Is.Not.Null);
        Assert.That(alloc.data.Count, Is.EqualTo(10));
        Assert.That(alloc.index, Is.EqualTo(0));
        Assert.That(alloc.allocation, Is.EqualTo(0));
        Assert.That(alloc.maxAllocation, Is.EqualTo(0));
        Assert.That(alloc.entries.data, Is.Not.Null);
        Assert.That(alloc.entries.count, Is.EqualTo(0));
        Assert.That(alloc.entries.capacity, Is.EqualTo(10));
    }

    [Test]
    public void Grow_IncreasesCapacityWhenMaxAllocationExceedsCurrent()
    {
        B2ArenaAllocatorTyped<int> alloc = b2CreateArenaAllocator<int>(10);

        alloc.maxAllocation = 15;
        int oldCapacity = alloc.capacity;
        int newCapacity = alloc.Grow();

        Assert.That(newCapacity, Is.GreaterThan(oldCapacity));
        Assert.That(newCapacity, Is.EqualTo(15 + 15 / 2)); // Check the growth factor
        Assert.That(alloc.data.Count, Is.EqualTo(newCapacity));
        Assert.That(alloc.allocation, Is.EqualTo(0)); // Allocation should be 0 before grow
    }

    [Test]
    public void Grow_DoesNotIncreaseCapacityWhenMaxAllocationIsWithinCurrent()
    {
        B2ArenaAllocatorTyped<int> alloc = b2CreateArenaAllocator<int>(10);

        alloc.maxAllocation = 5;
        int oldCapacity = alloc.capacity;
        int newCapacity = alloc.Grow();

        Assert.That(newCapacity, Is.EqualTo(oldCapacity));
        Assert.That(alloc.data.Count, Is.EqualTo(newCapacity));
        Assert.That(alloc.allocation, Is.EqualTo(0)); // Allocation should be 0 before grow
    }

    [Test]
    public void Destroy_ReleasesResourcesAndResetsProperties()
    {
        B2ArenaAllocatorTyped<int> alloc = b2CreateArenaAllocator<int>(10);

        alloc.Destroy();

        Assert.That(alloc.data.Array, Is.Null);
        Assert.That(alloc.capacity, Is.EqualTo(0));
        Assert.That(alloc.index, Is.EqualTo(0));
        Assert.That(alloc.allocation, Is.EqualTo(0));
        Assert.That(alloc.maxAllocation, Is.EqualTo(0));
        Assert.That(alloc.entries.data, Is.Null);
        Assert.That(alloc.entries.count, Is.EqualTo(0));
        Assert.That(alloc.entries.capacity, Is.EqualTo(0));
    }
}
