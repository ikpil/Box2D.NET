namespace Box2D.NET.Primitives
{
    public enum b2SolverStageType
    {
        b2_stagePrepareJoints,
        b2_stagePrepareContacts,
        b2_stageIntegrateVelocities,
        b2_stageWarmStart,
        b2_stageSolve,
        b2_stageIntegratePositions,
        b2_stageRelax,
        b2_stageRestitution,
        b2_stageStoreImpulses
    }
}