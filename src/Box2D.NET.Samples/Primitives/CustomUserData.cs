// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Samples.Primitives;

public static class CustomUserData
{
    public static CustomUserData<T> Create<T>(T value)
    {
        var userData = new CustomUserData<T>();
        userData.Value = value;

        return userData;
    }
}
public class CustomUserData<T>
{
    public T Value;

    internal CustomUserData()
    {
        
    }
}
