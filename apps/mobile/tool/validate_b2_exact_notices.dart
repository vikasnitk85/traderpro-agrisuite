import 'dart:convert';
import 'dart:io';

import 'package:crypto/crypto.dart';

const _sbomPath =
    '../../docs/supply-chain/TASK-7C2B2-Mobile-Production-SBOM.cdx.json';
const _noticePath =
    '../../docs/supply-chain/TASK-7C2B2-Third-Party-Notices-Exact.md';

void main() {
  final sbom =
      jsonDecode(File(_sbomPath).readAsStringSync()) as Map<String, Object?>;
  final notice = File(_noticePath).readAsStringSync();
  final components = (sbom['components']! as List<Object?>)
      .cast<Map<String, Object?>>();
  final expectedReferences = components
      .map((component) => component['bom-ref']! as String)
      .toSet();

  final indexedReferences = _captures(
    notice,
    RegExp(r'<!-- sbom-component: (.+?) -->'),
  );
  final exactReferenceLines = RegExp(
    r'<!-- exact-ref: (.+?) => (.+?) -->',
  ).allMatches(notice).toList();
  final mappedReferences = exactReferenceLines
      .map((match) => match.group(1)!)
      .toList();
  final exactTextIds = _captures(
    notice,
    RegExp(r'<!-- exact-text: ([A-F0-9]{64}) -->'),
  );
  final indexedLicenseLines = RegExp(
    r'<!-- sbom-component: (.+?) -->\r?\n'
    r'<!-- exact-ref: .*? -->\r?\n'
    r'- \*\*`.*?` .*?\*\* — `([^`]+)`\.',
  ).allMatches(notice).toList();

  _requireUnique('component index', indexedReferences);
  _requireUnique('exact-text mapping', mappedReferences);
  _requireUnique('exact-text section', exactTextIds);
  _requireSameSet('component index', expectedReferences, indexedReferences);
  _requireSameSet('exact-text mappings', expectedReferences, mappedReferences);
  _requireSameSet(
    'license index',
    expectedReferences,
    indexedLicenseLines.map((match) => match.group(1)!).toList(),
  );

  for (final line in indexedLicenseLines) {
    final componentReference = line.group(1)!;
    final actualLicense = line.group(2)!;
    final expectedLicense = _expectedLicense(componentReference);
    if (actualLicense != expectedLicense) {
      _fail(
        'Incorrect license label for $componentReference: '
        '$actualLicense; expected $expectedLicense.',
      );
    }
  }

  final exactTextIdSet = exactTextIds.toSet();
  final usedExactTextIds = <String>[];
  for (final line in exactReferenceLines) {
    final componentReference = line.group(1)!;
    final sectionIds = line.group(2)!.split(',');
    if (sectionIds.isEmpty || sectionIds.any((id) => id.isEmpty)) {
      _fail('Empty exact-text mapping for $componentReference.');
    }
    for (final sectionId in sectionIds) {
      usedExactTextIds.add(sectionId);
      if (!exactTextIdSet.contains(sectionId)) {
        _fail(
          'Component $componentReference refers to missing exact-text '
          'section $sectionId.',
        );
      }
    }
  }
  _requireSameSet('used exact-text sections', exactTextIdSet, usedExactTextIds);

  final exactBodies = RegExp(
    r'<!-- exact-text: ([A-F0-9]{64}) -->[\s\S]*?'
    r'```text\r?\n([\s\S]*?)```',
  ).allMatches(notice).toList();
  if (exactBodies.length != exactTextIds.length) {
    _fail(
      'Expected ${exactTextIds.length} exact bodies, found '
      '${exactBodies.length}.',
    );
  }
  for (final section in exactBodies) {
    final expectedHash = section.group(1)!;
    final body = section.group(2)!;
    final candidates = <String>[body];
    if (body.endsWith('\r\n')) {
      candidates.add(body.substring(0, body.length - 2));
    } else if (body.endsWith('\n')) {
      candidates.add(body.substring(0, body.length - 1));
    }
    final bodyMatches = candidates.any(
      (candidate) =>
          sha256.convert(utf8.encode(candidate)).toString().toUpperCase() ==
          expectedHash,
    );
    if (!bodyMatches) {
      _fail('Exact body does not match section hash $expectedHash.');
    }
  }

  _requireMarker(notice, 'LEGAL_NOTICE_APPROVAL = APPROVED');
  _requireMarker(notice, 'UNRESOLVED_NOTICE_ITEMS = 0');
  _requireMarker(notice, 'SQLCIPHER_PRODUCTION_COMPONENTS = 0');
  _requireMarker(notice, 'OPENSSL_PRODUCTION_COMPONENTS = 0');

  final pubCount = expectedReferences
      .where((reference) => reference.startsWith('pkg:pub/'))
      .length;
  final mavenCount = expectedReferences
      .where((reference) => reference.startsWith('pkg:maven/'))
      .length;
  final genericCount = expectedReferences
      .where((reference) => reference.startsWith('pkg:generic/'))
      .length;
  final nativeCount = expectedReferences
      .where((reference) => reference.startsWith('native:'))
      .length;

  if (pubCount != 56 ||
      mavenCount != 52 ||
      genericCount != 2 ||
      nativeCount != 2 ||
      expectedReferences.length != 112) {
    _fail(
      'Unexpected B2 SBOM shape: total=${expectedReferences.length}, '
      'pub=$pubCount, maven=$mavenCount, generic=$genericCount, '
      'native=$nativeCount.',
    );
  }

  stdout.writeln(
    'B2 exact-notice coverage verified: '
    '${expectedReferences.length}/112 components; '
    '$pubCount/56 Pub; $mavenCount/52 Maven; '
    '$genericCount/2 generic native projects; '
    '$nativeCount/2 selected native assets; '
    '${exactTextIds.length} unique exact-text sections and bodies; '
    '0 unresolved.',
  );
}

