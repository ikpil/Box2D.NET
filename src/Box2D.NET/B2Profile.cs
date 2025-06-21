// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    //! @cond
    /// Profiling data. Times are in milliseconds.
    public struct B2Profile
    {
        public float step;
        public float pairs;
        public float collide;
        public float solve;
        public float mergeIslands;
        public float prepareStages;
        public float solveConstraints;
        public float prepareConstraints;
        public float integrateVelocities;
        public float warmStart;
        public float solveImpulses;
        public float integratePositions;
        public float relaxImpulses;
        public float applyRestitution;
        public float storeImpulses;
        public float splitIslands;
        public float transforms;
        public float sensorHits;
        public float jointEvents;
        public float hitEvents;
        public float refit;
        public float bullets;
        public float sleepIslands;
        public float sensors;
    }
}
