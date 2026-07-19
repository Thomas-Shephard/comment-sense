window.BENCHMARK_DATA = {
  "lastUpdate": 1784457153143,
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
          "id": "afe40665f323a87088c1c6055d3dc53bac349bf2",
          "message": "feat: improve exception type resolution (#115)",
          "timestamp": "2026-03-17T20:41:19Z",
          "tree_id": "1e49674deccd212e00bce5b899395c0e0d674509",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/afe40665f323a87088c1c6055d3dc53bac349bf2"
        },
        "date": 1773780187208,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 4954219,
            "unit": "ns",
            "range": "± 6768947.797430135"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 20979856,
            "unit": "ns",
            "range": "± 17513885.129592236"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 5327438,
            "unit": "ns",
            "range": "± 5000912.330483758"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 8391010,
            "unit": "ns",
            "range": "± 6895269.129400977"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 53210224,
            "unit": "ns",
            "range": "± 8482443.282485629"
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
          "id": "08e6908517366781314044dd77dade074a9c546e",
          "message": "docs: update documentation (#116)",
          "timestamp": "2026-03-17T21:51:52Z",
          "tree_id": "6f04b13fe9c16a3dbeb307ec9cd6aa90b72c2ec2",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/08e6908517366781314044dd77dade074a9c546e"
        },
        "date": 1773784414286,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 4938560,
            "unit": "ns",
            "range": "± 7911229.785348639"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21501240,
            "unit": "ns",
            "range": "± 101763184.9485121"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 5277984,
            "unit": "ns",
            "range": "± 3161716.0168675617"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 8494160,
            "unit": "ns",
            "range": "± 10226415.339417942"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 53326642,
            "unit": "ns",
            "range": "± 4476023.435546616"
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
          "id": "bfdbf93c5f9a79abfc68938d86ac61de5f8ce582",
          "message": "chore: mark rules as shipped for v1.0.0 (#118)",
          "timestamp": "2026-03-18T10:57:25Z",
          "tree_id": "9d04b451648df0df669e8d96a6eaa42b65386d1d",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/bfdbf93c5f9a79abfc68938d86ac61de5f8ce582"
        },
        "date": 1773831549002,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 4938557,
            "unit": "ns",
            "range": "± 4871034.5318621425"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21091480,
            "unit": "ns",
            "range": "± 32026721.771656472"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 5352008,
            "unit": "ns",
            "range": "± 4659918.122659508"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 8358762,
            "unit": "ns",
            "range": "± 14840498.54855221"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 53060208,
            "unit": "ns",
            "range": "± 4388628.971962099"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "35642c85816faf459d58e7d01befacde81d28b27",
          "message": "chore: Bump coverlet.collector from 8.0.0 to 8.0.1 (#117)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-03-18T11:00:38Z",
          "tree_id": "da277b81ee361fe43d2f6c60f823a11b8b2b7b0f",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/35642c85816faf459d58e7d01befacde81d28b27"
        },
        "date": 1773831746334,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 4974893,
            "unit": "ns",
            "range": "± 3739300.2038308866"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21007464,
            "unit": "ns",
            "range": "± 41159272.27823285"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 5278746,
            "unit": "ns",
            "range": "± 4236023.098498301"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 8649300,
            "unit": "ns",
            "range": "± 5174995.896202472"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 53388034,
            "unit": "ns",
            "range": "± 3172000.270478518"
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
          "id": "abcc1952da0fa08c988e4267f492043f37e4bea8",
          "message": "chore: assign for dependabot notifications (#119)",
          "timestamp": "2026-03-18T17:35:37Z",
          "tree_id": "bee1988565a74abe4172bdf3ef824147311fe428",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/abcc1952da0fa08c988e4267f492043f37e4bea8"
        },
        "date": 1773855442641,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 4900222,
            "unit": "ns",
            "range": "± 5302277.253235807"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21009600,
            "unit": "ns",
            "range": "± 87649218.21677361"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 5281586,
            "unit": "ns",
            "range": "± 3342598.013614882"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 8254794,
            "unit": "ns",
            "range": "± 6765371.888808034"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 52900578,
            "unit": "ns",
            "range": "± 7136023.175960708"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4f76f99364e8347b9816064f1d2306fb79eff9e9",
          "message": "chore: Bump NUnit3TestAdapter from 6.1.0 to 6.2.0 (#121)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-03-23T07:54:44Z",
          "tree_id": "74940b8e431c54827d40af26d9fc8ffb3b222c47",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/4f76f99364e8347b9816064f1d2306fb79eff9e9"
        },
        "date": 1774252596209,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 4966221,
            "unit": "ns",
            "range": "± 8203624.202358844"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21079904,
            "unit": "ns",
            "range": "± 35877901.41701097"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 5330902,
            "unit": "ns",
            "range": "± 3741746.337334631"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 9117184,
            "unit": "ns",
            "range": "± 5519864.8714671545"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 53403202,
            "unit": "ns",
            "range": "± 4188395.30926987"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "9e4ed6665ddca86cee10f6714d80b27f07bd8fd4",
          "message": "chore: Bump benchmark-action/github-action-benchmark from 1.21.0 to 1.22.0 (#122)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-03-31T15:27:59+01:00",
          "tree_id": "b9b33393318e0d4edcc778f1edd97ee1301a882d",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/9e4ed6665ddca86cee10f6714d80b27f07bd8fd4"
        },
        "date": 1774967392720,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 4953981,
            "unit": "ns",
            "range": "± 8134549.262255744"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 20847960,
            "unit": "ns",
            "range": "± 31900477.463084113"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 5260061,
            "unit": "ns",
            "range": "± 3029070.058768763"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 8470589,
            "unit": "ns",
            "range": "± 8642189.47730737"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 52992759,
            "unit": "ns",
            "range": "± 3574407.5562096173"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "1c72e5a10dd09a40a63e5ae3770d0d82ec0701be",
          "message": "chore: Bump coverlet.collector and fix code coverage (#127)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>\nCo-authored-by: Thomas Shephard <thomas@thomas-shephard.com>",
          "timestamp": "2026-04-25T20:27:41+01:00",
          "tree_id": "70e45ca37615e3658780c88b91380bc24902899a",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/1c72e5a10dd09a40a63e5ae3770d0d82ec0701be"
        },
        "date": 1777145364828,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 4983955,
            "unit": "ns",
            "range": "± 7881042.314125535"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21242784,
            "unit": "ns",
            "range": "± 102548207.42068139"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 5349653,
            "unit": "ns",
            "range": "± 4624243.177856483"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 8417992,
            "unit": "ns",
            "range": "± 11376599.01541581"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 53243798,
            "unit": "ns",
            "range": "± 8622046.041768737"
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
          "id": "8d2e3c07b664a18936f53f135211e1082d3ef12b",
          "message": "test: add unit test for GhostReferenceAnalyzer regex cache (#129)",
          "timestamp": "2026-04-25T22:37:14+01:00",
          "tree_id": "d7c65fd3e41931a08b3d78682f3f3359aeedfb8b",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/8d2e3c07b664a18936f53f135211e1082d3ef12b"
        },
        "date": 1777153135688,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 4980472,
            "unit": "ns",
            "range": "± 7303710.528165725"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21165448,
            "unit": "ns",
            "range": "± 58503558.727659"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 5305356,
            "unit": "ns",
            "range": "± 4038167.2931087227"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 8102850,
            "unit": "ns",
            "range": "± 21502189.017688323"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 53334538,
            "unit": "ns",
            "range": "± 16136286.449802041"
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
          "id": "f64a3a9b3efadd757e4f162b50b9131994288bd7",
          "message": "refactor: move analyzer documentation processing off XElement (#128)",
          "timestamp": "2026-04-26T18:29:00+01:00",
          "tree_id": "9f9930709d86647dfd7273296d9dca0456414825",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/f64a3a9b3efadd757e4f162b50b9131994288bd7"
        },
        "date": 1777224651021,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3608867,
            "unit": "ns",
            "range": "± 10467203.171056014"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21601224,
            "unit": "ns",
            "range": "± 27220039.862923145"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4566948,
            "unit": "ns",
            "range": "± 84972.37354645653"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5849643,
            "unit": "ns",
            "range": "± 28407984.435820248"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30412000,
            "unit": "ns",
            "range": "± 1038711.7884549154"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "fc2163d1052ea2365e31da1ff41f228d6121248e",
          "message": "chore: Bump nuget/login from 1.1.0 to 1.2.0 (#130)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-04-27T17:51:40+01:00",
          "tree_id": "c70869ea4945cbb4d5edf8246afb036b7bdf3524",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/fc2163d1052ea2365e31da1ff41f228d6121248e"
        },
        "date": 1777308800358,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3599432,
            "unit": "ns",
            "range": "± 8346511.82371622"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21593208,
            "unit": "ns",
            "range": "± 26635997.6644963"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4655965,
            "unit": "ns",
            "range": "± 4322096.1042059455"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5750828,
            "unit": "ns",
            "range": "± 17028457.621992975"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30490954,
            "unit": "ns",
            "range": "± 8971217.158552565"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4bc058c0a2564237d20f6cce944c644df37c8dd0",
          "message": "chore: Bump Microsoft.NET.Test.Sdk from 18.4.0 to 18.5.1 (#131)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-04-29T09:44:01+01:00",
          "tree_id": "634d812610850e461f686c37d01c0d572b41b27b",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/4bc058c0a2564237d20f6cce944c644df37c8dd0"
        },
        "date": 1777452342391,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3576128,
            "unit": "ns",
            "range": "± 4899499.838056579"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 20785476,
            "unit": "ns",
            "range": "± 29383529.707481164"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4575472,
            "unit": "ns",
            "range": "± 92628.23679901361"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5805551,
            "unit": "ns",
            "range": "± 4929748.273918292"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30401749,
            "unit": "ns",
            "range": "± 2201526.4450501706"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "e97da2b9f47240f68a58aa10cc050a5af79434a7",
          "message": "chore: Bump NUnit from 4.5.1 to 4.6.0 (#132)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-05-04T09:29:16+01:00",
          "tree_id": "9e697129805fa967a3a045bf1772cd5eed69788e",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/e97da2b9f47240f68a58aa10cc050a5af79434a7"
        },
        "date": 1777883471681,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3628581,
            "unit": "ns",
            "range": "± 6212818.465572881"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21442632,
            "unit": "ns",
            "range": "± 37443632.41884868"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4559806,
            "unit": "ns",
            "range": "± 95507.41094851062"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5610811,
            "unit": "ns",
            "range": "± 17669849.36343675"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30418542,
            "unit": "ns",
            "range": "± 4309123.859069648"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "62d35316eec5aa581cdf692b3d95c66e18d7b069",
          "message": "chore: Bump NUnit.Analyzers from 4.12.0 to 4.13.0 (#133)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-05-04T09:44:12+01:00",
          "tree_id": "5490dca3ad188c82fcddd80da62686de30357267",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/62d35316eec5aa581cdf692b3d95c66e18d7b069"
        },
        "date": 1777884360880,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3555867,
            "unit": "ns",
            "range": "± 7254910.944666266"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21539232,
            "unit": "ns",
            "range": "± 122488642.21350054"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4833537,
            "unit": "ns",
            "range": "± 2214264.088823115"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5657948,
            "unit": "ns",
            "range": "± 5801647.255707694"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30429428,
            "unit": "ns",
            "range": "± 6692362.135018224"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "d0f80a2f620116f9c96d903bda40836335fdebe2",
          "message": "chore: Bump benchmark-action/github-action-benchmark from 1.22.0 to 1.22.1 (#134)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-05-07T19:21:51+01:00",
          "tree_id": "5a60448ac50e5bcff71b7f43c50254fe3fd1cc21",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/d0f80a2f620116f9c96d903bda40836335fdebe2"
        },
        "date": 1778178232658,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3660488,
            "unit": "ns",
            "range": "± 3153647.445185662"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21541600,
            "unit": "ns",
            "range": "± 14285382.640433624"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4576822,
            "unit": "ns",
            "range": "± 95895.31163734586"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5811752,
            "unit": "ns",
            "range": "± 6657991.273609379"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30719412,
            "unit": "ns",
            "range": "± 7091947.306283289"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "ed04024884bcd73305f7eb7a48fc4995f2f73cae",
          "message": "chore: Bump NUnit from 4.6.0 to 4.6.1 (#136)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-05-19T22:49:08+01:00",
          "tree_id": "a37f81297ffcc6066ad0cdd9171349a2ba7058d3",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/ed04024884bcd73305f7eb7a48fc4995f2f73cae"
        },
        "date": 1779227455597,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3579819,
            "unit": "ns",
            "range": "± 9803395.41691488"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21949848,
            "unit": "ns",
            "range": "± 35281098.20429039"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4670067,
            "unit": "ns",
            "range": "± 3233576.976008045"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5855218,
            "unit": "ns",
            "range": "± 24521961.210470423"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30560618,
            "unit": "ns",
            "range": "± 18879566.00547531"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "aafebc74835c023ec2e62124099a3e02b2e209be",
          "message": "chore: Bump Microsoft.NET.Test.Sdk from 18.5.1 to 18.6.0 (#138)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-05-27T16:19:55+01:00",
          "tree_id": "511ae86c8b1789550d4caa2c65d98012582a8898",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/aafebc74835c023ec2e62124099a3e02b2e209be"
        },
        "date": 1779895311111,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3582171,
            "unit": "ns",
            "range": "± 3917609.9115041867"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21302608,
            "unit": "ns",
            "range": "± 101960708.40375568"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4587881,
            "unit": "ns",
            "range": "± 73469.22232223692"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5391648,
            "unit": "ns",
            "range": "± 21661290.050865028"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30347739,
            "unit": "ns",
            "range": "± 3718771.5349003603"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "12230b9db1c0e41152502938acc283805fdaab1b",
          "message": "chore: Bump NUnit.Analyzers from 4.13.0 to 4.14.0 (#139)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-06-11T13:18:33+01:00",
          "tree_id": "372f6ecb77ce2c85e6c6fd17faadc022d85d51a5",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/12230b9db1c0e41152502938acc283805fdaab1b"
        },
        "date": 1781180429132,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3519418,
            "unit": "ns",
            "range": "± 3639396.0120060784"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21319656,
            "unit": "ns",
            "range": "± 122984364.83816354"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4602745,
            "unit": "ns",
            "range": "± 33120.93201596089"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5522550,
            "unit": "ns",
            "range": "± 28224276.04690354"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30412578,
            "unit": "ns",
            "range": "± 2468868.8142388784"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "f045b48c2303c078528c7767451f86862c441d31",
          "message": "chore: Bump Microsoft.NET.Test.Sdk from 18.6.0 to 18.7.0 (#141)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-07-03T13:15:09+01:00",
          "tree_id": "b838323f4c9bd348f96492f9bda95bb2ce42494b",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/f045b48c2303c078528c7767451f86862c441d31"
        },
        "date": 1783081008456,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3559512,
            "unit": "ns",
            "range": "± 11359929.323149735"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21432832,
            "unit": "ns",
            "range": "± 68021926.31484096"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4727442,
            "unit": "ns",
            "range": "± 2507259.617877223"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5652076,
            "unit": "ns",
            "range": "± 6951899.810727006"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30451740,
            "unit": "ns",
            "range": "± 3672491.2884113505"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "2a390a151f14e75e129e3d92e0cf75a46d7ce5a1",
          "message": "chore: Bump actions/checkout from 6 to 7 (#140)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-07-03T12:18:03Z",
          "tree_id": "f2cc69a49ebf6eec70ce85661376850b4ccc6e3e",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/2a390a151f14e75e129e3d92e0cf75a46d7ce5a1"
        },
        "date": 1783081179469,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3540008,
            "unit": "ns",
            "range": "± 5993732.367101324"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 20813316,
            "unit": "ns",
            "range": "± 38755186.356266744"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4591728,
            "unit": "ns",
            "range": "± 295196.090526786"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5585733,
            "unit": "ns",
            "range": "± 6637295.445565929"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30323469,
            "unit": "ns",
            "range": "± 2571754.387521638"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4877203ee13916099a22765ea0fbfb8fd3f3bc45",
          "message": "chore: Bump actions/setup-dotnet from 5 to 6 (#144)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-07-17T14:27:56+01:00",
          "tree_id": "f2de99714ba7417f8d0eea36b6f56dae0392be9d",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/4877203ee13916099a22765ea0fbfb8fd3f3bc45"
        },
        "date": 1784294986392,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3586589,
            "unit": "ns",
            "range": "± 8673965.871813912"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21349496,
            "unit": "ns",
            "range": "± 24860425.85329969"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4625204,
            "unit": "ns",
            "range": "± 111904.73586772961"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 6353524,
            "unit": "ns",
            "range": "± 13523390.563050859"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30256107,
            "unit": "ns",
            "range": "± 3665411.5483740303"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "f746052937f221a2b06beb7d12153b157c3d725d",
          "message": "chore: Bump Microsoft.NET.Test.Sdk from 18.7.0 to 18.8.1 (#143)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-07-17T13:30:44Z",
          "tree_id": "4429a9483694abd699636282f47f05ff186333eb",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/f746052937f221a2b06beb7d12153b157c3d725d"
        },
        "date": 1784295155801,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3467269,
            "unit": "ns",
            "range": "± 5741196.930584267"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21242872,
            "unit": "ns",
            "range": "± 21871829.2726039"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4569645,
            "unit": "ns",
            "range": "± 143544.6518785552"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5824262,
            "unit": "ns",
            "range": "± 6453102.709418008"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30491051,
            "unit": "ns",
            "range": "± 2345215.7921818844"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "60526bb67f36afe267ca9e8eedb6b693fe72f1ed",
          "message": "chore: Bump marocchino/sticky-pull-request-comment from 3.0.4 to 3.0.5 (#142)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-07-17T15:05:40+01:00",
          "tree_id": "c84613cfb8708512dd67d2b5f5cb3b96e1405f0b",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/60526bb67f36afe267ca9e8eedb6b693fe72f1ed"
        },
        "date": 1784297238935,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3436077,
            "unit": "ns",
            "range": "± 2079598.9542660448"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21593128,
            "unit": "ns",
            "range": "± 69166263.47901553"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4723986,
            "unit": "ns",
            "range": "± 3538803.5700041656"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5626934,
            "unit": "ns",
            "range": "± 12926698.665136237"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30235125,
            "unit": "ns",
            "range": "± 5867307.966633938"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "da31914529c4317cca7800796e0bb3f229136d4b",
          "message": "chore: Bump Microsoft.CodeAnalysis.CSharp Analyzer.Testing and CodeFix.Testing from 1.1.3 to 1.1.4 (#137)\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>\nCo-authored-by: Thomas Shephard <thomas@thomas-shephard.com>",
          "timestamp": "2026-07-19T10:59:00+01:00",
          "tree_id": "ed485f57ca500b299bc94b16078c90e96514a316",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/da31914529c4317cca7800796e0bb3f229136d4b"
        },
        "date": 1784455245231,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3554280,
            "unit": "ns",
            "range": "± 11040861.77333814"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21263232,
            "unit": "ns",
            "range": "± 28939228.690918736"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4654327,
            "unit": "ns",
            "range": "± 3493324.882583534"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5742955,
            "unit": "ns",
            "range": "± 6799709.853783756"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30481554,
            "unit": "ns",
            "range": "± 3703959.857506477"
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
          "id": "123d26f79be072c9bf1b81be5938c30aaa93d81f",
          "message": "refactor: remove unused analyzer guard (#145)",
          "timestamp": "2026-07-19T11:11:48+01:00",
          "tree_id": "3d300bb3a92d38adaaec53a38fbe5b149d8e574b",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/123d26f79be072c9bf1b81be5938c30aaa93d81f"
        },
        "date": 1784456019488,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3600355,
            "unit": "ns",
            "range": "± 6941927.288519896"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21349960,
            "unit": "ns",
            "range": "± 40021589.37823544"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4569474,
            "unit": "ns",
            "range": "± 78891.11623309233"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5754082,
            "unit": "ns",
            "range": "± 5181573.165152613"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30323226,
            "unit": "ns",
            "range": "± 5203849.351406285"
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
          "id": "a6b78d222a19de64ca4465f0715d0bc22287c783",
          "message": "test: consolidate XML extension tests (#146)",
          "timestamp": "2026-07-19T11:17:26+01:00",
          "tree_id": "98c221a693e45100fb332e09e8044995ab51e41f",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/a6b78d222a19de64ca4465f0715d0bc22287c783"
        },
        "date": 1784456351761,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3430170,
            "unit": "ns",
            "range": "± 353825.78119164915"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 20561452,
            "unit": "ns",
            "range": "± 28476653.713429715"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4583894,
            "unit": "ns",
            "range": "± 241758.32232105496"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5599101,
            "unit": "ns",
            "range": "± 3638280.967662968"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30756528,
            "unit": "ns",
            "range": "± 190282.66122895433"
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
          "id": "029770764bd9819f4099c8f27ba4882ce1f89310",
          "message": "refactor: avoid documentation trivia set allocation (#148)",
          "timestamp": "2026-07-19T11:30:58+01:00",
          "tree_id": "8f441bf5cb4d9f48f2e5027a5dd94efae68cd740",
          "url": "https://github.com/Thomas-Shephard/comment-sense/commit/029770764bd9819f4099c8f27ba4882ce1f89310"
        },
        "date": 1784457152937,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "CommentSense.PerformanceTests.AnalyzerBenchmarks.FullAnalysis(MethodCount: 100, ScanCalledMethods: True, GhostReferenceMode: \"strict\", SimilarityThreshold: 0.8)",
            "value": 3479819,
            "unit": "ns",
            "range": "± 8436033.488250962"
          },
          {
            "name": "CommentSense.PerformanceTests.DogfoodBenchmarks.AnalyzeProject",
            "value": 21143064,
            "unit": "ns",
            "range": "± 53267038.60001795"
          },
          {
            "name": "CommentSense.PerformanceTests.LeakBenchmarks.SimulateLongSession",
            "value": 4708644,
            "unit": "ns",
            "range": "± 4215554.063440966"
          },
          {
            "name": "CommentSense.PerformanceTests.ParallelBenchmarks.ConcurrentAnalysis(FileCount: 100)",
            "value": 5763918,
            "unit": "ns",
            "range": "± 10168931.8295996"
          },
          {
            "name": "CommentSense.PerformanceTests.PathologicalBenchmarks.AnalyzePathologicalDocs(DocSizeMultiplier: 10)",
            "value": 30287668,
            "unit": "ns",
            "range": "± 8602127.067219654"
          }
        ]
      }
    ]
  }
}