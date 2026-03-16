window.BENCHMARK_DATA = {
  "lastUpdate": 1773685780886,
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
      },
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
          "id": "ccd6a13fb8937f305bfb3eed7a3f07fb58283aa0",
          "message": "feat: implement inheritdoc validation (#109)",
          "timestamp": "2026-03-16T18:27:59Z",
          "tree_id": "5ccd637e67da278da3d322f487819878bfea334c",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/ccd6a13fb8937f305bfb3eed7a3f07fb58283aa0"
        },
        "date": 1773685780612,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 4970947,
            "unit": "ns",
            "range": "± 7632263.639695618"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 19233520,
            "unit": "ns",
            "range": "± 58965406.61144239"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 5436755,
            "unit": "ns",
            "range": "± 4250390.61094563"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 8713972,
            "unit": "ns",
            "range": "± 21617187.15524557"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 53098696,
            "unit": "ns",
            "range": "± 2874433.9120195983"
          }
        ]
      }
    ]
  }
}