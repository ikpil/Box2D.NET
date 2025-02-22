namespace Box2D.NET.Samples.Primitives;

public class SampleTask
{
    public b2TaskCallback m_task;
    public object m_taskContext;

    public int m_SetSize;
    public int m_MinRange;


    public SampleTask()
    {
    }

    public virtual void ExecuteRange(enki::TaskSetPartition range, uint threadIndex)
    {
        m_task(range.start, range.end, threadIndex, m_taskContext);
    }
}