# Task 7C2B1 third-party notice inputs

- Status: Selected-stack technical inventory; formal legal approval outstanding
- Evidence date: 2026-08-08
- Scope: selected TraderPro V1 storage stack plus clearly separated fallback
  candidate evidence

This document identifies technically established notice inputs and
obligations. It is not legal advice, does not replace the complete upstream
license texts, and is not a final production notice bundle. Legal Owner
approval and comparison against the actual B2/release SBOM and shipped binary
graph remain mandatory before distribution.

## Selected V1 stack

ADR-0011 selects SQLite3MultipleCiphers. The following components are the
required B1 notice inputs for the selected storage and secure-store stack:

| Component | Version | License | Notice/copyright source | Required production action | Open legal/release check |
| --- | --- | --- | --- | --- | --- |
| SQLite3MultipleCiphers | 2.3.6 | MIT | [Pinned license](https://github.com/utelle/SQLite3MultipleCiphers/blob/v2.3.6/LICENSE) | Reproduce the complete MIT notice, including `Copyright (c) 2019-2026 Ulrich Telle`. | Confirm whether bundled cipher source carries additional third-party attribution beyond the root license. |
| Dart `sqlite3` wrapper | 3.5.0 | MIT | Resolved package `LICENSE`; [upstream repository](https://github.com/simolus3/sqlite3.dart) | Reproduce the complete MIT copyright and permission text in app/material notices. | Confirm whether the exact build-hook release assets add a tag-specific notice file. |
| SQLite | 3.53.3 | Public domain | [SQLite copyright and blessing](https://www.sqlite.org/copyright.html) | No copyright license notice is required for public-domain SQLite; include provenance/blessing as conservative attribution. | Confirm final legal preference for displaying the blessing. |
| `flutter_secure_storage` | 10.3.1 | BSD-3-Clause | Resolved package `LICENSE`; [upstream repository](https://github.com/juliansteenbakker/flutter_secure_storage) | Reproduce the complete BSD notice and disclaimer, including `Copyright 2017 German Saprykin`; do not use contributor names for endorsement. | Confirm notices for every federated platform package actually shipped. |
| Tink Android | 1.21.0 | Apache-2.0 | Resolved Maven artifact metadata; [Tink repository](https://github.com/tink-crypto/tink) | Provide Apache 2.0 and applicable Tink NOTICE content; retain source attributions. | Legal must review the exact 1.21.0 AAR contents, not only the repository root. |

The draft selected product notice entries therefore use these names and the
complete pinned upstream texts, without paraphrasing their grants:

- `SQLite3MultipleCiphers 2.3.6 — MIT License — Copyright (c) 2019-2026
  Ulrich Telle`;
- `sqlite3.dart 3.5.0 — MIT License — Copyright (c) 2020 Simon Binder`;
- `SQLite 3.53.3 — public-domain dedication`;
- `flutter_secure_storage 10.3.1 — BSD 3-Clause — Copyright 2017 German
  Saprykin`;
- `Tink Android 1.21.0 — Apache License 2.0`; and
- every shipped Maven and Dart transitive component from the production SBOM.

“Complete pinned text” means unmodified license and NOTICE content from the
exact tag or distributed artifact, not text copied from this assessment.

## Relevant selected Android transitive groups

The evidence-only `releaseRuntimeClasspath` and comparative SBOM include
AndroidX, Kotlin, Kotlin coroutines, Gson, Error Prone annotations, JSR-305,
JetBrains annotations, Guava `listenablefuture`, ReLinker, and JSpecify
components. Most use Apache-2.0 or similarly permissive terms, but this
assessment does not collapse them into one legal work. Before distribution:

1. extract license and NOTICE files from each exact Maven artifact in the
   production lock/SBOM;
2. preserve artifact-specific copyrights and NOTICE text;
3. deduplicate identical license bodies without dropping attribution;
4. compare the generated set with the final APK/AAB dependency graph; and
5. obtain Legal Owner approval.

The Tink subtree specifically resolves Gson 2.13.2, Error Prone annotations
2.41.0, JSR-305 3.0.2, and AndroidX annotation-jvm 1.8.2 in the B1 evidence.
They must not be omitted merely because they are transitive. B2 must use the
versions actually resolved by its production graph.

## Dart transitive packages

`TASK-7C2B1-Mobile-SBOM.cdx.json` records all 103 entries from the spike
`pubspec.lock`, including direct, dev, SDK, and transitive packages and every
available pub archive SHA-256. A production notice generator must inspect the
resolved `LICENSE` files for the shipped release subset. The current document
records the security-critical direct packages but does not claim that every
Dart transitive notice has been legally reviewed.

## SQLCipher fallback candidate evidence

SQLCipher and OpenSSL are not production-selected notice inputs. Their B1
provenance and notice work remains below solely to preserve the validated
fallback comparison:

| Component | Version | License | Candidate evidence obligation if reconsidered |
| --- | --- | --- | --- |
| SQLCipher Community | 4.17.0 | BSD-style / BSD-3-Clause | Use the [official SQLCipher license page](https://www.zetetic.net/sqlcipher/license/) and [pinned upstream license](https://github.com/sqlcipher/sqlcipher/blob/v4.17.0/LICENSE.txt); reproduce the complete license and Zetetic copyright and obtain approval for attribution placement. |
| OpenSSL | 3.6.2 | Apache-2.0 | The tested SQLCipher artifact statically includes OpenSSL. Use the [pinned OpenSSL license](https://github.com/openssl/openssl/blob/openssl-3.6.2/LICENSE.txt), retain applicable notices, and complete export/compliance review. Static linkage does not remove notice obligations. |

If SQLCipher is reconsidered, ADR-0011 requires a new ADR and evidence pass
using the then-current native artifact and current OpenSSL security patch. The
resulting exact license and NOTICE inputs must replace, not silently inherit,
the frozen candidate versions above.

## SBOM and approval gate

The Task 7C2B1 CycloneDX SBOM intentionally remains comparative evidence for
both tested candidates. It must not be rewritten to suggest that only
SQLite3MultipleCiphers was tested. Task 7C2B2 must generate a production SBOM
from the actual production dependency graph after the selected packages are
introduced, and the final notice bundle must be generated from that graph.

Formal legal approval has not been established and is not claimed. The B1
technical selection can be recorded, but final production third-party notice
packaging and Legal Owner approval remain Task 7C2B2/release requirements.
Any dependency, engine, SQLite, Tink, ABI, or build-tool change requires the
SBOM and notice set to be regenerated and reviewed.
