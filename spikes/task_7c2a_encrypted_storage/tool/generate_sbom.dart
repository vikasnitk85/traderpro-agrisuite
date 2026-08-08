import 'dart:convert';
import 'dart:io';

import 'package:yaml/yaml.dart';

const _outputPath = '../../docs/supply-chain/TASK-7C2B1-Mobile-SBOM.cdx.json';

void main() {
  final lock = loadYaml(File('pubspec.lock').readAsStringSync()) as YamlMap;
  final packages = lock['packages'] as YamlMap;
  final components = <Map<String, Object?>>[];

  for (final entry in packages.entries) {
    final name = entry.key.toString();
    final data = entry.value as YamlMap;
    final version = data['version'].toString();
    final source = data['source'].toString();
    final dependency = data['dependency'].toString();
    final description = data['description'];
    String? sha256;
    if (description is YamlMap && description['sha256'] != null) {
      sha256 = description['sha256'].toString();
    }
    components.add({
      'type': 'library',
      'bom-ref': 'pkg:pub/$name@$version',
      'name': name,
      'version': version,
      'purl': 'pkg:pub/$name@$version',
      if (sha256 != null)
        'hashes': [
          {'alg': 'SHA-256', 'content': sha256},
        ],
      'properties': [
        {'name': 'traderpro:ecosystem', 'value': 'Dart/pub'},
        {'name': 'traderpro:dependency-class', 'value': dependency},
        {'name': 'traderpro:lock-source', 'value': source},
      ],
    });
  }

  for (final dependency in _mavenComponents) {
    final coordinate = dependency.split(':');
    final group = coordinate[0];
    final name = coordinate[1];
    final version = coordinate.sublist(2).join(':');
    components.add({
      'type': 'library',
      'bom-ref': 'pkg:maven/$group/$name@$version',
      'group': group,
      'name': name,
      'version': version,
      'purl': 'pkg:maven/$group/$name@$version',
      'properties': [
        {'name': 'traderpro:ecosystem', 'value': 'Android/Gradle'},
        {'name': 'traderpro:resolution', 'value': 'releaseRuntimeClasspath'},
      ],
    });
  }

  components.addAll(_nativeComponents);
  components.sort(
    (left, right) =>
        left['bom-ref'].toString().compareTo(right['bom-ref'].toString()),
  );

  final rootReference = 'pkg:generic/traderpro-7c2b1-storage-evidence@1';
  final document = <String, Object?>{
    r'$schema': 'https://cyclonedx.org/schema/bom-1.6.schema.json',
    'bomFormat': 'CycloneDX',
    'specVersion': '1.6',
    'serialNumber': 'urn:uuid:36fa3f4e-d92c-4ef4-a623-7c2b10000001',
    'version': 1,
    'metadata': {
      'timestamp': '2026-08-03T00:00:00Z',
      'tools': {
        'components': [
          {
            'type': 'application',
            'name': 'TraderPro Task 7C2B1 deterministic SBOM generator',
            'version': '1',
          },
        ],
      },
      'component': {
        'type': 'application',
        'bom-ref': rootReference,
        'name': 'TraderPro Task 7C2B1 encrypted-storage evidence harness',
        'version': '1',
        'properties': [
          {
            'name': 'traderpro:scope',
            'value': 'isolated spike only; not the production mobile app',
          },
          {
            'name': 'traderpro:sca-status',
            'value': 'not executed; approved scanner unavailable',
          },
        ],
      },
    },
    'components': components,
    'dependencies': [
      {
        'ref': rootReference,
        'dependsOn': components
            .map((component) => component['bom-ref'].toString())
            .toList(),
      },
    ],
  };

  final output = File(_outputPath);
  output.parent.createSync(recursive: true);
  output.writeAsStringSync(
    '${const JsonEncoder.withIndent('  ').convert(document)}\n',
  );
}

