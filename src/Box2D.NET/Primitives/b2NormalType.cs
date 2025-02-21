namespace Box2D.NET.Primitives
{
    public enum b2NormalType
    {
        // This means the normal points in a direction that is non-smooth relative to a convex vertex and should be skipped
        b2_normalSkip,

        // This means the normal points in a direction that is smooth relative to a convex vertex and should be used for collision
        b2_normalAdmit,

        // This means the normal is in a region of a concave vertex and should be snapped to the segment normal
        b2_normalSnap
    };
}