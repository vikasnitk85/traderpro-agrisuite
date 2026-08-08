# Task 7C2B1 mobile SCA review

- Review date: 2026-08-07
- Scope: frozen Task 7C2B1 CycloneDX SBOM and the two evidence-only Android
  encrypted-storage candidates
- Scanner: Anchore Grype 0.116.1, commit
  `30394f177175da63ae36b35cdd809c248eb4de7f`, Syft 1.50.0
- Database: schema v6.1.9, built 2026-08-06T07:04:15Z, valid manual
  import
- Automatic Grype application and database updates: disabled for the scan
- SBOM SHA-256:
  `2E9144D23D29ECCC2C21458D616E35779E328A0849778FC72A5CA11889D8F7A9`
- Reviewed transient Grype JSON SHA-256:
  `7474722BE1CC022752D837C07EEA2EB326E447D0E6E237FCBBBB0BD619CF2C6A`

The raw scanner JSON and database are deliberately outside the repository and
are not part of the commit set. This document is the reviewed, sanitized
evidence summary.

## Database verification

Only the user-approved archive was downloaded:

| Field | Verified value |
| --- | --- |
| Archive | `vulnerability-db_v6.1.9_2026-08-06T00:32:51Z_1785999855.tar.zst` |
| Official URL | `https://grype.anchore.io/databases/v6/vulnerability-db_v6.1.9_2026-08-06T00:32:51Z_1785999855.tar.zst` |
| Compressed bytes | 139,968,909 |
| SHA-256 | `c58fe02aaaaae4c10bcce9a77c3d8affd6cfd66ea4ab0a1510039594da17c914` |
| Imported database bytes | 1,987,256,320 |
| Import metadata bytes | 97 |
| Active cache bytes | 1,987,256,417 |
| Import digest | `xxh64:140f69a39af552a7` |

The compressed size and SHA-256 matched before import. Grype reported the
active database as schema `v6.1.9`, built `2026-08-06T07:04:15Z`, source
`manual import`, and `valid: true`. The active cache is under
`%LOCALAPPDATA%\TraderProTools\grype-cache\db`; no scanner cache was written
to this repository.

## Scan coverage

The frozen CycloneDX 1.6 SBOM contains 166 top-level components: 160 libraries
and six ABI-specific files. It contains 160 package URLs: 103 Dart Pub, 53
Maven, and four generic native-library coordinates. Grype accepted the SBOM
without parse or catalog errors and processed all 160 package-URL components
as match candidates. Its JSON presenter does not emit a separate recognized
package count when there are no matches.

