# Retree Benchmarks

Benchmark results for RetreeCore change detection and event propagation.

## Latest Results

**Date:** 2026-03-21 (BeginDeepTracking fix — correct deep change detection through RetreeList/RetreeDictionary)
**Runtime:** net9.0 | **Iterations:** 100 | **Warmup:** 5

> **Note on deeper change counts:** Prior results had a bug where changes inside `RetreeList`/`RetreeDictionary` items (and their children) were not being propagated correctly — so Total Changes and Mean Depth were artificially low. These results reflect correct deep tracking. The XL/XHigh scenario now detects 661,600 changes (depth 6.16) vs 41,400 (depth 1.02) in prior runs. The extra cost is inherent to correct deep detection across 4834 nodes. For all scenarios up to Large/High the tick times are broadly similar (sub-0.25ms).

### Tick Duration (ms)

| Scenario | Nodes | Ops | Min | Mean | Median | P95 | P99 | Max |
|----------|-------|-----|------|------|--------|------|------|------|
| Small/Low | 8 | 5 | 0.0027 | 0.0034 | 0.0031 | 0.0045 | 0.0056 | 0.0072 |
| Small/Med | 8 | 13 | 0.0032 | 0.0038 | 0.0037 | 0.0050 | 0.0054 | 0.0063 |
| Small/High | 8 | 50 | 0.0034 | 0.0040 | 0.0038 | 0.0051 | 0.0056 | 0.0057 |
| Med/Low | 99 | 10 | 0.0133 | 0.0153 | 0.0145 | 0.0198 | 0.0225 | 0.0325 |
| Med/Med | 99 | 45 | 0.0206 | 0.0223 | 0.0213 | 0.0293 | 0.0327 | 0.0336 |
| Med/High | 99 | 180 | 0.0374 | 0.0423 | 0.0400 | 0.0528 | 0.0613 | 0.0724 |
| Large/Low | 519 | 20 | 0.0582 | 0.0643 | 0.0608 | 0.0779 | 0.0847 | 0.0854 |
| Large/Med | 519 | 100 | 0.0744 | 0.0856 | 0.0829 | 0.1039 | 0.1111 | 0.1136 |
| Large/High | 519 | 650 | 0.1644 | 0.1843 | 0.1821 | 0.2092 | 0.2391 | 0.2437 |
| XL/XHigh | 4834 | 10000 | 3.2895 | 5.4254 | 5.7170 | 6.4773 | 6.6898 | 7.5279 |

### Total Scenario Time (ms) [mutations + tick + emission]

| Scenario | Min | Mean | Median | P95 | P99 | Max |
|----------|------|------|--------|------|------|------|
| Small/Low | 0.0028 | 0.0037 | 0.0034 | 0.0049 | 0.0064 | 0.0079 |
| Small/Med | 0.0078 | 0.0100 | 0.0091 | 0.0147 | 0.0171 | 0.0181 |
| Small/High | 0.0158 | 0.0188 | 0.0177 | 0.0239 | 0.0266 | 0.0281 |
| Med/Low | 0.0139 | 0.0160 | 0.0150 | 0.0206 | 0.0231 | 0.0340 |
| Med/Med | 0.0303 | 0.0330 | 0.0314 | 0.0409 | 0.0437 | 0.0527 |
| Med/High | 0.0786 | 0.0879 | 0.0830 | 0.1090 | 0.1133 | 0.1234 |
| Large/Low | 0.0593 | 0.0658 | 0.0623 | 0.0795 | 0.0883 | 0.0885 |
| Large/Med | 0.1017 | 0.1164 | 0.1130 | 0.1388 | 0.1475 | 0.1547 |
| Large/High | 0.4036 | 0.4603 | 0.4555 | 0.5133 | 0.5651 | 0.5937 |
| XL/XHigh | 3.8982 | 7.3803 | 7.8856 | 8.9903 | 9.8074 | 10.0112 |

### Change Detection Summary

| Scenario | Total Changes | Mean Depth | Mean/Tick |
|----------|--------------|------------|-----------|
| Small/Low | 500 | 1.40 | 5.0 |
| Small/Med | 1200 | 1.33 | 12.0 |
| Small/High | 2400 | 1.83 | 24.0 |
| Med/Low | 1000 | 1.90 | 10.0 |
| Med/Med | 4500 | 3.22 | 45.0 |
| Med/High | 14700 | 3.54 | 147.0 |
| Large/Low | 1900 | 2.95 | 19.0 |
| Large/Med | 8600 | 5.03 | 86.0 |
| Large/High | 60200 | 5.45 | 602.0 |
| XL/XHigh | 661600 | 6.16 | 6616.0 |

