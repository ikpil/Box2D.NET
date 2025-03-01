// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Samples.Primitives;

public class TaskScheduler
{
    public void Initialize(int workerCount)
    {
        //Debug.Assert(false);
    }
    
    public void AddTaskSetToPipe(SampleTask task)
    {
        task.m_task.Invoke(0, task.m_SetSize, 0, task.m_taskContext);
        // !!
        //Debug.Assert(false);
    }

    public void WaitforTask(SampleTask task)
    {
        //Debug.Assert(false);
    }
}