const _mavenComponents = <String>[
  'androidx.activity:activity:1.8.1',
  'androidx.annotation:annotation:1.8.1',
  'androidx.annotation:annotation-experimental:1.4.0',
  'androidx.annotation:annotation-jvm:1.8.2',
  'androidx.arch.core:core-common:2.2.0',
  'androidx.arch.core:core-runtime:2.2.0',
  'androidx.collection:collection:1.1.0',
  'androidx.concurrent:concurrent-futures:1.1.0',
  'androidx.core:core:1.13.1',
  'androidx.core:core-ktx:1.13.1',
  'androidx.customview:customview:1.0.0',
  'androidx.exifinterface:exifinterface:1.4.1',
  'androidx.fragment:fragment:1.7.1',
  'androidx.interpolator:interpolator:1.0.0',
  'androidx.lifecycle:lifecycle-common:2.7.0',
  'androidx.lifecycle:lifecycle-common-java8:2.7.0',
  'androidx.lifecycle:lifecycle-livedata:2.7.0',
  'androidx.lifecycle:lifecycle-livedata-core:2.7.0',
  'androidx.lifecycle:lifecycle-livedata-core-ktx:2.7.0',
  'androidx.lifecycle:lifecycle-process:2.7.0',
  'androidx.lifecycle:lifecycle-runtime:2.7.0',
  'androidx.lifecycle:lifecycle-viewmodel:2.7.0',
  'androidx.lifecycle:lifecycle-viewmodel-savedstate:2.7.0',
  'androidx.loader:loader:1.0.0',
  'androidx.profileinstaller:profileinstaller:1.3.1',
  'androidx.savedstate:savedstate:1.2.1',
  'androidx.startup:startup-runtime:1.1.1',
  'androidx.tracing:tracing:1.2.0',
  'androidx.versionedparcelable:versionedparcelable:1.1.1',
  'androidx.viewpager:viewpager:1.0.0',
  'androidx.window.extensions.core:core:1.0.0',
  'androidx.window:window:1.2.0',
  'androidx.window:window-java:1.2.0',
  'com.getkeepsafe.relinker:relinker:1.4.5',
  'com.google.code.findbugs:jsr305:3.0.2',
  'com.google.code.gson:gson:2.13.2',
  'com.google.crypto.tink:tink-android:1.21.0',
  'com.google.errorprone:error_prone_annotations:2.41.0',
  'com.google.guava:listenablefuture:1.0',
  'io.flutter:arm64_v8a_release:1.0.0-a10d8ac38de835021c8d2f920dbf50a920ccc030',
  'io.flutter:armeabi_v7a_release:1.0.0-a10d8ac38de835021c8d2f920dbf50a920ccc030',
  'io.flutter:flutter_embedding_release:1.0.0-a10d8ac38de835021c8d2f920dbf50a920ccc030',
  'io.flutter:x86_64_release:1.0.0-a10d8ac38de835021c8d2f920dbf50a920ccc030',
  'org.jetbrains:annotations:23.0.0',
  'org.jetbrains.kotlin:kotlin-stdlib:2.3.20',
  'org.jetbrains.kotlin:kotlin-stdlib-common:2.3.20',
  'org.jetbrains.kotlin:kotlin-stdlib-jdk7:1.8.20',
  'org.jetbrains.kotlin:kotlin-stdlib-jdk8:1.8.20',
  'org.jetbrains.kotlinx:kotlinx-coroutines-android:1.7.1',
  'org.jetbrains.kotlinx:kotlinx-coroutines-bom:1.7.1',
  'org.jetbrains.kotlinx:kotlinx-coroutines-core:1.7.1',
  'org.jetbrains.kotlinx:kotlinx-coroutines-core-jvm:1.7.1',
  'org.jspecify:jspecify:1.0.0',
];

const _nativeComponents = <Map<String, Object?>>[
  {
    'type': 'library',
    'bom-ref': 'pkg:generic/sqlcipher@4.17.0',
    'name': 'SQLCipher Community',
    'version': '4.17.0',
    'purl': 'pkg:generic/sqlcipher@4.17.0',
    'licenses': [
      {
        'license': {'id': 'BSD-3-Clause'},
      },
    ],
  },
  {
    'type': 'library',
    'bom-ref': 'pkg:generic/sqlite3mc@2.3.6',
    'name': 'SQLite3MultipleCiphers',
    'version': '2.3.6',
    'purl': 'pkg:generic/sqlite3mc@2.3.6',
    'licenses': [
      {
        'license': {'id': 'MIT'},
      },
    ],
  },
  {
    'type': 'library',
    'bom-ref': 'pkg:generic/sqlite@3.53.3',
    'name': 'SQLite',
    'version': '3.53.3',
    'purl': 'pkg:generic/sqlite@3.53.3',
    'licenses': [
      {
        'license': {'name': 'Public Domain'},
      },
    ],
  },
  {
    'type': 'library',
    'bom-ref': 'pkg:generic/openssl@3.6.2',
    'name': 'OpenSSL',
    'version': '3.6.2',
    'purl': 'pkg:generic/openssl@3.6.2',
    'licenses': [
      {
        'license': {'id': 'Apache-2.0'},
      },
    ],
    'properties': [
      {
        'name': 'traderpro:linkage',
        'value': 'statically linked into SQLCipher Android ELF',
      },
    ],
  },
  ..._sqlCipherArtifacts,
  ..._sqlite3McArtifacts,
];

