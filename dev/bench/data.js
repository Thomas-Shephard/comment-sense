window.BENCHMARK_DATA = {
  "lastUpdate": 1773595597053,
  "repoUrl": "https://github.com/Thomas-Shephard/comment-sense",
  "entries": {
    "CommentSense Memory Allocations": [
      {
        "commit": {
          "author": {
            "email": "thomas@thomas-shephard.com",
            "name": "Thomas Shephard",
            "username": "Thomas-Shephard"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "d056b39bf8530aadf855e6854e9de451c36b6b45",
          "message": "ci: fix gh-pages tracking (#110)",
          "timestamp": "2026-03-15T17:24:48Z",
          "tree_id": "c77c5e251ef478080c0fe7c3e5afcbc17c7cba00",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/d056b39bf8530aadf855e6854e9de451c36b6b45"
        },
        "date": 1773595596694,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 4946651,
            "unit": "ns",
            "range": "± 5687965.641843942"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 18906984,
            "unit": "ns",
            "range": "± 21300088.584931415"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 5286312,
            "unit": "ns",
            "range": "± 4140077.2211372345"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 8330018,
            "unit": "ns",
            "range": "± 13500897.629653482"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 53165504,
            "unit": "ns",
            "range": "± 5128648.879471378"
          }
        ]
      }
    ]
  }
}