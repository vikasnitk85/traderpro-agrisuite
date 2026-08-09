# Task 7C2B2 mobile production SCA review

- Review date: 2026-08-08
- Scope: new B2 production CycloneDX SBOM and selected ARM native artifacts
- Scanner: Anchore Grype 0.116.1, commit
  `30394f177175da63ae36b35cdd809c248eb4de7f`, Syft 1.50.0
- Database: schema v6.1.9, built 2026-08-06T07:04:15Z, valid manual import
- Automatic application/database updates: disabled
- SBOM SHA-256:
  `3588fab97d004563e450b5c4d271017911f3a4149277e401b6a5874ceb543150`
- Transient external Grype JSON SHA-256:
  `6b8b2608a90b6b3a93b78c599c50338b8f652f6ecd0f607f994b2c35d29f264f`

Raw scanner output and the scanner database remain outside the repository.
This file retains only sanitized review evidence.

## Production SBOM coverage

`TASK-7C2B2-Mobile-Production-SBOM.cdx.json` was generated from the current
production-reachable pub graph, Gradle `releaseRuntimeClasspath`, and the two
selected ARM `libsqlite3mc.so` files. Grype accepted the CycloneDX 1.6 input.

| Coverage | Components | Result |
| --- | ---: | --- |
| Pub purls | 56 | Supported ecosystem matcher; zero matches |
| Maven purls | 52 | Supported ecosystem matcher; zero matches |
| Generic SQLite3MC/SQLite purls without CPE | 2 | Manual native/upstream review required |
| ABI native files without purl/CPE | 2 | Manual ELF/provenance review required |
| Total | 112 | 108 ecosystem coordinates plus four manual components |

The SBOM has no SQLCipher or OpenSSL component and no x86 native component.
`x86_64` remains emulator/test-only.

## Grype result

The scan exited 0 with an empty `matches` array. No ignored-match field was
present in the JSON.

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 0 |
| Medium | 0 |
| Low | 0 |
| Negligible/Unknown | 0 |

This is not a claim that unmatched native code is vulnerability-free.

## Manual native review

### SQLite3MultipleCiphers 2.3.6

The selected stripped ELFs identify SQLite3MultipleCiphers 2.3.6 based on
SQLite 3.53.3. The official v2.3.6 release is the latest listed release at the
review date, uses a signed tag, and publishes artifact attestation/hash
evidence. Review found no applicable unresolved Critical/High project advisory.
The exact packaged hashes, dependencies, and competing-engine negatives are in
the B2 native-provenance document.

### SQLite 3.53.3

SQLite's official recent-CVE table records the 2026 arbitrary-SQL/FTS5 issue as
fixed in 3.53.2; the selected 3.53.3 includes that fix:
[SQLite CVE status](https://sqlite.org/cves.html). TraderPro does not accept
arbitrary SQL or imported database files. The 3.53.3 release is recorded by
SQLite here: [SQLite 3.53.3 release](https://www.sqlite.org/releaselog/3_53_3.html).

An upstream forum report describes an operational 3.53.3 hot-journal
regression during crash recovery of multi-database `ATTACH` transactions. The
B2 foundation has one database, does not use `ATTACH`, and configures WAL, so
the reported preconditions are absent. It is not an applicable Critical/High
security finding, but upstream resolution should be monitored before a future
SQLite/SQLite3MC upgrade.

## Conclusion

No unresolved applicable Critical or High vulnerability was found in the
selected B2 production graph. Any dependency, SQLite3MC, SQLite, Flutter
engine, Tink, ABI, or build-hook change requires regeneration and review.
The Project/Legal Owner approved the exact-text notice bundle on 2026-08-08
for this frozen graph; that approval is separate from the SCA conclusion and
must be renewed after a graph change.
