namespace Box2D.NET.Primitives
{
    /// A begin touch event is generated when two shapes begin touching.
    public class b2ContactBeginTouchEvent
    {
        /// Id of the first shape
        public b2ShapeId shapeIdA;

        /// Id of the second shape
        public b2ShapeId shapeIdB;

        /// The initial contact manifold. This is recorded before the solver is called,
        /// so all the impulses will be zero.
        public b2Manifold manifold;

        public b2ContactBeginTouchEvent()
        {
        }

        public b2ContactBeginTouchEvent(b2ShapeId shapeIdA, b2ShapeId shapeIdB, ref b2Manifold manifold)
        {
            this.shapeIdA = shapeIdA;
            this.shapeIdB = shapeIdB;
            this.manifold = manifold;
        }
    }
}