# Task 7C2B2 mobile production native provenance

- Evidence date: 2026-08-08
- Scope: unsigned ARM release APKs produced from `apps/mobile`
- Selected engine: SQLite3MultipleCiphers 2.3.6 / SQLite 3.53.3
- Production ABIs: `armeabi-v7a`, `arm64-v8a`
- Test-only ABI: `x86_64`

This is B2 production evidence generated from the actual selected build. It
does not replace or rewrite the B1 comparative evidence.

## APK inventory

| ABI | Bytes | SHA-256 | Signing state |
| --- | ---: | --- | --- |
| arm64-v8a | 17,861,928 | `9426dc64c547f518d2c46ce5d5f30d7f2fef2d86958de271912eacb823f1dd26` | Unsigned; `apksigner` reports missing signing manifest |
| armeabi-v7a | 15,070,452 | `be411ba1a88e158f97d6b8da7cbd6eb0bae8f45dcc96810998c063b4b10e82f1` | Unsigned; no repository signing configuration |

The merged release manifest has no `debuggable` attribute, no INTERNET
permission, `allowBackup=false`, `usesCleartextTraffic=false`, and references
both broad backup-rule resources.

## Packaged ELF inventory

All files are stripped (`.symtab` absent). `libapp.so` has no dynamic `NEEDED`
entry. Flutter/JNI dependencies are Android system libraries only.

| ABI | Library | Bytes | SHA-256 | ELF | Dynamic `NEEDED` |
| --- | --- | ---: | --- | --- | --- |
| arm64-v8a | `libapp.so` | 3,539,856 | `f192bcaace51447feddc33def9647aafdfa3e060b7eaf1970f432988e4094be4` | ELF64 / AArch64 | none |
| arm64-v8a | `libdartjni.so` | 131,400 | `9841024a68dae321997aacd6eda858109c648780d0f9c7afa0d218eefe5308bd` | ELF64 / AArch64 | `liblog`, `libm`, `libdl`, `libc` |
| arm64-v8a | `libflutter.so` | 11,580,816 | `bc65ea22533619373528334ecab818f8220f2f1136cd09c44fb141d7377f93b2` | ELF64 / AArch64 | `libc`, `libdl`, `libm`, `libandroid`, `libEGL`, `libGLESv2`, `liblog`, `libjnigraphics` |
| arm64-v8a | `libsqlite3mc.so` | 2,043,872 | `3500da95ebda57b2a749cfaf74258d9bb7b94702f14e435505d302942d869403` | ELF64 / AArch64 | `libm`, `libdl`, `libc` |
| armeabi-v7a | `libapp.so` | 3,949,132 | `f25219a5ef99923937aa78816532150fa3d525d5a66a982e55bc2592ac7bbc77` | ELF32 / ARM | none |
| armeabi-v7a | `libdartjni.so` | 81,600 | `79b2b2bb12a6e78487ed09f44739a388a3b23bde4226e84ec412d06cbd3846d2` | ELF32 / ARM | `liblog`, `libm`, `libdl`, `libc` |
| armeabi-v7a | `libflutter.so` | 8,452,636 | `fadd6d49ebad16d758d52d3e837b59484c072568e157baa7b11a1f387c2f22b3` | ELF32 / ARM | `libc`, `libdl`, `libm`, `libandroid`, `libEGL`, `libGLESv2`, `liblog`, `libjnigraphics` |
| armeabi-v7a | `libsqlite3mc.so` | 2,037,668 | `7e90f4263ae214925f945054a5ec514f5182ee3dedf2576c7bec1142f67040a6` | ELF32 / ARM | `libm`, `libdl`, `libc` |

## SQLite3MC source and version evidence

The checked-in hook selects only:

```yaml
hooks:
  user_defines:
    sqlite3:
      source: sqlite3mc
```

Both selected libraries contain `SQLite3 Multiple Ciphers 2.3.6` and SQLite
`3.53.3` version strings. The upstream signed v2.3.6 release states that it is
based on SQLite 3.53.3 and publishes the Android AAR with SHA-256
`2916964d1c257d0a858c85f8270287f95204332c8e2ca6a1cadab0efacbfc0df`:
[upstream v2.3.6 release](https://github.com/utelle/SQLite3MultipleCiphers/releases/tag/v2.3.6).
The Dart `sqlite3` 3.5.0 hook validates downloaded release assets against its
embedded allowlist before packaging; the final stripped ELF hashes above are
the authoritative shipped hashes.

## Competing-engine and crypto-provider negatives

Each APK has exactly one SQLite native entry: `libsqlite3mc.so`. Neither APK
contains:

- `libsqlite3.so`;
- `libsqlcipher.so`;
- `libcrypto.so` or `libssl.so`;
- another SQLite implementation; or
- an OpenSSL dynamic dependency.

`llvm-strings` finds `sqlcipher` and an SQLite3MC SQLCipher-compatibility
configuration message inside `libsqlite3mc.so`. These are part of the selected
multi-cipher engine, not evidence of a separately packaged SQLCipher engine.
There is no SQLCipher library/component or OpenSSL provider, and production
forces `chacha20`, rejects `cipher_version`, and verifies every frozen setting.

APK files and extracted ELFs remain ignored build artifacts and are not part
of the proposed commit.
