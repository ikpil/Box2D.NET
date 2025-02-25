// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;

namespace Box2D.NET.Samples.Primitives;

public class Proxy
{
    public b2AABB box;
    public b2AABB fatBox;
    public b2Vec2 position;
    public b2Vec2 width;
    public int proxyId;
    public int rayStamp;
    public int queryStamp;
    public bool moved;
}

