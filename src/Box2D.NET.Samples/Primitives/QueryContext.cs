// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Ids;

namespace Box2D.NET.Samples.Primitives;

public class QueryContext
{
    public B2Vec2 point;
    public B2BodyId bodyId = b2_nullBodyId;

    public QueryContext(B2Vec2 point, B2BodyId bodyId)
    {
        this.point = point;
        this.bodyId = bodyId;
    }
}
