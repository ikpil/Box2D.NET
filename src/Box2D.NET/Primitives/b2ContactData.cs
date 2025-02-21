namespace Box2D.NET.Primitives
{
    /// The contact data for two shapes. By convention the manifold normal points
    /// from shape A to shape B.
    /// @see b2Shape_GetContactData() and b2Body_GetContactData()
    public class b2ContactData
    {
        public b2ShapeId shapeIdA;
        public b2ShapeId shapeIdB;
        public b2Manifold manifold;
    }
}