using System.Runtime.InteropServices;
using NUnit.Framework;
using static Box2D.NET.B2Tables;

namespace Box2D.NET.Test;

public class B2TableTests
{
    [Test]
    public void Test_B2Tables_b2GetHashSetBytes()
    {
        int size = Marshal.SizeOf<B2SetItem>();
        const int capacity = 1024;
        
        B2HashSet set = b2CreateSet(capacity);
        set.capacity = capacity;
        
        int bytes = b2GetHashSetBytes(ref set);
        Assert.That(bytes, Is.EqualTo(size * capacity));
    }
}