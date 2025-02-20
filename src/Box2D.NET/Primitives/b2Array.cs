namespace Box2D.NET.Primitives
{
    // Array declaration that doesn't need the type T to be defined
    public class b2Array<T>
    {
        public T[] data;
        public int count;
        public int capacity;
    }
}