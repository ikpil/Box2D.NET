using NUnit.Framework;
using Box2D.NET;

namespace Box2D.NET.Test
{
    public class B2UserDataTest
    {
        [Test]
        public void TestEmpty()
        {
            var userData = B2UserData.Empty;
            Assert.That(userData.IsEmpty(), Is.True);
            Assert.That(userData.GetSigned(), Is.EqualTo(0));
            Assert.That(userData.GetUnsigned(), Is.EqualTo(0));
            Assert.That(userData.GetDouble(), Is.EqualTo(0.0));
            Assert.That(userData.GetRef<object>(), Is.Null);
        }

        [Test]
        public void TestSigned()
        {
            long value = -1234567890L;
            var userData = B2UserData.Signed(value);
            
            Assert.That(userData.IsEmpty(), Is.False);
            Assert.That(userData.type, Is.EqualTo(B2UserDataType.Signed));
            Assert.That(userData.GetSigned(), Is.EqualTo(value));
            
            // Default value check
            Assert.That(userData.GetUnsigned(123), Is.EqualTo(123));
            Assert.That(userData.GetDouble(1.23), Is.EqualTo(1.23));
            Assert.That(userData.GetRef<string>(), Is.Null);
        }

        [Test]
        public void TestUnsigned()
        {
            ulong value = 1234567890UL;
            var userData = B2UserData.Unsigned(value);
            
            Assert.That(userData.IsEmpty(), Is.False);
            Assert.That(userData.type, Is.EqualTo(B2UserDataType.Unsigned));
            Assert.That(userData.GetUnsigned(), Is.EqualTo(value));
            
            // Default value check
            Assert.That(userData.GetSigned(-1), Is.EqualTo(-1));
            Assert.That(userData.GetDouble(1.23), Is.EqualTo(1.23));
            Assert.That(userData.GetRef<string>(), Is.Null);
        }

        [Test]
        public void TestDouble()
        {
            double value = 3.14159;
            var userData = B2UserData.Double(value);
            
            Assert.That(userData.IsEmpty(), Is.False);
            Assert.That(userData.type, Is.EqualTo(B2UserDataType.Double));
            Assert.That(userData.GetDouble(), Is.EqualTo(value));
            
            // Default value check
            Assert.That(userData.GetSigned(-1), Is.EqualTo(-1));
            Assert.That(userData.GetUnsigned(123), Is.EqualTo(123));
            Assert.That(userData.GetRef<string>(), Is.Null);
        }

        [Test]
        public void TestRef()
        {
            string value = "Hello Box2D";
            var userData = B2UserData.Ref(value);
            
            Assert.That(userData.IsEmpty(), Is.False);
            Assert.That(userData.type, Is.EqualTo(B2UserDataType.Ref));
            Assert.That(userData.GetRef<string>(), Is.EqualTo(value));
            
            // Default value check
            Assert.That(userData.GetSigned(-1), Is.EqualTo(-1));
            Assert.That(userData.GetUnsigned(123), Is.EqualTo(123));
            Assert.That(userData.GetDouble(1.23), Is.EqualTo(1.23));
        }

        [Test]
        public void TestRefTypeMismatch()
        {
            string value = "Hello Box2D";
            var userData = B2UserData.Ref(value);
            
            // Trying to get as int (boxed) should return null because stored object is string
            Assert.That(userData.GetRef<object>(), Is.EqualTo(value));
            
            // This cast will fail inside GetRef and return null
            var intRef = userData.GetRef<B2Body>(); 
            Assert.That(intRef, Is.Null);
        }
    }
}