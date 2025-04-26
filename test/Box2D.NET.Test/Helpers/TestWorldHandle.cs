using System;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Ids;

namespace Box2D.NET.Test.Helpers;

public class TestWorldHandle : IDisposable
{
    public B2WorldId Id { get; private set; }

    public TestWorldHandle(B2WorldId worldId)
    {
        Id = worldId;
    }

    public void Dispose()
    {
        if (B2_IS_NON_NULL(Id))
        {
            b2DestroyWorld(Id);
            Id = b2_nullWorldId;
        }
    }

}