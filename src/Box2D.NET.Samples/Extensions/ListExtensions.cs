using System.Collections.Generic;

namespace Box2D.NET.Samples.Extensions;

public static class ListExtensions
{
    public static void Resize<T>(this List<T> list, int count)
    {
        list.EnsureCapacity(count);
        if (list.Count < count)
        {
            for (int i = list.Count; i < count; i++)
            {
                list.Add(default);
            }
        }
        else if (list.Count > count)
        {
            list.RemoveRange(count, list.Count - count);
        }
    }
}