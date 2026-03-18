window.BENCHMARK_DATA = {
  "lastUpdate": 1773855442925,
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
      }
    ]
  }
}