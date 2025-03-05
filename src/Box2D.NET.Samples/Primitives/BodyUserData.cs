namespace Box2D.NET.Samples.Primitives;

public static class BodyUserData
{
    public static BodyUserData<T> Create<T>(T value)
    {
        var userData = new BodyUserData<T>();
        userData.Value = value;

        return userData;
    }
}
public class BodyUserData<T>
{
    public T Value;

    internal BodyUserData()
    {
        
    }
}