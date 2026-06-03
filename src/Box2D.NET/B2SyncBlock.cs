namespace Box2D.NET
{
    // A unit of multithreaded work along with atomic synchronization. The syncIndex grows
    // monotonically allowing the solver block to be re-used across sub-steps.
    public struct B2SyncBlock
    {
        public B2SolverBlock block;
        public B2AtomicInt syncIndex;
    }
}