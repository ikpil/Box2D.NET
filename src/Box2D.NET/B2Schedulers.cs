// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Atomics;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Timers;
using static Box2D.NET.B2Semaphores;
using static Box2D.NET.B2Threads;

namespace Box2D.NET
{
    public static class B2Schedulers
    {
        // Try to claim and execute one pending task.
        // Returns true if work was performed, false otherwise.
        private static bool b2SchedulerExecuteOne(B2Scheduler scheduler)
        {
            int taskCount = b2AtomicLoadInt(ref scheduler.nextSlot);
            for (int t = 0; t < taskCount; ++t)
            {
                ref B2SchedulerTask task = ref scheduler.tasks[t];
                if (b2AtomicLoadInt(ref task.status) != (int)B2SchedulerTaskStatus.b2_schedulerPending)
                {
                    continue;
                }

                if (b2AtomicCompareExchangeInt(ref task.status, (int)B2SchedulerTaskStatus.b2_schedulerPending, (int)B2SchedulerTaskStatus.b2_schedulerClaimed) == false)
                {
                    continue;
                }

                task.callback(task.taskContext);

                b2AtomicStoreInt(ref task.status, (int)B2SchedulerTaskStatus.b2_schedulerComplete);
                return true;
            }

            return false;
        }

        // Background worker thread entry point.
        internal static void b2SchedulerWorkerMain(object context)
        {
            B2SchedulerWorkerContext workerContext = (B2SchedulerWorkerContext)context;
            B2Scheduler scheduler = workerContext.scheduler;

            while (true)
            {
                b2WaitSemaphore(ref scheduler.taskSemaphore);

                if (b2AtomicLoadInt(ref scheduler.shutdown) != 0)
                {
                    break;
                }

                // Claim and execute all available work
                while (b2SchedulerExecuteOne(scheduler))
                {
                }
            }
        }

        internal static B2Scheduler b2CreateScheduler(int workerCount)
        {
            B2_ASSERT(0 < workerCount && workerCount <= B2_MAX_WORKERS);
            B2Scheduler scheduler = new B2Scheduler();

            scheduler.workerCount = workerCount;
            int threadCount = workerCount - 1;
            scheduler.threadCount = threadCount;
            scheduler.taskSemaphore = b2CreateSemaphore(0);
            b2AtomicStoreInt(ref scheduler.shutdown, 0);
            b2AtomicStoreInt(ref scheduler.nextSlot, 0);

            for (int i = 0; i < scheduler.tasks.Length; ++i)
            {
                scheduler.tasks[i] = new B2SchedulerTask();
            }

            // Background threads use indices 1..workerCount-1.
            // Main thread uses index 0.
            for (int i = 0; i < threadCount; ++i)
            {
                scheduler.workerContexts[i] = new B2SchedulerWorkerContext();
                scheduler.workerContexts[i].scheduler = scheduler;
                scheduler.workerContexts[i].threadIndex = i + 1;

                string name = $"box2d_worker_{i + 1:00}";
                scheduler.threads[i] = b2CreateThread(b2SchedulerWorkerMain, scheduler.workerContexts[i], name);
            }

            return scheduler;
        }


        internal static void b2DestroyScheduler(B2Scheduler scheduler)
        {
            b2AtomicStoreInt(ref scheduler.shutdown, 1);

            // Wake all background threads so they see the shutdown flag
            for (int i = 0; i < scheduler.threadCount; ++i)
            {
                b2SignalSemaphore(ref scheduler.taskSemaphore);
            }

            for (int i = 0; i < scheduler.threadCount; ++i)
            {
                b2JoinThread(scheduler.threads[i]);
                scheduler.threads[i] = null;
            }

            b2DestroySemaphore(ref scheduler.taskSemaphore);

            scheduler.threads = null;
            scheduler.workerContexts = null;
            scheduler.workerCount = 0;
        }

        internal static void b2ResetScheduler(B2Scheduler scheduler)
        {
            b2AtomicStoreInt(ref scheduler.nextSlot, 0);
        }

        // See b2EnqueueTaskCallback and b2FinishTaskCallback
        internal static object b2SchedulerEnqueueTask(b2TaskCallback task, object taskContext, object userContext)
        {
            B2Scheduler scheduler = (B2Scheduler)userContext;

            int slot = b2AtomicFetchAddInt(ref scheduler.nextSlot, 1);
            B2_ASSERT(slot < B2_MAX_TASKS);

            ref B2SchedulerTask schedulerTask = ref scheduler.tasks[slot];
            schedulerTask.callback = task;
            schedulerTask.taskContext = taskContext;

            // Memory fence: status must be published after callback and context are written
            b2AtomicStoreInt(ref schedulerTask.status, (int)B2SchedulerTaskStatus.b2_schedulerPending);

            // One wake per enqueue is enough: at most one worker picks up each task.
            b2SignalSemaphore(ref scheduler.taskSemaphore);

            return schedulerTask;
        }

        internal static void b2SchedulerFinishTask(object userTask, object userContext)
        {
            if (userTask == null)
            {
                return;
            }

            B2Scheduler scheduler = (B2Scheduler)userContext;
            B2SchedulerTask waitTask = (B2SchedulerTask)userTask;

            // Main thread helps execute any available work while waiting for the
            // target task to complete. This keeps the main thread from idling when
            // background threads are busy on other tasks from the same phase.
            while (b2AtomicLoadInt(ref waitTask.status) != (int)B2SchedulerTaskStatus.b2_schedulerComplete)
            {
                if (b2SchedulerExecuteOne(scheduler) == false)
                {
                    b2Yield();
                }
            }
        }
    }
}