The 103 Pub and 53 Maven coordinates, 156 components total, have dedicated
ecosystem matching against GitHub Security Advisories. The four generic
native-library coordinates have no CPE. The six ABI-specific file components
have neither a PURL nor a CPE. Consequently, those ten components cannot be
represented as reliably vulnerability-scanned. Anchore documents that Pub and
Maven are supported ecosystems and that other packages need CPE metadata for
the NVD fallback: [supported package ecosystems](https://oss.anchore.com/docs/guides/vulnerability/ecosystems/).

| Coverage class | Components | Result |
| --- | ---: | --- |
| Pub ecosystem coordinates | 103 | Presented to a supported matcher; zero matches |
| Maven ecosystem coordinates | 53 | Presented to a supported matcher; zero matches |
| Generic native logical components | 4 | No reliable ecosystem/CPE match; manual review required |
| ABI-specific native files | 6 | No PURL/CPE match; manual review required |
| Total | 166 | 156 ecosystem-matchable; 10 manually reviewed |

No component in the frozen SBOM carries a CPE. A zero Grype match is therefore
not represented as proof that the native components are vulnerability-free.

## Grype results

The frozen command used Grype's SBOM input mode and disabled both update
paths. It exited 0 and produced a JSON `matches` array of length zero.

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 0 |
| Medium | 0 |
| Low | 0 |
| Negligible / Unknown | 0 |

There is therefore no Grype vulnerability ID, affected package, installed or
fixed version, match type, confidence, or candidate-specific packaging
classification to report. There were zero SQLCipher-only, zero
SQLite3MC-only, and zero shared scanner matches. Manual native review below is
separate and controls the real conclusion for the unmatched components.

## Components not reliably matched

| Logical component | Exact version | Packaged candidate | ABI files covered by manual review |
| --- | --- | --- | --- |
| SQLCipher Community | 4.17.0 | SQLCipher only | `libsqlcipher.so` for arm64-v8a, armeabi-v7a, x86_64 |
| OpenSSL | 3.6.2 | Statically linked into SQLCipher only | Same three `libsqlcipher.so` files |
| SQLite3MultipleCiphers | 2.3.6 | SQLite3MC only | `libsqlite3mc.so` for arm64-v8a, armeabi-v7a, x86_64 |
| SQLite | 3.53.3 | Both candidates | Embedded in the corresponding three ABI libraries |

The Dart `sqlite3` 3.5.0, `flutter_secure_storage` 10.3.1, and Android Tink
1.21.0 package coordinates were ecosystem-matchable, but they were also
checked manually because they are security-sensitive.

## Manual upstream advisory review

Official release and repository advisory sources were checked for every frozen
security-sensitive component:

| Component | Manual finding | Remediation / update implication |
| --- | --- | --- |
| SQLCipher Community 4.17.0 | No published GitHub repository security advisory was present. Release 4.17.0 embeds SQLite 3.53.3. | Continue upstream advisory monitoring; do not infer absence of undisclosed issues. |
| OpenSSL 3.6.2 | Eighteen published OpenSSL advisories affect the version: one High, five Moderate, and twelve Low. The 17 issues published 2026-06-09 are fixed in 3.6.3; CVE-2026-54876, published 2026-08-05, is fixed in 3.6.4. | Prefer a future reviewed SQLCipher artifact with OpenSSL 3.6.4 or later; changing the native artifact requires the full storage regression. |
| SQLite3MultipleCiphers 2.3.6 | No published GitHub repository security advisory was present. The exact release is based on SQLite 3.53.3. | Later releases exist, but no security fix was identified as the reason to upgrade this frozen candidate. |
| SQLite 3.53.3 | SQLite 3.53.4 was released 2026-07-24 with unspecified problem fixes; no official security designation for the delta was found. | Assess routine uptake through the native candidates; do not call it a security fix without upstream evidence. |
| Dart `sqlite3` 3.5.0 | No published repository security advisory was present. Version 3.5.1 exists, with no identified security fix in its release evidence. | Routine dependency review only. |
| `flutter_secure_storage` 10.3.1 | No published repository security advisory was present. The clean-install behavior is a configuration/bootstrap defect examined separately, not an advisory match. | Keep 10.3.1 pinned for B1 because a safe empty-namespace configuration was demonstrated; do not upgrade silently. |
| Tink Android 1.21.0 | No published `tink-java` repository security advisory was present for the pinned release. | Continue normal upstream monitoring; later versions alone do not establish a security defect here. |

Sources: [OpenSSL 3.6 advisories](https://openssl-library.org/news/vulnerabilities-3.6/),
[SQLCipher 4.17.0 release](https://github.com/sqlcipher/sqlcipher/releases/tag/v4.17.0),
[SQLCipher advisories](https://github.com/sqlcipher/sqlcipher/security/advisories),
[SQLite3MC 2.3.6 release](https://github.com/utelle/SQLite3MultipleCiphers/releases/tag/v2.3.6),
[SQLite3MC advisories](https://github.com/utelle/SQLite3MultipleCiphers/security/advisories),
[SQLite release history](https://sqlite.org/changes.html),
[`sqlite3.dart` advisories](https://github.com/simolus3/sqlite3.dart/security/advisories),
[`flutter_secure_storage` advisories](https://github.com/juliansteenbakker/flutter_secure_storage/security/advisories),
and [Tink Java advisories](https://github.com/tink-crypto/tink-java/security/advisories).

## OpenSSL applicability review

OpenSSL 3.6.2 is physically shipped in the SQLCipher candidate, so its
advisories cannot be classified as not shipped. The exact SQLCipher 4.17.0
`src/crypto_openssl.c` provider was inspected. Its reachable provider surface
uses OpenSSL random generation, EVP AES-256-CBC streaming, HMAC-SHA1/256/512,
and PBKDF2. It does not invoke PKCS7/S-MIME, CMS, QUIC, TLS/OCSP, CMP/CRMF,
PKCS#12, attacker-controlled ASN.1 decoding, finite-field DH, AES-OCB, or
AES-SIV/GCM-SIV operations implicated by these advisories.

| Severity | Advisory IDs | B1 classification |
| --- | --- | --- |
| High | CVE-2026-45447 | Shipped library, affected PKCS7 verification API not reachable from the SQLCipher provider; `not reachable`, high confidence from exact upstream call-surface inspection. |
| Moderate | CVE-2026-34182, CVE-2026-34183, CVE-2026-35188, CVE-2026-42764, CVE-2026-45445 | Shipped library, affected CMS/QUIC/OCSP/AES-OCB APIs not reachable from the provider; `not reachable`. |
| Low | CVE-2026-34180, CVE-2026-34181, CVE-2026-42765, CVE-2026-42766, CVE-2026-42767, CVE-2026-42768, CVE-2026-42769, CVE-2026-42770, CVE-2026-45446, CVE-2026-7383, CVE-2026-9076, CVE-2026-54876 | Shipped library, affected ASN.1/PKCS#12/OCSP/CMS/CMP/CRMF/DH/SIV/TLS APIs not reachable from the provider; `not reachable`. |

This is a reachability classification for the frozen SQLCipher provider, not
a statement that OpenSSL 3.6.2 itself is vulnerability-free. There is no
unresolved applicable Critical or High finding in the reviewed packaged call
surface. OpenSSL 3.6.4 is nevertheless the minimum version that resolves all
official issues known at the evidence date, so the native update remains a
tracked hardening action before a production foundation is merged.

## Conclusion

The SCA gate is complete for the frozen B1 evidence: Grype found no ecosystem
matches, all ten components outside reliable matching were manually reviewed,
and the one High advisory in a shipped library was classified not reachable
against the exact provider source. This review did not select an engine by
itself; ADR-0011 subsequently selected SQLite3MultipleCiphers using the full
runtime, security, and supply-chain comparison. The comparative SBOM and
SQLCipher/OpenSSL evidence remain intact. This result does not waive the
separate legal or production-foundation gates.
