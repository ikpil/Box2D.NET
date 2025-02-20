namespace Box2D.NET.Primitives
{
    public interface IB2ArenaAllocatable
    {
        int capacity { get; }
        int index { get; }
        int allocation { get; }
        int maxAllocation { get; }
    }
}