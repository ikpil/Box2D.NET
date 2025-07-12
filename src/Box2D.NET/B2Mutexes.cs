using System.Threading;

namespace Box2D.NET
{
    public class B2Mutexes
    {
        public static B2Mutex b2CreateMutex()
        {
            B2Mutex m = new B2Mutex();
            m.lc = new object();
            return m;
        }

        public static void b2DestroyMutex(ref B2Mutex m)
        {
            m.lc = null;
        }

        public static void b2LockMutex(ref B2Mutex m)
        {
            Monitor.Enter(m.lc);
        }

        public static void b2UnlockMutex(ref B2Mutex m)
        {
            Monitor.Exit(m.lc);
        }
    }
}