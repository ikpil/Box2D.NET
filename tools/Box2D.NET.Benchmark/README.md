# Box2D.NET physics benchmark

This console application ports `benchmark/main.c` from Box2D commit
`8c65dcb91a5e8fbce53492c169cb4460b53b0b54`. It runs the ten upstream
physics scenarios through the implementations in `Box2D.NET.Shared`.

Run a single benchmark with one worker and one repeat:

```powershell
dotnet run --project tools/Box2D.NET.PhysicsBenchmark/Box2D.NET.PhysicsBenchmark.csproj -c Release -f net8.0 -- -b=0 -w=1 -r=1
```

Options match the native runner:

- `-b=<integer>` selects a benchmark index from 0 through 9.
- `-t=<integer>` limits the maximum worker count.
- `-w=<integer>` runs one worker count.
- `-r=<integer>` selects the repeat count from 1 through 1000.
- `-nc` disables continuous collision detection.
- `-s` writes per-step profile data files.
- `-h` prints usage.

Benchmark indices are `0` compounds, `1` joint_grid, `2` junkyard,
`3` large_pyramid, `4` many_pyramids, `5` rain, `6` smash, `7` spinner,
`8` tumbler, and `9` washer.

The runner writes `<scenario>.csv` timing summaries and, with `-s`,
`<scenario>_t<workers>.dat` profile files in the current directory.

`baselines/` preserves the 47 native reference CSV files from the pinned
upstream commit. They are historical results for the named CPU/SIMD
configurations, not expected values for managed runs on different hardware.

Release builds use the upstream scenario sizes and step counts. Debug builds
use the same reduced scenario sizes and ten-step loop as the native debug
runner, which is useful for smoke checks.
