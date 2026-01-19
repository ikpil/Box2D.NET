using System.Runtime.InteropServices;

namespace Box2D.NET
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct B2UserData
    {
        public static B2UserData Empty => default;
        
        // Value types (Overlap at offset 0)
        [FieldOffset(0)] public readonly double dValue;
        [FieldOffset(0)] public readonly long iValue;
        [FieldOffset(0)] public readonly ulong ulValue;

        // Reference type (Offset 8 to avoid GC aliasing)
        [FieldOffset(8)] public readonly object oValue;

        // Type flag (Offset 16)
        [FieldOffset(16)] public readonly B2UserDataType type;

        public B2UserData(long i) : this()
        {
            type = B2UserDataType.Signed;
            iValue = i;
        }

        public B2UserData(ulong ul) : this()
        {
            type = B2UserDataType.Unsigned;
            ulValue = ul;
        }

        public B2UserData(double d) : this()
        {
            type = B2UserDataType.Double;
            dValue = d;
        }

        public B2UserData(object o) : this()
        {
            type = B2UserDataType.Ref;
            oValue = o;
        }


        public bool IsEmpty()
        {
            return type == B2UserDataType.None;
        }

        public long GetSigned(long def = 0)
        {
            return type == B2UserDataType.Signed
                ? iValue
                : def;
        }

        public ulong GetUnsigned(ulong def = 0)
        {
            return type == B2UserDataType.Unsigned
                ? ulValue
                : def;
        }

        public double GetDouble(double def = 0)
        {
            return type == B2UserDataType.Double
                ? dValue
                : def;
        }

        public T GetRef<T>() where T : class
        {
            return type == B2UserDataType.Ref
                ? oValue as T
                : null;
        }

        public static B2UserData Signed(long i)
        {
            return new B2UserData(i);
        }

        public static B2UserData Unsigned(ulong u)
        {
            return new B2UserData(u);
        }

        public static B2UserData Double(double d)
        {
            return new B2UserData(d);
        }

        public static B2UserData Ref<T>(T o) where T : class
        {
            return new B2UserData(o);
        }
    }
}