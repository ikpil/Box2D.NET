// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

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
