// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using NUnit.Framework;
using static Box2D.NET.B2Ids;

namespace Box2D.NET.Test;

public class test_id
{
    [Test]
    public void IdTest()
    {
        ulong x = 0x0123456789ABCDEFul;

        {
            B2BodyId id = b2LoadBodyId(x);
            ulong y = b2StoreBodyId(id);
            Assert.That(x, Is.EqualTo(y));
        }

        {
            B2ShapeId id = b2LoadShapeId(x);
            ulong y = b2StoreShapeId(id);
            Assert.That(x, Is.EqualTo(y));
        }

        {
            B2ChainId id = b2LoadChainId(x);
            ulong y = b2StoreChainId(id);
            Assert.That(x, Is.EqualTo(y));
        }

        {
            B2JointId id = b2LoadJointId(x);
            ulong y = b2StoreJointId(id);
            Assert.That(x, Is.EqualTo(y));
        }
    }
}
