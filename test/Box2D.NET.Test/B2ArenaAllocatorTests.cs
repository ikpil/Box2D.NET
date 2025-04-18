using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using static Box2D.NET.B2ArenaAllocators;

namespace Box2D.NET.Test;

public class B2ArenaAllocatorTests
{
    [Test]
    public void Test_GetOrCreateFor_CreatesAllocatorForType()
    {
        // Arrange
        var arena = b2CreateArenaAllocator(10);

        // Act
        var typedAllocator = arena.GetOrCreateFor<int>();

        // Assert
        Assert.That(typedAllocator, Is.Not.Null, "Allocator should be created for the specified type.");
        Assert.That(typedAllocator, Is.AssignableFrom<B2ArenaAllocatorTyped<int>>(), "Returned allocator should be of the correct type.");
    }

    [Test]
    public void Test_GetOrCreateFor_ReturnsSameAllocatorForSameType()
    {
        // Arrange
        var arena = b2CreateArenaAllocator(10);

        // Act
        var typedAllocator1 = arena.GetOrCreateFor<int>();
        var typedAllocator2 = arena.GetOrCreateFor<int>();

        // Assert
        Assert.That(typedAllocator1, Is.SameAs(typedAllocator2), "Allocator should be reused for the same type.");
    }

    [Test]
    public void Test_GetOrCreateFor_AllocatesForDifferentTypes()
    {
        // Arrange
        var arena = b2CreateArenaAllocator(10);

        // Act
        IB2ArenaAllocatable typedAllocator1 = arena.GetOrCreateFor<int>();
        Assert.That(arena.Count, Is.EqualTo(1));

        IB2ArenaAllocatable typedAllocator2 = arena.GetOrCreateFor<uint>();
        Assert.That(arena.Count, Is.EqualTo(2));

        // Assert
        Assert.That(typedAllocator1, Is.Not.SameAs(typedAllocator2), "Different types should result in different allocators.");
    }

    [Test]
    public void Test_AsSpan_ReturnsAllocatorsSpan()
    {
        // Arrange
        var arena = b2CreateArenaAllocator(10);
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
    public void Test_GetOrCreateFor_ShouldBeThreadSafe_WhenCalledConcurrently()
    {
        // Arrange
        var arena = b2CreateArenaAllocator(10);
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


    [Test]
    public void Test_AllocateArenaItem_WithinCapacity_UsesArenaMemory()
    {
        var arena = b2CreateArenaAllocator(100);
        var alloc = arena.GetOrCreateFor<int>();

        // 0, 1, 2
        for (int i = 0; i < 100 / 32; ++i)
        {
            var result = b2AllocateArenaItem<int>(arena, 1, "test");
            Assert.That(result.Count, Is.EqualTo(32), $"index({i})");
            Assert.That(result.Offset, Is.EqualTo(i * 32), $"index({i})");

            Assert.That(alloc.index, Is.EqualTo((i + 1) * 32), $"index({i})");
            Assert.That(alloc.allocation, Is.EqualTo((i + 1) * 32), $"index({i})");
            Assert.That(alloc.entries.count, Is.EqualTo(i + 1), $"index({i})");
            Assert.That(alloc.entries.data[i].usedMalloc, Is.False, $"index({i})");
        }
    }

    [Test]
    public void Test_AllocateArenaItem_ExceedsCapacity_UsesHeapFallback()
    {
        // Create an arena with very small capacity to force fallback
        var arena = b2CreateArenaAllocator(32);
        var alloc = arena.GetOrCreateFor<int>();

        for (int i = 0; i < 3; ++i)
        {
            // Request size large enough to exceed arena capacity every time
            int requestSize = 33;
            var result = b2AllocateArenaItem<int>(arena, requestSize, $"heap_test_{i}");

            int expectedSize32 = ((requestSize - 1) | 0x1F) + 1;

            Assert.That(result.Count, Is.EqualTo(64), $"iteration({i})");
            Assert.That(alloc.index, Is.EqualTo(0), $"iteration({i}) - Arena index should not change");
            Assert.That(alloc.allocation, Is.EqualTo((i + 1) * expectedSize32), $"iteration({i}) - Allocation tracking");
            Assert.That(alloc.entries.count, Is.EqualTo(i + 1), $"iteration({i}) - Entry count");

            var entry = alloc.entries.data[i];
            Assert.That(entry.usedMalloc, Is.True, $"iteration({i}) - Should fallback to malloc");
            Assert.That(entry.name, Is.EqualTo($"heap_test_{i}"), $"iteration({i}) - Entry name");

            // Ensure the memory is not from the arena's buffer
            Assert.That(result.Array, Is.Not.SameAs(alloc.data.Array), $"iteration({i}) - Should not use arena memory");
        }
    }

    [Test]
    public void Test_FreeArenaItem_ArenaAndHeap()
    {
        var arena = b2CreateArenaAllocator(64);
        var alloc = arena.GetOrCreateFor<int>();

        // Arena allocation (fits into arena buffer)
        var arenaMem = b2AllocateArenaItem<int>(arena, 1, "arena_alloc"); // size32 = 32

        // Heap fallback allocation (exceeds arena capacity)
        var heapMem = b2AllocateArenaItem<int>(arena, 65, "heap_alloc"); // size32 = 96 → forces fallback

        Assert.That(alloc.entries.count, Is.EqualTo(2));

        // --- Free heap allocation ---
        b2FreeArenaItem(arena, heapMem);

        Assert.That(alloc.entries.count, Is.EqualTo(1));
        Assert.That(alloc.index, Is.EqualTo(32), "Heap free should not affect arena index");
        Assert.That(alloc.allocation, Is.EqualTo(32));

        // --- Free arena allocation ---
        b2FreeArenaItem(arena, arenaMem);

        Assert.That(alloc.entries.count, Is.EqualTo(0));
        Assert.That(alloc.index, Is.EqualTo(0));
        Assert.That(alloc.allocation, Is.EqualTo(0));
    }

    [Test]
    public void Test_GrowArena()
    {
        var arena = b2CreateArenaAllocator(10);
        var intSegment = b2AllocateArenaItem<int>(arena, 32, "int * 32");
        var byteSegment = b2AllocateArenaItem<byte>(arena, 32, "byte * 32");

        // before
        var allocSpan = arena.AsSpan();
        for (int i = 0; i < allocSpan.Length; ++i)
        {
            Assert.That(allocSpan[i].index, Is.EqualTo(0));
            Assert.That(allocSpan[i].capacity, Is.EqualTo(10));
            Assert.That(allocSpan[i].allocation, Is.EqualTo(32));
            Assert.That(allocSpan[i].maxAllocation, Is.EqualTo(32));
        }
        
        b2FreeArenaItem(arena, intSegment);
        b2FreeArenaItem(arena, byteSegment);
        b2GrowArena(arena);
        
        for (int i = 0; i < allocSpan.Length; ++i)
        {
            Assert.That(allocSpan[i].index, Is.EqualTo(0));
            Assert.That(allocSpan[i].capacity, Is.EqualTo(32 + 32 / 2));
            Assert.That(allocSpan[i].allocation, Is.EqualTo(0));
            Assert.That(allocSpan[i].maxAllocation, Is.EqualTo(32));
        }

    }
}