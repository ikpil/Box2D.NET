// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public enum B2TracyCZone
    {
        pair_task,
        update_pairs,
        create_contacts,
        prepare_overflow_contact,
        warmstart_overflow_contact,
        solve_contact,
        overflow_resitution,
        prepare_contact,
        warm_start_contact,
        restitution,
        store_impulses,
        merge_islands,
        split,
        sensor_task,
        overlap_sensors,
        sensor_state,
        integrate_velocity,
        prepare_joints,
        warm_joints,
        solve_joints,
        integrate_positions,
        ccd,
        finalize_transfprms,
        bullet_body_task,
        merge,
        prepare_stages,
        solve_constraints,
        update_transforms,
        joint_events,
        hit_events,
        refit_bvh,
        bullets,
        sleep_islands,
        collide_task,
        tree_task,
        collide,
        contact_state,
        world_step,
        sensor_hits,
    }
}
