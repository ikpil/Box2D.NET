# Dense contact performance and patch inventory

This change collects all portable library patches accumulated downstream since
Box2D.NET tag `3.1.654` (`5efc96def866edbb4e5a9368d84de5bf8c2dcaca`). It adapts them to
upstream `main` at `154bacd0fddc0e72e484d3061cdc76aa2d842994`, preserving the newer
body flags, kinematic scatter guards, island representation, scheduler and task API.
It includes the original buffer reuse change in PR #101 and both static-joint
changes from PR #102.

## Complete inventory

The downstream audit compared every imported C# file against its pinned source,
excluding visibility and compiler-only adaptations. The resulting portable changes
are covered here:

| Accumulated patch | Current upstream implementation |
| --- | --- |
| Per-world step context and graph-color block array | `B2World`, `B2StepContext`, `B2Worlds`, `B2Solvers`; reset transient fields, clear retained references on destruction |
| Empty overflow-contact allocation avoidance | Early returns in warm start, solve and restitution in `B2ContactSolvers` |
| Static revolute and wheel identity state reuse | `B2RevoluteJoints`, `B2WheelJoints`; retain dynamic-body write guards |
| Swap-removal preserves distinct spare reference objects | `B2Arrays`; island removal uses the shared primitive |
| Contact and island creation uses prepared array slots | `B2Contacts`, `B2Islands`; current upstream island arrays also retain capacity |
| Sleep/wake solver-set capacity retention | `B2SolverSets`; world destruction releases active and dormant storage |
| Eight-lane contact arithmetic and storage | `B2FloatW`, `B2Cores`, `B2ContactConstraintWide`, `B2ContactSolvers`, `B2RuntimeValidator`; gather/scatter and layout checks change together |
| Absent second contact point avoids normal/friction work | Warm start and solve skip only when every second-point normal mass is zero |
| Serial stages execute directly | `B2Solvers`; avoids stealing synchronization when only one worker is active |
| Retained multiworker jobs and constraint failure signaling | Adapted to the existing upstream scheduler through `B2ParallelFor`, `B2ParallelForShared`, `B2World`, `B2StepContext`, `B2Solvers`; join handles before propagating errors |
| New upstream preparation/caller storage | Reuse contact/joint span arrays and worker-0 context, rather than introducing new per-step allocations while porting the earlier reuse patch |

The downstream engine's visibility changes, nullable/XML warning directives and
`B2ArenaAllocatorIndexer` visibility are embedding adaptations, not library patches.
Its awake-body worker policy, scene callbacks, fixture tags, contact snapshots,
interpolation and object-pair identity changes require engine-owned types and remain
in that engine. This PR uses upstream's built-in scheduler rather than copying the
engine's task adapter. Upstream already provides `B2WorldDef.capacity` for expected
body/contact storage; no engine-specific capacity API is added here.

## Numerical and API boundaries

Arithmetic keeps separate multiply/add operations. Min/max, clamp, masks and blend
preserve scalar NaN and signed-zero selection. .NET 8+ uses `Vector256<float>` and
its runtime fallback; `netstandard2.1` keeps eight scalar lanes without adding an
intrinsics dependency. Its project uses C# 12 for the scalar target as well, enabling
compiler caching of static method-group delegates; modern targets retain their SDK
language defaults. Existing `Vector<float>` overloads are unchanged.

`B2FloatW` becomes 32 bytes and wide constraint indices become eight elements.
On .NET 8+, `X/Y/Z/W` are ref-returning properties over vector storage; scalar
fallback retains fields. Direct reads/writes and the four-argument constructor
remain usable (upper lanes start at zero), but reflection/field ABI and wide
structure layout change: consumers of these public low-level types must rebuild.
The two-argument `b2DestroySolverSet` entry point remains available.

Cached buffers retain their peak capacity until world destruction. New topology
or a nested parallel-for may allocate. A fatal callback/constraint error joins
started work and permits world destruction; restarting a partly solved world is
not a recovery contract.

## Reproduce

```sh
DOTNET_TieredCompilation=0 dotnet run --project tools/Box2D.NET.Benchmark -c Release -f net10.0 -- --dense 1
DOTNET_TieredCompilation=0 dotnet run --project tools/Box2D.NET.Benchmark -c Release -f net10.0 -- --dense 4
```

For the baseline, check out `154bacd0fddc0e72e484d3061cdc76aa2d842994` in a separate
checkout and copy only `DenseBenchmark.cs` and the `--dense` dispatch in `Program.Main`
there. The library sources remain unchanged for baseline measurements.

The benchmark encloses 1,536 always-awake circles in a tank, uses 1/144-second steps
with four substeps, warms 1,600 steps and samples 1,024. Construction, checksums and
JSON serialization are outside the allocation bracket. It reports both owner and
all-thread managed allocation, final topology, stage profiles and the complete
position/rotation/velocity SHA-256.

Linux x64, Ryzen 7 5700X, .NET 10.0.1, Release, `DOTNET_TieredCompilation=0`, three
fresh processes per row. Runs alternate upstream/PR serial/PR four-worker within
each trial. Values below are medians of trial means and trial p99 values, not
percentiles of pooled samples. Desktop load was uncontrolled.

| Implementation | Workers | Mean ms | p99 ms | All-thread managed bytes per sampled run |
| --- | ---: | ---: | ---: | ---: |
| Upstream main `154bacd` | 1 | 6.905 | 7.612 | 2,090,040 |
| This change | 1 | 0.997 | 1.153 | 0 |
| This change, built-in scheduler | 4 | 0.465 | 0.594 | 0 |

All nine runs have 5,877 potential and 3,813 colored/overflow contacts. The complete
state hash is identical:
`C2554173F6A5D78AD49163D4BBF0909906B31A92518A795A2FEC21A63E22CEDD`.
Raw trials are in [dense-5700x-net10.json](results/dense-5700x-net10.json).
This measures this kernel workload, not rendered FPS, other CPUs, or a native
Box2D comparison. The native/downstream pinned-version results use an older
algorithm and are intentionally not used as this PR's baseline.

## Verification

- Release library build: `netstandard2.1`, `net8.0`, `net9.0`, `net10.0`.
- Entire NUnit suite: 208/208 in Release on .NET 8 and .NET 10; 208/208 in Debug on .NET 10.
- Regression checks cover eight-lane edge arithmetic, constructor/indexer behavior,
  spare-slot identity, sleep/wake retention and destruction, allocation-free warmed
  static revolute/wheel steps, uneven/nested parallel ranges, callback failures and
  fatal constraint peer cancellation/joining.
- AVX-disabled regression/determinism tests: 12/12. One AVX-disabled dense run
  retains the same state hash and zero all-thread allocation (1.391 ms mean,
  1.615 ms p99); this is a local fallback check, not execution on another CPU.
- Scalar `netstandard2.1` assembly was directly loaded by a .NET 10 test harness
  referencing the same regression/determinism test sources: 12/12 passed.
- Existing falling-hinge determinism expectations remain unchanged across worker
  counts: sleep step 328, hash `0x26E08AEE`.
- Local execution does not establish ARM, browser or other operating-system results.
  .NET 9 built successfully; its runtime is not installed on the measured host.
