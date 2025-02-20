namespace Box2D.NET.Primitives
{
    /// Version numbering scheme.
    /// See https://semver.org/
    public struct b2Version
    {
        /// Significant changes
        public int major;

        /// Incremental changes
        public int minor;

        /// Bug fixes
        public int revision;

        public b2Version(int major, int minor, int revision)
        {
            this.major = major;
            this.minor = minor;
            this.revision = revision;
        }
    }
}