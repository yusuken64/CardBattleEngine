```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
Intel Core Ultra 7 165H 1.40GHz, 1 CPU, 22 logical and 16 physical cores
.NET SDK 10.0.301
  [Host] : .NET 8.0.28 (8.0.28, 8.0.2826.26413), X64 RyuJIT x86-64-v3

Toolchain=InProcessNoEmitToolchain  IterationCount=15  LaunchCount=1  
WarmupCount=3  

```
| Method               | EntityCount | MaxDepth | Mean     | Error    | StdDev   | Gen0       | Gen1       | Allocated |
|--------------------- |------------ |--------- |---------:|---------:|---------:|-----------:|-----------:|----------:|
| EngineIterateActions | 7           | 4        | 177.9 ms | 18.53 ms | 16.42 ms | 32000.0000 | 13000.0000 | 392.08 MB |