const _sqlCipherArtifacts = <Map<String, Object?>>[
  {
    'type': 'file',
    'bom-ref': 'native:sqlcipher:android:arm64-v8a',
    'name': 'libsqlcipher.so (arm64-v8a)',
    'version': '4.17.0',
    'hashes': [
      {
        'alg': 'SHA-256',
        'content':
            '434b5862737328d913964bd5eefac7db8748cd979ac863b3dfc09952e89f9cbd',
      },
    ],
    'properties': [
      {'name': 'traderpro:elf-machine', 'value': 'AArch64'},
      {'name': 'traderpro:bytes', 'value': '5080752'},
    ],
  },
  {
    'type': 'file',
    'bom-ref': 'native:sqlcipher:android:armeabi-v7a',
    'name': 'libsqlcipher.so (armeabi-v7a)',
    'version': '4.17.0',
    'hashes': [
      {
        'alg': 'SHA-256',
        'content':
            'e82f4ccf192bdd8fa5c1f996618fe70362bdeb7a4cec4f3e98d10da51f49a27d',
      },
    ],
    'properties': [
      {'name': 'traderpro:elf-machine', 'value': 'ARM'},
      {'name': 'traderpro:bytes', 'value': '4094016'},
    ],
  },
  {
    'type': 'file',
    'bom-ref': 'native:sqlcipher:android:x86_64',
    'name': 'libsqlcipher.so (x86_64)',
    'version': '4.17.0',
    'hashes': [
      {
        'alg': 'SHA-256',
        'content':
            '1bca6e2a4db6e39004f1933bc10b5b6e95ee371c70c1d16a920c787ee57afaed',
      },
    ],
    'properties': [
      {
        'name': 'traderpro:elf-machine',
        'value': 'Advanced Micro Devices X86-64',
      },
      {'name': 'traderpro:bytes', 'value': '5939008'},
    ],
  },
];

const _sqlite3McArtifacts = <Map<String, Object?>>[
  {
    'type': 'file',
    'bom-ref': 'native:sqlite3mc:android:arm64-v8a',
    'name': 'libsqlite3mc.so (arm64-v8a)',
    'version': '2.3.6',
    'hashes': [
      {
        'alg': 'SHA-256',
        'content':
            '3500da95ebda57b2a749cfaf74258d9bb7b94702f14e435505d302942d869403',
      },
    ],
    'properties': [
      {'name': 'traderpro:elf-machine', 'value': 'AArch64'},
      {'name': 'traderpro:bytes', 'value': '2043872'},
    ],
  },
  {
    'type': 'file',
    'bom-ref': 'native:sqlite3mc:android:armeabi-v7a',
    'name': 'libsqlite3mc.so (armeabi-v7a)',
    'version': '2.3.6',
    'hashes': [
      {
        'alg': 'SHA-256',
        'content':
            '7e90f4263ae214925f945054a5ec514f5182ee3dedf2576c7bec1142f67040a6',
      },
    ],
    'properties': [
      {'name': 'traderpro:elf-machine', 'value': 'ARM'},
      {'name': 'traderpro:bytes', 'value': '2037668'},
    ],
  },
  {
    'type': 'file',
    'bom-ref': 'native:sqlite3mc:android:x86_64',
    'name': 'libsqlite3mc.so (x86_64)',
    'version': '2.3.6',
    'hashes': [
      {
        'alg': 'SHA-256',
        'content':
            '88be4bd4ceb1e65e84d04916489f33d8b08b566950a5e1311ac94a47ced90d28',
      },
    ],
    'properties': [
      {
        'name': 'traderpro:elf-machine',
        'value': 'Advanced Micro Devices X86-64',
      },
      {'name': 'traderpro:bytes', 'value': '2139576'},
    ],
  },
];
