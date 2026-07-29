# TPTECH-001.13 — Weight-Processing Contract

## Status and purpose

This specification defines TraderPro's version 1 pure weight-processing rule.
It gives the Flutter mobile application and .NET backend one deterministic
interpretation of a captured scale value. It does not define how a scale value
is acquired, persisted, synchronised, or used by a receiving workflow.

The rule preserves two distinct facts:

- **Raw weight** is the original decimal string captured from the scale. It is
  returned unchanged, including leading and trailing zeros.
- **Processed weight** is the exact value produced by the capture-time
  processing policy. Later operator display and business calculations use this
  value rather than silently reprocessing or rewriting the raw fact.

## Input policy

`rawWeightKg` is an unsigned, non-negative decimal string:

- the integer part contains 1 to 14 ASCII digits;
- the optional fractional part uses a dot followed by 1 to 6 ASCII digits;
- signs, commas, exponent notation, whitespace, alphabetic tokens, `NaN`, and
  `Infinity` are not accepted;
- zero is valid for this pure function;
- leading and trailing zeros are accepted and preserved in the raw result.

Examples of accepted syntax are `0`, `50`, `0.0005`, `00050.237000`, and
`99999999999999.999999`. Examples of rejected syntax are `-1`, `+50.2`,
`50,2`, `.5`, `50.`, `5e1`, `NaN`, and strings with surrounding whitespace.

The policy selects exactly 1, 2, or 3 decimal places and exactly one of the
case-sensitive method names `Standard`, `Floor`, or `Ceiling`.

## Exact processing semantics

Processing is performed as exact six-decimal scaled-integer arithmetic. Binary
floating point is never used.

- **Standard** returns the nearest representable value at the selected
  precision. When the raw value is exactly halfway between two values, the
  higher value is selected. Because all valid inputs are non-negative, this is
  half-up rounding.
- **Floor** returns the greatest representable value at the selected precision
  that is less than or equal to the raw value.
- **Ceiling** returns the smallest representable value at the selected
  precision that is greater than or equal to the raw value. If the raw value is
  already exact at the selected precision, it is not increased.

Processing may carry into a new integer digit. For example, Standard processing
of `99.999` at two decimal places produces `100.00`.

## Result contract

Every successful result is immutable and exposes:

| Property | Meaning |
| --- | --- |
| `RawWeightKg` / `rawWeightKg` | The original input string, unchanged |
| `ProcessedWeightKg` / `processedWeightKg` | Canonical value with exactly six fractional digits |
| `DisplayWeightKg` / `displayWeightKg` | Value with exactly the selected 1, 2, or 3 fractional digits |
| `DecimalPlaces` / `decimalPlaces` | The selected display precision |
| `Method` / `method` | The selected processing method |

Canonical storage formatting does not prescribe a database column or add
persistence behavior. It establishes the representation future storage
boundaries must preserve.

## Stable errors

Both implementations expose the same stable error codes:

| Error code | Condition |
| --- | --- |
| `WEIGHT_RAW_REQUIRED` | Raw input is empty or whitespace-only |
| `WEIGHT_RAW_INVALID` | Raw input does not use the accepted decimal syntax |
| `WEIGHT_RAW_NEGATIVE` | Raw input begins with a negative sign |
| `WEIGHT_RAW_INTEGER_DIGITS_EXCEEDED` | Integer part contains more than 14 digits |
| `WEIGHT_RAW_FRACTION_DIGITS_EXCEEDED` | Fractional part contains more than 6 digits |
| `WEIGHT_DECIMAL_PLACES_INVALID` | Precision is not 1, 2, or 3 |
| `WEIGHT_PROCESSING_METHOD_INVALID` | Method is not exactly Standard, Floor, or Ceiling |

Callers may use the code for deterministic validation behavior. Exception
message wording is explanatory and is not the stable contract.

## Shared golden vectors

The executable contract is
`contracts/golden-vectors/weight-processing.v1.json`. Its metadata identifies
the contract name, version, and six-decimal storage precision. `validCases`
contain named inputs and expected processed/display values. `invalidCases`
contain named invalid inputs and expected stable error codes.

The C# and Dart tests locate the repository root and load this same file at
runtime. They do not embed separate copies:

- C# implementation:
  `services/backend/src/TraderPro.Domain/Common/Measurements/`
- C# vector tests:
  `services/backend/tests/TraderPro.UnitTests/WeightProcessingGoldenVectorTests.cs`
- Dart implementation: `apps/mobile/lib/core/measurements/`
- Dart vector tests:
  `apps/mobile/test/core/measurements/weight_processing_golden_vector_test.dart`

Run both targeted suites from the repository root:

```powershell
powershell -ExecutionPolicy Bypass `
  -File .\scripts\test\test-weight-processing-parity.ps1
```

The .NET implementation uses `System.Numerics.BigInteger`; the Dart
implementation uses `BigInt`. Both are standard-library exact integer types.
No package dependency is added.

## Architecture and deferred behavior

The domain processor has no EF Core, ASP.NET Core, Npgsql, Infrastructure, JSON,
date/time, or database dependency. The mobile processor has no Flutter widget,
Drift, HTTP, network, BLE, or platform-channel dependency.

The following remain explicitly deferred:

- Receiving Sessions and receiving-entry validation, including whether zero
  may be recorded;
- BLE communication, device pairing, scale integration, and drift handling;
- local persistence, PostgreSQL persistence, migrations, and API endpoints;
- offline reconciliation and cloud synchronisation;
- inventory, procurement settlement, and financial posting.

Future workflows must capture the active policy with the raw physical fact and
must not silently discard or rewrite offline-captured raw values.
