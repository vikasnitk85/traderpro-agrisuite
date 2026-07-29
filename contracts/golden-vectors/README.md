# Weight-processing golden vectors

`weight-processing.v1.json` is the cross-platform executable contract for
TraderPro weight processing. The C# and Dart test suites both read this same
repository-level file; neither suite copies the cases into platform-specific
test data.

## Numeric representation

Raw weights are strings because their exact decimal text is a captured physical
fact. Parsing through binary floating point could change the value, and
normalising the text would lose evidence such as leading or trailing zeros.
The processor therefore returns `rawWeightKg` exactly as supplied.

A valid raw value has 1 to 14 ASCII integer digits and, optionally, a dot
followed by 1 to 6 ASCII fractional digits. Signs, whitespace, commas,
scientific notation, and non-numeric values are rejected. Zero is valid for
this pure contract.

Processing uses exact integer arithmetic at six fractional digits:

- **Standard** selects the nearest value at the requested precision. An exact
  midpoint rounds upward for these non-negative values (half-up).
- **Floor** selects the greatest value at the requested precision that is less
  than or equal to the raw value.
- **Ceiling** selects the smallest value at the requested precision that is
  greater than or equal to the raw value. An already exact value is unchanged.

Supported display precisions are exactly 1, 2, and 3 decimal places.
`processedWeightKg` is always canonicalised to exactly six fractional digits
for storage. `displayWeightKg` always has exactly the selected number of
fractional digits.

## Contract evolution and errors

The top-level contract name, version, and storage precision identify the
schema. Each named valid case defines one input policy and its processed and
display representations. Each named invalid case defines the stable error code
that both implementations must expose:

- `WEIGHT_RAW_REQUIRED`
- `WEIGHT_RAW_INVALID`
- `WEIGHT_RAW_NEGATIVE`
- `WEIGHT_RAW_INTEGER_DIGITS_EXCEEDED`
- `WEIGHT_RAW_FRACTION_DIGITS_EXCEEDED`
- `WEIGHT_DECIMAL_PLACES_INVALID`
- `WEIGHT_PROCESSING_METHOD_INVALID`

Changing an existing case or error meaning is a contract-breaking change and
requires a new vector version. Additive cases that clarify the version 1 rule
may be added while preserving all existing outcomes.

The C# loader is in
`services/backend/tests/TraderPro.UnitTests/WeightProcessingGoldenVectorTests.cs`.
The Dart loader is in
`apps/mobile/test/core/measurements/weight_processing_golden_vector_test.dart`.
Run both through `scripts/test/test-weight-processing-parity.ps1`.