String _expectedLicense(String componentReference) {
  if (componentReference.startsWith('pkg:pub/')) {
    final packageName = componentReference.substring(
      'pkg:pub/'.length,
      componentReference.indexOf('@'),
    );
    return switch (packageName) {
      'drift' || 'sqlite3' || 'uuid' || 'yaml' => 'MIT',
      'material_color_utilities' => 'Apache-2.0',
      'vector_math' => 'BSD-3-Clause AND Zlib',
      'sky_engine' => 'Multiple licenses (Flutter engine aggregate)',
      _ => 'BSD-3-Clause',
    };
  }
  if (componentReference.startsWith('pkg:maven/io.flutter/')) {
    return 'Multiple licenses (Flutter engine aggregate)';
  }
  if (componentReference.startsWith('pkg:maven/')) {
    return 'Apache-2.0';
  }
  return switch (componentReference) {
    'pkg:generic/sqlite3mc@2.3.6' => 'MIT',
    'pkg:generic/sqlite@3.53.3' => 'Public domain',
    'native:sqlite3mc:android:arm64-v8a' ||
    'native:sqlite3mc:android:armeabi-v7a' => 'MIT + SQLite public domain',
    _ => throw StateError(
      'No expected B2 license classification for $componentReference.',
    ),
  };
}

List<String> _captures(String input, RegExp pattern) =>
    pattern.allMatches(input).map((match) => match.group(1)!).toList();

void _requireUnique(String label, List<String> values) {
  final duplicates = <String>{};
  final seen = <String>{};
  for (final value in values) {
    if (!seen.add(value)) {
      duplicates.add(value);
    }
  }
  if (duplicates.isNotEmpty) {
    _fail('Duplicate $label entries: ${duplicates.join(', ')}.');
  }
}

void _requireSameSet(
  String label,
  Set<String> expected,
  List<String> actualValues,
) {
  final actual = actualValues.toSet();
  final missing = expected.difference(actual).toList()..sort();
  final unexpected = actual.difference(expected).toList()..sort();
  if (missing.isNotEmpty || unexpected.isNotEmpty) {
    _fail(
      '$label differs from the production SBOM. '
      'Missing: ${missing.join(', ')}. '
      'Unexpected: ${unexpected.join(', ')}.',
    );
  }
}

void _requireMarker(String notice, String marker) {
  if (!notice.contains(marker)) {
    _fail('Missing required notice marker: $marker.');
  }
}

Never _fail(String message) {
  stderr.writeln(message);
  exit(1);
}
