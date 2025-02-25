// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    //! @cond
    /// Profiling data. Times are in milliseconds.
    public class b2Profile
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
        public float hitEvents;
        public float refit;
        public float bullets;
        public float sleepIslands;
        public float sensors;

        public void Clear()
        {
            step = 0;
            pairs = 0;
            collide = 0;
            solve = 0;
            mergeIslands = 0;
            prepareStages = 0;
            solveConstraints = 0;
            prepareConstraints = 0;
            integrateVelocities = 0;
            warmStart = 0;
            solveImpulses = 0;
            integratePositions = 0;
            relaxImpulses = 0;
            applyRestitution = 0;
            storeImpulses = 0;
            splitIslands = 0;
            transforms = 0;
            hitEvents = 0;
            refit = 0;
            bullets = 0;
            sleepIslands = 0;
            sensors = 0;
        }
    }
}
