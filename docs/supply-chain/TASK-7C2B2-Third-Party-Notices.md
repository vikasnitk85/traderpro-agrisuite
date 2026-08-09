# Task 7C2B2 production third-party notice inventory

- Status: Exact-text bundle approved by the Project/Legal Owner for the frozen B2 graph
- Evidence date: 2026-08-08
- Source: B2 production SBOM and selected ARM binary graph
- Exact-text evidence: [`TASK-7C2B2-Third-Party-Notices-Exact.md`](TASK-7C2B2-Third-Party-Notices-Exact.md)
- Owner approval received: 2026-08-08
- Approval state: `LEGAL_NOTICE_APPROVAL = APPROVED`
- Approved distribution surface: About / Legal / Third-Party Notices
- SQLite public-domain dedication/provenance: INCLUDE
- Pre-approval exact-bundle SHA-256: `1583ED65450E0B6722CA9A1990FDA63D2890748FBB31EF0A1183F2885DCDC33D`
- Approved exact-bundle SHA-256: `8C2DCA5A6479E72A01DA0E9737AF75AC3689AFAD92DF928426CC30EF5D2DB77E`
- Approval authority: Project/Legal Owner; not external legal counsel advice

This inventory is generated for the actual B2 production runtime graph. It
does not include SQLCipher or OpenSSL because neither is selected or packaged.
The exact component/version inventory is frozen by
`TASK-7C2B2-Mobile-Production-SBOM.cdx.json`.

## Selected storage and security notices

| Component | Version | Technical license source/action |
| --- | --- | --- |
| SQLite3MultipleCiphers | 2.3.6 | MIT; reproduce the exact v2.3.6 `LICENSE`, including Ulrich Telle attribution |
| SQLite | 3.53.3 | Public-domain dedication; include the exact upstream blessing/provenance in the approved notice surface |
| Dart `sqlite3` | 3.5.0 | MIT; reproduce the exact resolved package `LICENSE` |
| `flutter_secure_storage` | 10.3.1 | BSD-3-Clause; reproduce the exact package notice/disclaimer and review the Android implementation contents |
| Tink Android | 1.21.0 | Apache-2.0; reproduce Apache-2.0 and any artifact `NOTICE` content |
| Drift | 2.34.3 | MIT; reproduce the exact resolved package `LICENSE` |
| Flutter engine/embedding | engine revision `a10d8ac38de835021c8d2f920dbf50a920ccc030` | Reproduce Flutter/engine third-party notices supplied with the exact release artifacts |

Complete upstream texts must be copied from the exact resolved packages/AARs;
they must not be reconstructed from this summary.

## Production pub resolution inventory

The conservative Android notice input includes all 56 production-reachable pub
packages because platform tree-shaking must not be used to omit a required
notice without Legal verification:

`args 2.7.0`, `async 2.13.1`, `characters 1.4.1`, `code_assets 1.2.1`,
`collection 1.19.1`, `convert 3.1.2`, `crypto 3.0.7`, `drift 2.34.3`,
`ffi 2.2.0`, `ffi_leak_tracker 0.1.2`, `file 7.0.1`, `fixnum 1.1.1`,
`flutter 0.0.0`, `flutter_secure_storage 10.3.1`,
`flutter_secure_storage_darwin 0.3.2`, `flutter_secure_storage_linux 3.0.2`,
`flutter_secure_storage_platform_interface 2.0.3`,
`flutter_secure_storage_web 2.1.1`, `flutter_secure_storage_windows 4.2.2`,
`flutter_web_plugins 0.0.0`, `glob 2.1.3`, `hooks 2.0.2`, `jni 1.0.2`,
`jni_flutter 1.0.2`, `jni_util 1.0.0`, `logging 1.3.0`,
`material_color_utilities 0.13.0`, `meta 1.18.0`,
`native_toolchain_c 0.19.2`, `objective_c 9.5.0`, `package_config 2.2.0`,
`path 1.9.1`, `path_provider 2.1.6`, `path_provider_android 2.3.1`,
`path_provider_foundation 2.6.0`, `path_provider_linux 2.2.2`,
`path_provider_platform_interface 2.1.3`, `path_provider_windows 2.3.0`,
`platform 3.1.6`, `plugin_platform_interface 2.1.8`, `pub_semver 2.2.0`,
`record_use 0.6.0`, `sky_engine 0.0.0`, `source_span 1.10.2`,
`sqlite3 3.5.0`, `stack_trace 1.12.1`, `stream_channel 2.1.4`,
`string_scanner 1.4.1`, `term_glyph 1.2.2`, `typed_data 1.4.0`,
`uuid 4.6.0`, `vector_math 2.2.0`, `web 1.1.1`, `win32 6.4.0`,
`xdg_directories 1.1.0`, and `yaml 3.1.3`.

## Production Maven resolution inventory

The 52 exact coordinates are recorded in the SBOM. They cover:

- AndroidX activity, annotation, arch-core, collection, concurrent, core,
  customview, exifinterface, fragment, interpolator, lifecycle, loader,
  profileinstaller, savedstate, startup, tracing, versionedparcelable,
  viewpager, and window artifacts;
- Tink Android 1.21.0, Gson 2.13.2, Error Prone annotations 2.41.0,
  JSR-305 3.0.2, Guava `listenablefuture` 1.0, ReLinker 1.4.5, and
  JSpecify 1.0.0;
- Kotlin stdlib 2.3.20 plus resolved JDK/common compatibility artifacts,
  Kotlin coroutines 1.7.1, and JetBrains annotations 23.0.0; and
- Flutter `armeabi_v7a_release`, `arm64_v8a_release`, and
  `flutter_embedding_release` at engine revision
  `a10d8ac38de835021c8d2f920dbf50a920ccc030`.

The linked exact-text evidence bundle maps all 52 resolved coordinates to exact
license text and records the artifact-specific `NOTICE` scan result. The Owner
approved that evidence, its attribution associations, and its SHA-256-based
deduplication on 2026-08-08 for the frozen B2 graph.

## Approval record and future distribution requirement

The Project/Legal Owner approved the complete exact-text bundle on 2026-08-08
against the frozen production SBOM and both ARM binary graphs. The final
product must expose the bundle through About / Legal / Third-Party Notices and
include the SQLite public-domain dedication/provenance. B2 records this future
distribution/release requirement but does not implement that UI.

Any dependency, version, ABI policy, storage engine, Flutter engine revision,
or production binary-graph change requires notice/SBOM regeneration and Owner
review again. This record does not claim external legal counsel advice.

`LEGAL_NOTICE_APPROVAL = APPROVED`
