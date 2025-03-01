// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Version numbering scheme.
    /// See https://semver.org/
    public struct B2Version
    {
        /// Significant changes
        public int major;

        /// Incremental changes
        public int minor;

        /// Bug fixes
        public int revision;

        public B2Version(int major, int minor, int revision)
        {
            this.major = major;
            this.minor = minor;
            this.revision = revision;
        }
    }
}
