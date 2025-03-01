// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Arrays;

namespace Box2D.NET
{
    // This holds solver set data. The following sets are used:
    // - static set for all static bodies (no contacts or joints)
    // - active set for all active bodies with body states (no contacts or joints)
    // - disabled set for disabled bodies and their joints
    // - all further sets are sleeping island sets along with their contacts and joints
    // The purpose of solver sets is to achieve high memory locality.
    // https://www.youtube.com/watch?v=nZNd5FjSquk
    public class B2SolverSet
    {
        // Body array. Empty for unused set.
        public B2Array<B2BodySim> bodySims;

        // Body state only exists for active set
        public B2Array<B2BodyState> bodyStates;

        // This holds sleeping/disabled joints. Empty for static/active set.
        public B2Array<B2JointSim> jointSims;

        // This holds all contacts for sleeping sets.
        // This holds non-touching contacts for the awake set.
        public B2Array<B2ContactSim> contactSims;

        // The awake set has an array of islands. Sleeping sets normally have a single islands. However, joints
        // created between sleeping sets causes the sets to merge, leaving them with multiple islands. These sleeping
        // islands will be naturally merged with the set is woken.
        // The static and disabled sets have no islands.
        // Islands live in the solver sets to limit the number of islands that need to be considered for sleeping.
        public B2Array<B2IslandSim> islandSims;

        // Aligns with b2World::solverSetIdPool. Used to create a stable id for body/contact/joint/islands.
        public int setIndex;

        public void Clear()
        {
            b2Array_Clear(ref bodySims);
            b2Array_Clear(ref bodyStates);
            b2Array_Clear(ref jointSims);
            b2Array_Clear(ref contactSims);
            b2Array_Clear(ref islandSims);
            setIndex = 0;
        }
    }
}
