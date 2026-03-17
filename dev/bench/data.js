window.BENCHMARK_DATA = {
  "lastUpdate": 1773761871757,
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
          "id": "52b8c3b7f54c855311d62ca49a47af3e13c5a0fa",
          "message": "feat: support inherited exception validation (#111)",
          "timestamp": "2026-03-16T21:44:42Z",
          "tree_id": "7cdac7160768790b460f9e29f0928a0451d2c772",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/52b8c3b7f54c855311d62ca49a47af3e13c5a0fa"
        },
        "date": 1773697589724,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 4949712,
            "unit": "ns",
            "range": "± 4731001.242485779"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 19678808,
            "unit": "ns",
            "range": "± 26474519.405311394"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 5315400,
            "unit": "ns",
            "range": "± 3605427.1329590506"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 8271398,
            "unit": "ns",
            "range": "± 16128457.132447047"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 53172136,
            "unit": "ns",
            "range": "± 4218369.684652242"
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
          "id": "c9145025adbb23f1de83c668bffd58551c6d7f6c",
          "message": "feat: enforce property summary style patterns (#112)",
          "timestamp": "2026-03-17T12:51:38Z",
          "tree_id": "63e54ede8ea54430f50096a5afa351798987429f",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/c9145025adbb23f1de83c668bffd58551c6d7f6c"
        },
        "date": 1773752000208,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 4957741,
            "unit": "ns",
            "range": "± 6787619.4119532555"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 20293456,
            "unit": "ns",
            "range": "± 77064958.42262918"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 5270833,
            "unit": "ns",
            "range": "± 4822263.088078098"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 8342790,
            "unit": "ns",
            "range": "± 6331312.086855991"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 53331408,
            "unit": "ns",
            "range": "± 4569713.154178932"
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
          "id": "d01c050423dfcaea7a5ee829bc157db4636df8ae",
          "message": "refactor: improve exception analyzer performance (#113)",
          "timestamp": "2026-03-17T14:59:51Z",
          "tree_id": "091958a28f4c759c88f5ca47da69231b319f217d",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/d01c050423dfcaea7a5ee829bc157db4636df8ae"
        },
        "date": 1773759702163,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 4988272,
            "unit": "ns",
            "range": "± 4706866.315210024"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 20868952,
            "unit": "ns",
            "range": "± 49494440.049583144"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 5446835,
            "unit": "ns",
            "range": "± 4141291.0355012487"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 8423294,
            "unit": "ns",
            "range": "± 16786633.15476704"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 53537976,
            "unit": "ns",
            "range": "± 5625000.289646605"
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
          "id": "04e1d250e6ecf8eadc623b71f8ef1b0d20315bb9",
          "message": "test: improve testing of new c# features (#114)",
          "timestamp": "2026-03-17T15:36:07Z",
          "tree_id": "806af052f90ef7a742b1040ffcf0951afdf17048",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/04e1d250e6ecf8eadc623b71f8ef1b0d20315bb9"
        },
        "date": 1773761870887,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 4967109,
            "unit": "ns",
            "range": "± 8615091.261174012"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 20886392,
            "unit": "ns",
            "range": "± 23391799.750626557"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 5268259,
            "unit": "ns",
            "range": "± 3094591.6360858036"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 8445498,
            "unit": "ns",
            "range": "± 4968328.236158521"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 52954308,
            "unit": "ns",
            "range": "± 5581484.21008424"
          }
        ]
      }
    ]
  }
}