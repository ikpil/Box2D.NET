// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Cached contact data stored in the island for fast contiguous iteration.
    // Avoids touching b2Contact during union-find in b2SplitIsland.
    public class B2ContactLink
    {
        public int contactId;
        public int bodyIdA;
        public int bodyIdB;

        public B2ContactLink ToCopy()
        {
            var copy = new B2ContactLink();
            copy.contactId = contactId;
            copy.bodyIdA = bodyIdA;
            copy.bodyIdB = bodyIdB;

            return copy;
        }
    }
}
