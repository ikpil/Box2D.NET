namespace Box2D.NET.Samples.Extensions;

public static class ArrayExtensions
{
    public static int FindIndex<T>(this T[] a, T target)
    {
        for (int i = 0; i < a.Length; ++i)
        {
            if (a[i].Equals(target))
                return i;
        }

        return -1;
    }
}