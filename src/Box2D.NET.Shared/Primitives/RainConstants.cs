﻿namespace Box2D.NET.Shared.Primitives
{
#if NDEBUG
    public enum RainConstants
    {
        RAIN_ROW_COUNT = 5,
        RAIN_COLUMN_COUNT = 40,
        RAIN_GROUP_SIZE = 5,
    }

#else
    public enum RainConstants
    {
        RAIN_ROW_COUNT = 3,
        RAIN_COLUMN_COUNT = 10,
        RAIN_GROUP_SIZE = 2,
    }
#endif
}