## Prior Results

**Date:** 2026-03-21 (unseal RetreeDictionary + add SerializedRetreeDictionary)
**Runtime:** net9.0 | **Iterations:** 100 | **Warmup:** 5

> **Warning — these results were misleading:** Deep changes inside `RetreeList`/`RetreeDictionary` items were silently not detected. Total Changes and Mean Depth are artificially low (XL/XHigh depth 1.02 vs correct 6.16). The tick times look fast because most work was skipped.

### Tick Duration (ms)

| Scenario | Nodes | Ops | Min | Mean | Median | P95 | P99 | Max |
|----------|-------|-----|------|------|--------|------|------|------|
| Small/Low | 8 | 5 | 0.0022 | 0.0026 | 0.0024 | 0.0032 | 0.0043 | 0.0043 |
| Small/Med | 8 | 13 | 0.0025 | 0.0030 | 0.0030 | 0.0038 | 0.0040 | 0.0043 |
| Small/High | 8 | 50 | 0.0025 | 0.0029 | 0.0027 | 0.0037 | 0.0038 | 0.0039 |
| Med/Low | 99 | 10 | 0.0037 | 0.0043 | 0.0041 | 0.0054 | 0.0056 | 0.0062 |
| Med/Med | 99 | 45 | 0.0051 | 0.0058 | 0.0056 | 0.0072 | 0.0076 | 0.0094 |
| Med/High | 99 | 180 | 0.0047 | 0.0054 | 0.0053 | 0.0068 | 0.0072 | 0.0074 |
| Large/Low | 519 | 20 | 0.0056 | 0.0066 | 0.0064 | 0.0080 | 0.0112 | 0.0132 |
| Large/Med | 519 | 100 | 0.0043 | 0.0053 | 0.0050 | 0.0066 | 0.0076 | 0.0144 |
| Large/High | 519 | 650 | 0.0035 | 0.0042 | 0.0040 | 0.0053 | 0.0060 | 0.0095 |
| XL/XHigh | 4834 | 10000 | 0.0087 | 0.0102 | 0.0100 | 0.0127 | 0.0138 | 0.0146 |

### Total Scenario Time (ms) [mutations + tick + emission]

| Scenario | Min | Mean | Median | P95 | P99 | Max |
|----------|------|------|--------|------|------|------|
| Small/Low | 0.0023 | 0.0028 | 0.0027 | 0.0036 | 0.0048 | 0.0049 |
| Small/Med | 0.0064 | 0.0082 | 0.0078 | 0.0109 | 0.0131 | 0.0132 |
| Small/High | 0.0098 | 0.0114 | 0.0109 | 0.0147 | 0.0158 | 0.0168 |
| Med/Low | 0.0041 | 0.0048 | 0.0045 | 0.0061 | 0.0065 | 0.0071 |
| Med/Med | 0.0097 | 0.0116 | 0.0112 | 0.0144 | 0.0162 | 0.0245 |
| Med/High | 0.0240 | 0.0277 | 0.0272 | 0.0336 | 0.0352 | 0.0374 |
| Large/Low | 0.0065 | 0.0080 | 0.0077 | 0.0097 | 0.0136 | 0.0149 |
| Large/Med | 0.0166 | 0.0197 | 0.0188 | 0.0273 | 0.0341 | 0.0352 |
| Large/High | 0.1009 | 0.1199 | 0.1189 | 0.1364 | 0.1424 | 0.1440 |
| XL/XHigh | 0.6634 | 0.7447 | 0.7500 | 0.8160 | 0.8336 | 0.8531 |

### Change Detection Summary

| Scenario | Total Changes | Mean Depth | Mean/Tick |
|----------|--------------|------------|-----------|
| Small/Low | 400 | 0.75 | 4.0 |
| Small/Med | 1000 | 0.90 | 10.0 |
| Small/High | 1500 | 1.07 | 15.0 |
| Med/Low | 700 | 1.14 | 7.0 |
| Med/Med | 2000 | 1.40 | 20.0 |
| Med/High | 3700 | 1.38 | 37.0 |
| Large/Low | 1300 | 1.31 | 13.0 |
| Large/Med | 2100 | 1.33 | 21.0 |
| Large/High | 10200 | 0.99 | 102.0 |
| XL/XHigh | 41400 | 1.02 | 414.0 |

