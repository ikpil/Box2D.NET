// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Body move events triggered when a body moves.
    /// Triggered when a body moves due to simulation. Not reported for bodies moved by the user.
    /// This also has a flag to indicate that the body went to sleep so the application can also
    /// sleep that actor/entity/object associated with the body.
    /// On the other hand if the flag does not indicate the body went to sleep then the application
    /// can treat the actor/entity/object associated with the body as awake.
    /// This is an efficient way for an application to update game object transforms rather than
    /// calling functions such as b2Body_GetTransform() because this data is delivered as a contiguous array
    /// and it is only populated with bodies that have moved.
    /// @note If sleeping is disabled all dynamic and kinematic bodies will trigger move events.
    public struct B2BodyMoveEvent
    {
        public object userData;
        public B2Transform transform;
        public B2BodyId bodyId;
        public bool fellAsleep;
    }
}
