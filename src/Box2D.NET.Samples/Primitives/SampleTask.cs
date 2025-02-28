// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

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

    public virtual void ExecuteRange(int start, int end, uint threadIndex)
    {
        m_task(start, end, threadIndex, m_taskContext);
    }
}