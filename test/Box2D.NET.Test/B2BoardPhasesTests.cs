using NUnit.Framework;
using static Box2D.NET.B2BoardPhases; 

namespace Box2D.NET.Test;

public class B2BoardPhasesTests
{
    [Test]
    public void Test_B2BoardPhases_B2_PROXY_TYPE()
    {
        // StaticBody = 0
        Assert.That(B2_PROXY_TYPE(0b00), Is.EqualTo(B2BodyType.b2_staticBody));

        // KinematicBody = 1
        Assert.That(B2_PROXY_TYPE(0b01), Is.EqualTo(B2BodyType.b2_kinematicBody));

        // DynamicBody = 2
        Assert.That(B2_PROXY_TYPE(0b10), Is.EqualTo(B2BodyType.b2_dynamicBody));

        {
            // Mask with upper bits (e.g., ID = 123456 << 2)
            int staticKey = (123456 << 2) | 0;
            int kinematicKey = (123456 << 2) | 1;
            int dynamicKey = (123456 << 2) | 2;

            Assert.That(B2_PROXY_TYPE(staticKey), Is.EqualTo(B2BodyType.b2_staticBody));
            Assert.That(B2_PROXY_TYPE(kinematicKey), Is.EqualTo(B2BodyType.b2_kinematicBody));
            Assert.That(B2_PROXY_TYPE(dynamicKey), Is.EqualTo(B2BodyType.b2_dynamicBody));
        }

        {
            // 3 is not a defined B2BodyType (assuming only 0, 1, 2 are used)
            var unknownKey = (123 << 2) | 3;
            var type = B2_PROXY_TYPE(unknownKey);

            Assert.That((int)type, Is.EqualTo(3), "Should extract raw value 3 even if it's not a valid enum");
        }
    } 
}