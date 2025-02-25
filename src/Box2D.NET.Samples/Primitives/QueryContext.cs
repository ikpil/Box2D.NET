// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.id;

namespace Box2D.NET.Samples.Primitives;

public class QueryContext
{
    public b2Vec2 point;
    public b2BodyId bodyId = b2_nullBodyId;

    public QueryContext(b2Vec2 point, b2BodyId bodyId)
    {
        this.point = point;
        this.bodyId = bodyId;
    }
}
