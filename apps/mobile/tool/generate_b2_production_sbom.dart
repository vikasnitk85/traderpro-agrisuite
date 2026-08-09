import 'dart:convert';
import 'dart:io';

import 'package:crypto/crypto.dart';

const _outputPath =
    '../../docs/supply-chain/TASK-7C2B2-Mobile-Production-SBOM.cdx.json';
const _flutterEngineRevision = 'a10d8ac38de835021c8d2f920dbf50a920ccc030';

Future<void> main() async {
  final pubResult = await Process.run('flutter.bat', <String>[
    'pub',
    'deps',
    '--json',
  ], runInShell: true);
  _requireSuccess(pubResult, 'flutter pub deps --json');
  final pubGraph =
      jsonDecode(pubResult.stdout as String) as Map<String, Object?>;

  final gradleResult = await Process.run(
    r'.\gradlew.bat',
    <String>[
      ':app:dependencies',
      '--configuration',
      'releaseRuntimeClasspath',
      '--console=plain',
    ],
    workingDirectory: 'android',
    environment: Platform.environment,
    runInShell: true,
  );
  _requireSuccess(gradleResult, 'Gradle releaseRuntimeClasspath');

  final pub = _productionPubComponents(pubGraph);
  final maven = _mavenComponents(gradleResult.stdout as String);
  final native = await _nativeComponents();
  final components =
      <Map<String, Object?>>[
        ...pub.components,
        ...maven,
        ...native,
        _sqlite3McComponent(),
        _sqliteComponent(),
      ]..sort(
        (left, right) =>
            (left['bom-ref'] as String).compareTo(right['bom-ref'] as String),
      );

  final rootDependencies = <String>{
    ...pub.rootDependencies,
    ...maven.map((component) => component['bom-ref']! as String),
    ...native.map((component) => component['bom-ref']! as String),
    'pkg:generic/sqlite3mc@2.3.6',
    'pkg:generic/sqlite@3.53.3',
  }.toList()..sort();
  final dependencyEntries = <Map<String, Object?>>[
    <String, Object?>{
      'ref': 'pkg:pub/traderpro_agrisuite_mobile@1.0.0%2B1',
      'dependsOn': rootDependencies,
    },
    ...pub.dependencies,
    <String, Object?>{
      'ref': 'native:sqlite3mc:android:arm64-v8a',
      'dependsOn': <String>[
        'pkg:generic/sqlite3mc@2.3.6',
        'pkg:generic/sqlite@3.53.3',
      ],
    },
    <String, Object?>{
      'ref': 'native:sqlite3mc:android:armeabi-v7a',
      'dependsOn': <String>[
        'pkg:generic/sqlite3mc@2.3.6',
        'pkg:generic/sqlite@3.53.3',
      ],
    },
  ];

  final sbom = <String, Object?>{
    r'$schema': 'https://cyclonedx.org/schema/bom-1.6.schema.json',
    'bomFormat': 'CycloneDX',
    'specVersion': '1.6',
    'version': 1,
    'metadata': <String, Object?>{
      'timestamp': '2026-08-08T00:00:00Z',
      'tools': <String, Object?>{
        'components': <Map<String, Object?>>[
          <String, Object?>{
            'type': 'application',
            'name': 'TraderPro Task 7C2B2 production SBOM generator',
            'version': '1',
          },
        ],
      },
      'component': <String, Object?>{
        'type': 'application',
        'bom-ref': 'pkg:pub/traderpro_agrisuite_mobile@1.0.0%2B1',
        'group': 'com.traderpro.agrisuite',
        'name': 'traderpro_agrisuite_mobile',
        'version': '1.0.0+1',
        'purl': 'pkg:pub/traderpro_agrisuite_mobile@1.0.0%2B1',
      },
      'properties': <Map<String, String>>[
        <String, String>{
          'name': 'traderpro:task',
          'value': '7C2B2 secure commercial local foundation',
        },
        <String, String>{
          'name': 'traderpro:production-abis',
          'value': 'armeabi-v7a,arm64-v8a',
        },
        <String, String>{
          'name': 'traderpro:x86_64-policy',
          'value': 'emulator/test-only; excluded from production APKs',
        },
        <String, String>{
          'name': 'traderpro:flutter-engine-revision',
          'value': _flutterEngineRevision,
        },
      ],
    },
    'components': components,
    'dependencies': dependencyEntries,
  };

  final output = File(_outputPath);
  await output.parent.create(recursive: true);
  await output.writeAsString(
    '${const JsonEncoder.withIndent('  ').convert(sbom)}\n',
    flush: true,
  );
  stdout.writeln('Wrote ${output.path} with ${components.length} components.');
}

