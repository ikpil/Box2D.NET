﻿using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Box2D.NET.Test;

public class B2ArenaAllocatorTests
{
    [Test]
    public void GetOrCreateFor_CreatesAllocatorForType()
    {
        // Arrange
        var arena = new B2ArenaAllocator(10);

        // Act
        var typedAllocator = arena.GetOrCreateFor<int>();

        // Assert
        Assert.That(typedAllocator, Is.Not.Null, "Allocator should be created for the specified type.");
        Assert.That(typedAllocator, Is.AssignableFrom<B2ArenaAllocatorTyped<int>>(), "Returned allocator should be of the correct type.");
    }

    [Test]
    public void GetOrCreateFor_ReturnsSameAllocatorForSameType()
    {
        // Arrange
        var arena = new B2ArenaAllocator(10);

        // Act
        var typedAllocator1 = arena.GetOrCreateFor<int>();
        var typedAllocator2 = arena.GetOrCreateFor<int>();

        // Assert
        Assert.That(typedAllocator1, Is.SameAs(typedAllocator2), "Allocator should be reused for the same type.");
    }

    [Test]
    public void GetOrCreateFor_AllocatesForDifferentTypes()
    {
        // Arrange
        var arena = new B2ArenaAllocator(10);

        // Act
        IB2ArenaAllocatable typedAllocator1 = arena.GetOrCreateFor<int>();
        Assert.That(arena.Count, Is.EqualTo(1));

        IB2ArenaAllocatable typedAllocator2 = arena.GetOrCreateFor<uint>();
        Assert.That(arena.Count, Is.EqualTo(2));

        // Assert
        Assert.That(typedAllocator1, Is.Not.SameAs(typedAllocator2), "Different types should result in different allocators.");
    }

    [Test]
    public void AsSpan_ReturnsAllocatorsSpan()
    {
        // Arrange
        var arena = new B2ArenaAllocator(10);
        IB2ArenaAllocatable first = arena.GetOrCreateFor<int>();
        IB2ArenaAllocatable second = arena.GetOrCreateFor<float>();
        IB2ArenaAllocatable third = arena.GetOrCreateFor<double>();

        // Act
        var span = arena.AsSpan();

        // Assert
        Assert.That(span.Length, Is.EqualTo(3), "Span should contain at least one allocator.");
        Assert.That(span[0], Is.EqualTo(first));
        Assert.That(span[1], Is.EqualTo(second));
        Assert.That(span[2], Is.EqualTo(third));
    }

    [Test]
    public void GetOrCreateFor_ShouldBeThreadSafe_WhenCalledConcurrently()
    {
        // Arrange
        var arena = new B2ArenaAllocator(10);
        var tasks = new Task[100];
        var typedAllocators = new IB2ArenaAllocatable[100];
        var ce = new CountdownEvent(1);

        // Act: Run 100 tasks concurrently
        for (int i = 0; i < 100; i++)
        {
            int idx = i;
            tasks[i] = Task.Run(() =>
            {
                ce.Wait(1000);
                // Multiple threads trying to get or create an allocator for MyType
                var allocator1 = arena.GetOrCreateFor<byte>();
                var allocator2 = arena.GetOrCreateFor<char>();
                var allocator3 = arena.GetOrCreateFor<short>();
                var allocator4 = arena.GetOrCreateFor<int>();
                var allocator5 = arena.GetOrCreateFor<long>();
                var allocator6 = arena.GetOrCreateFor<ushort>();
                var allocator7 = arena.GetOrCreateFor<uint>();
                var allocator8 = arena.GetOrCreateFor<ulong>();
                var allocator9 = arena.GetOrCreateFor<float>();
                var allocator10 = arena.GetOrCreateFor<double>();
                typedAllocators[idx] = allocator1;
            });
        }

        ce.Signal();


        // Wait for all tasks to complete
        Task.WhenAll(tasks).Wait();

        // Assert: Ensure all threads got the same allocator instance for MyType
        for (int i = 1; i < typedAllocators.Length; i++)
        {
            Assert.That(typedAllocators[i], Is.SameAs(typedAllocators[0]), "Allocator instance should be shared among threads.");
        }

        Assert.That(arena.Count, Is.EqualTo(10));
    }
}