## Older Results

**Date:** 2026-03-07 (netstandard2.0)
**Runtime:** net9.0 | **Iterations:** 100 | **Warmup:** 5

### Tick Duration (ms)

| Scenario | Nodes | Ops | Min | Mean | Median | P95 | P99 | Max |
|----------|-------|-----|------|------|--------|------|------|------|
| Small/Low | 8 | 5 | 0.0021 | 0.0031 | 0.0028 | 0.0046 | 0.0055 | 0.0055 |
| Small/Med | 8 | 13 | 0.0027 | 0.0029 | 0.0029 | 0.0032 | 0.0036 | 0.0037 |
| Small/High | 8 | 50 | 0.0026 | 0.0029 | 0.0029 | 0.0032 | 0.0038 | 0.0039 |
| Med/Low | 99 | 10 | 0.0037 | 0.0042 | 0.0041 | 0.0052 | 0.0060 | 0.0064 |
| Med/Med | 99 | 45 | 0.0052 | 0.0058 | 0.0057 | 0.0065 | 0.0071 | 0.0079 |
| Med/High | 99 | 180 | 0.0050 | 0.0053 | 0.0053 | 0.0060 | 0.0062 | 0.0063 |
| Large/Low | 519 | 20 | 0.0058 | 0.0065 | 0.0063 | 0.0076 | 0.0112 | 0.0123 |
| Large/Med | 519 | 100 | 0.0047 | 0.0051 | 0.0050 | 0.0059 | 0.0066 | 0.0069 |
| Large/High | 519 | 650 | 0.0034 | 0.0037 | 0.0037 | 0.0039 | 0.0042 | 0.0047 |
| XL/XHigh | 4834 | 10000 | 0.0087 | 0.0100 | 0.0097 | 0.0120 | 0.0127 | 0.0161 |

### Total Scenario Time (ms) [mutations + tick + emission]

| Scenario | Min | Mean | Median | P95 | P99 | Max |
|----------|------|------|--------|------|------|------|
| Small/Low | 0.0024 | 0.0036 | 0.0032 | 0.0057 | 0.0064 | 0.0065 |
| Small/Med | 0.0067 | 0.0076 | 0.0073 | 0.0090 | 0.0115 | 0.0115 |
| Small/High | 0.0106 | 0.0118 | 0.0114 | 0.0145 | 0.0157 | 0.0167 |
| Med/Low | 0.0041 | 0.0048 | 0.0045 | 0.0059 | 0.0070 | 0.0075 |
| Med/Med | 0.0099 | 0.0113 | 0.0110 | 0.0138 | 0.0158 | 0.0162 |
| Med/High | 0.0255 | 0.0276 | 0.0270 | 0.0319 | 0.0330 | 0.0335 |
| Large/Low | 0.0068 | 0.0077 | 0.0074 | 0.0093 | 0.0123 | 0.0140 |
| Large/Med | 0.0174 | 0.0193 | 0.0187 | 0.0239 | 0.0250 | 0.0253 |
| Large/High | 0.1014 | 0.1055 | 0.1041 | 0.1122 | 0.1174 | 0.1277 |
| XL/XHigh | 0.6770 | 0.7999 | 0.8217 | 0.9171 | 0.9590 | 0.9770 |

### Change Detection Summary

| Scenario | Total Changes | Mean Depth | Mean/Tick |
|----------|--------------|------------|-----------|
| Small/Low | 400 | 0.75 | 4.0 |
| Small/Med | 1000 | 0.90 | 10.0 |
| Small/High | 1500 | 1.07 | 15.0 |
| Med/Low | 700 | 1.14 | 7.0 |
| Med/Med | 2000 | 1.40 | 20.0 |
| Med/High | 3700 | 1.38 | 37.0 |
| Large/Low | 1300 | 1.31 | 13.0 |
| Large/Med | 2100 | 1.33 | 21.0 |
| Large/High | 10200 | 0.99 | 102.0 |
| XL/XHigh | 41400 | 1.02 | 414.0 |