({
  List<Map<String, Object?>> components,
  List<Map<String, Object?>> dependencies,
  List<String> rootDependencies,
})
_productionPubComponents(Map<String, Object?> graph) {
  final packageList = graph['packages']! as List<Object?>;
  final packages = <String, Map<String, Object?>>{};
  for (final value in packageList) {
    final package = value! as Map<String, Object?>;
    packages[package['name']! as String] = package;
  }
  final root = packages[graph['root']! as String]!;
  final direct = (root['directDependencies']! as List<Object?>).cast<String>();
  final included = <String>{};

  void visit(String name) {
    if (!included.add(name)) {
      return;
    }
    final package = packages[name];
    if (package == null) {
      throw StateError('Missing pub graph node for $name.');
    }
    for (final dependency
        in (package['dependencies']! as List<Object?>).cast<String>()) {
      visit(dependency);
    }
  }

  for (final dependency in direct) {
    visit(dependency);
  }

  final components = <Map<String, Object?>>[];
  final dependencies = <Map<String, Object?>>[];
  for (final name in included.toList()..sort()) {
    final package = packages[name]!;
    final version = package['version']! as String;
    final encodedVersion = Uri.encodeComponent(version);
    final reference = 'pkg:pub/$name@$encodedVersion';
    components.add(<String, Object?>{
      'type': 'library',
      'bom-ref': reference,
      'name': name,
      'version': version,
      'scope': 'required',
      'purl': reference,
      'properties': <Map<String, String>>[
        <String, String>{
          'name': 'traderpro:pub-source',
          'value': package['source']! as String,
        },
      ],
    });
    final dependsOn =
        (package['dependencies']! as List<Object?>)
            .cast<String>()
            .where(included.contains)
            .map((dependency) {
              final dependencyVersion =
                  packages[dependency]!['version']! as String;
              return 'pkg:pub/$dependency@${Uri.encodeComponent(dependencyVersion)}';
            })
            .toList()
          ..sort();
    dependencies.add(<String, Object?>{
      'ref': reference,
      'dependsOn': dependsOn,
    });
  }

  return (
    components: components,
    dependencies: dependencies,
    rootDependencies: direct.map((dependency) {
      final version = packages[dependency]!['version']! as String;
      return 'pkg:pub/$dependency@${Uri.encodeComponent(version)}';
    }).toList(),
  );
}

List<Map<String, Object?>> _mavenComponents(String report) {
  final coordinate = RegExp(
    r'([A-Za-z0-9_.-]+):([A-Za-z0-9_.-]+):([A-Za-z0-9_.+\-]+)'
    r'(?:\s+->\s+([A-Za-z0-9_.+\-]+))?',
  );
  final selected = <String, String>{};
  final explicitlySelected = <String>{};
  for (final line in const LineSplitter().convert(report)) {
    final match = coordinate.firstMatch(line);
    if (match == null) {
      continue;
    }
    final group = match.group(1)!;
    final name = match.group(2)!;
    if (group == 'io.flutter' && name == 'x86_64_release') {
      continue;
    }
    final key = '$group:$name';
    final redirected = match.group(4);
    if (redirected != null) {
      selected[key] = redirected;
      explicitlySelected.add(key);
    } else if (!explicitlySelected.contains(key)) {
      selected.putIfAbsent(key, () => match.group(3)!);
    }
  }

  return selected.entries.map((entry) {
    final separator = entry.key.indexOf(':');
    final group = entry.key.substring(0, separator);
    final name = entry.key.substring(separator + 1);
    final version = entry.value;
    final reference = 'pkg:maven/$group/$name@$version';
    return <String, Object?>{
      'type': 'library',
      'bom-ref': reference,
      'group': group,
      'name': name,
      'version': version,
      'scope': 'required',
      'purl': reference,
      'properties': <Map<String, String>>[
        <String, String>{
          'name': 'traderpro:gradle-configuration',
          'value': 'releaseRuntimeClasspath',
        },
      ],
    };
  }).toList();
}

Future<List<Map<String, Object?>>> _nativeComponents() async {
  const artifacts = <({String abi, String path})>[
    (
      abi: 'arm64-v8a',
      path:
          'build/b2-native-inspection/arm64-v8a/lib/arm64-v8a/libsqlite3mc.so',
    ),
    (
      abi: 'armeabi-v7a',
      path:
          'build/b2-native-inspection/armeabi-v7a/lib/armeabi-v7a/libsqlite3mc.so',
    ),
  ];
  final components = <Map<String, Object?>>[];
  for (final artifact in artifacts) {
    final file = File(artifact.path);
    if (!await file.exists()) {
      throw StateError('Missing release native artifact: ${artifact.path}');
    }
    final bytes = await file.readAsBytes();
    components.add(<String, Object?>{
      'type': 'file',
      'bom-ref': 'native:sqlite3mc:android:${artifact.abi}',
      'name': 'libsqlite3mc.so (${artifact.abi})',
      'version': '2.3.6+sqlite-3.53.3',
      'scope': 'required',
      'hashes': <Map<String, String>>[
        <String, String>{
          'alg': 'SHA-256',
          'content': sha256.convert(bytes).toString(),
        },
      ],
      'properties': <Map<String, String>>[
        <String, String>{'name': 'traderpro:abi', 'value': artifact.abi},
        <String, String>{
          'name': 'traderpro:size-bytes',
          'value': bytes.length.toString(),
        },
        <String, String>{
          'name': 'traderpro:dynamic-dependencies',
          'value': 'libm.so,libdl.so,libc.so',
        },
        <String, String>{'name': 'traderpro:stripped', 'value': 'true'},
      ],
    });
  }
  return components;
}

Map<String, Object?> _sqlite3McComponent() => <String, Object?>{
  'type': 'library',
  'bom-ref': 'pkg:generic/sqlite3mc@2.3.6',
  'name': 'SQLite3 Multiple Ciphers',
  'version': '2.3.6',
  'scope': 'required',
  'purl': 'pkg:generic/sqlite3mc@2.3.6',
  'externalReferences': <Map<String, String>>[
    <String, String>{
      'type': 'website',
      'url': 'https://utelle.github.io/SQLite3MultipleCiphers/',
    },
  ],
};

Map<String, Object?> _sqliteComponent() => <String, Object?>{
  'type': 'library',
  'bom-ref': 'pkg:generic/sqlite@3.53.3',
  'name': 'SQLite',
  'version': '3.53.3',
  'scope': 'required',
  'purl': 'pkg:generic/sqlite@3.53.3',
  'externalReferences': <Map<String, String>>[
    <String, String>{'type': 'website', 'url': 'https://sqlite.org/'},
  ],
};

void _requireSuccess(ProcessResult result, String operation) {
  if (result.exitCode != 0) {
    throw StateError('$operation failed with exit code ${result.exitCode}.');
  }
}
