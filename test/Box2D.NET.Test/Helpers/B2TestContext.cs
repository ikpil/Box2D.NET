// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Ids;

namespace Box2D.NET.Test.Helpers;

public class B2TestContext : IDisposable
{
    public B2WorldId WorldId { get; private set; }

    public static B2TestContext CreateFor()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = 1;
        worldDef.enqueueTask = EnqueueTask;
        worldDef.finishTask = FinishTask;
        worldDef.userTaskContext = null;
        worldDef.enableSleep = true;

        B2WorldId worldId = b2CreateWorld(ref worldDef);

        return new B2TestContext(worldId);
    }

    public B2TestContext(B2WorldId worldId)
    {
        WorldId = worldId;
    }

    public void Dispose()
    {
        if (B2_IS_NON_NULL(WorldId))
        {
            b2DestroyWorld(WorldId);
            WorldId = b2_nullWorldId;
        }
    }


    private static object EnqueueTask(b2TaskCallback task, int itemCount, int minRange, object taskContext, object userContext)
    {
        // Execute the task immediately for testing purposes
        task(0, itemCount, 0, taskContext);
        return null;
    }

    private static void FinishTask(object userTask, object userContext)
    {
        // No cleanup needed for testing
    }
}