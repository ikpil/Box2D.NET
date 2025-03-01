// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public enum B2SolverStageType
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
