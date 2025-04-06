namespace Box2D.NET
{
    public struct B2PlaneSolverResult
    {
        public B2Vec2 position;
        public int iterationCount;

        public B2PlaneSolverResult(B2Vec2 position, int iterationCount)
        {
            this.position = position;
            this.iterationCount = iterationCount;
        }
    }
}