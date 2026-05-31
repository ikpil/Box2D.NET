namespace Box2D.NET.Test.Primitives;

public class DummyObject<T>
{
    public T Value;

    public DummyObject()
    {
        Value = default(T);
    }

    public DummyObject(T value)
    {
        Value = value;
    }
}