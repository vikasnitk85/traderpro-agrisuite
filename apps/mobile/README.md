# TraderPro AgriSuite mobile

Android-first Flutter client for TraderPro AgriSuite.

The application contains the foundation shell, the shared exact
weight-processing contract, and a focused Drift/SQLite offline-store proof of
concept for local Receiving Sessions and immutable weight facts. Complete
business modules and Receiving UI are not implemented.

## Structure

- `lib/app`: application composition and startup screen
- `lib/core`: cross-feature policies, local database, and sync primitives
- `lib/features/device_activation`: device activation placeholder
- `lib/features/receiving`: local Receiving store spike; no widgets or network
- `lib/infrastructure`: external system and device adapters
- `lib/shared`: shared presentation components

## Validate

```shell
dart run build_runner build
flutter analyze
flutter test
```

The local store uses ordinary, unencrypted SQLite. It is a proof of concept and
is not production-ready for sensitive customer data.
