// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // A contact edge is used to connect bodies and contacts together
    // in a contact graph where each body is a node and each contact
    // is an edge. A contact edge belongs to a doubly linked list
    // maintained in each attached body. Each contact has two contact
    // edges, one for each attached body.
    public struct B2ContactEdge
    {
        public int bodyId;
        public int prevKey;
        public int nextKey;
    }
}
