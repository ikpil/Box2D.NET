// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using static Box2D.NET.id;
using static Box2D.NET.shape;
using static Box2D.NET.solver;
using static Box2D.NET.body;
using static Box2D.NET.world;
using static Box2D.NET.joint;
using static Box2D.NET.distance_joint;
using static Box2D.NET.motor_joint;
using static Box2D.NET.mouse_joint;
using static Box2D.NET.prismatic_joint;
using static Box2D.NET.revolute_joint;
using static Box2D.NET.weld_joint;
using static Box2D.NET.wheel_joint;
using static Box2D.NET.id_pool;
using static Box2D.NET.manifold;

namespace Box2D.NET.Test;

public class test_id
{
    [Test]
    public void IdTest()
    {
        ulong x = 0x0123456789ABCDEFul;

        {
            b2BodyId id = b2LoadBodyId(x);
            ulong y = b2StoreBodyId(id);
            Assert.That(x, Is.EqualTo(y));
        }

        {
            b2ShapeId id = b2LoadShapeId(x);
            ulong y = b2StoreShapeId(id);
            Assert.That(x, Is.EqualTo(y));
        }

        {
            b2ChainId id = b2LoadChainId(x);
            ulong y = b2StoreChainId(id);
            Assert.That(x, Is.EqualTo(y));
        }

        {
            b2JointId id = b2LoadJointId(x);
            ulong y = b2StoreJointId(id);
            Assert.That(x, Is.EqualTo(y));
        }
    }
}