// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Box2D.NET.Samples.Primitives;

public class TaskScheduler
{
    private ConcurrentQueue<Task> _runningTasks;
    private ConcurrentQueue<uint> _workers;
    private SemaphoreSlim _semaphore;

    private int _workerCount;

    public void Initialize(int workerCount)
    {
        _workerCount = workerCount;
        _semaphore = new SemaphoreSlim(workerCount);
        _runningTasks = new ConcurrentQueue<Task>();
        _workers = new ConcurrentQueue<uint>();
        for (int i = 0; i < _workerCount; ++i)
        {
            _workers.Enqueue((uint)i);
        }
    }

    public void AddTaskSetToPipe(SampleTask task)
    {
        // single thread
        if (1 >= _workerCount)
        {
            task.m_task.Invoke(0, task.m_SetSize, 0, task.m_taskContext);
            return;
        }

        uint loop = 0;
        int index = 0;
        int remain = task.m_SetSize;
        int minRange = task.m_MinRange;
        while (0 < remain)
        {
            var stepCount = Math.Min(remain, minRange);
            remain -= stepCount;

            var startIndex = index;
            var endIndex = startIndex + stepCount;

            index = endIndex;

            var running = Task.Run(async () =>
            {
                await _semaphore.WaitAsync();
                _workers.TryDequeue(out uint workerIndex);
                try
                {
                    task.m_task.Invoke(startIndex, endIndex, workerIndex, task.m_taskContext);
                }
                finally
                {
                    _workers.Enqueue(workerIndex);
                    _semaphore.Release();
                }
            });

            _runningTasks.Enqueue(running);
        }
    }

    public void WaitforTask(SampleTask task)
    {
        // wait!
        while (_runningTasks.TryDequeue(out var runningTask))
        {
            runningTask.Wait();
        }
    }
}