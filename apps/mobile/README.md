# TraderPro AgriSuite mobile

Android-first Flutter client for TraderPro AgriSuite.

The application currently contains only the foundation shell and feature
placeholders. Business modules are not implemented.

## Structure

- `lib/app`: application composition and startup screen
- `lib/core`: cross-feature domain primitives and policies
- `lib/features/device_activation`: device activation placeholder
- `lib/features/receiving`: receiving placeholder
- `lib/infrastructure`: external system and device adapters
- `lib/shared`: shared presentation components

## Validate

```shell
flutter analyze
flutter test
```
