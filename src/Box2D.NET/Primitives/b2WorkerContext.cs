namespace Box2D.NET.Primitives
{
    public class b2WorkerContext
    {
        public b2StepContext context;
        public int workerIndex;
        public object userTask;

        public void Clear()
        {
            context = null;
            workerIndex = -1;
            userTask = null;
        }
    }
}