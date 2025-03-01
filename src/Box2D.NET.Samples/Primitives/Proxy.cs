// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Samples.Primitives;

public class Proxy
{
    public B2AABB box;
    public B2AABB fatBox;
    public B2Vec2 position;
    public B2Vec2 width;
    public int proxyId;
    public int rayStamp;
    public int queryStamp;
    public bool moved;
}

