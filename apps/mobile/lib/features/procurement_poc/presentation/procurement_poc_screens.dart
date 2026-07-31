import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';

import '../../../core/measurements/weight_processing_exception.dart';
import '../../../core/measurements/weight_processing_method.dart';
import '../../../core/measurements/weight_processor.dart';
import '../../../core/sync/outbox_operation_status.dart';
import '../../receiving/domain/weight_source.dart';
import '../domain/procurement_poc_models.dart';
import 'procurement_poc_controller.dart';

final class ProcurementPocHome extends StatefulWidget {
  const ProcurementPocHome({required this.controller, super.key});

  final ProcurementPocController controller;

  @override
  State<ProcurementPocHome> createState() => _ProcurementPocHomeState();
}

final class _ProcurementPocHomeState extends State<ProcurementPocHome> {
  var _selectedIndex = 0;

  @override
  void initState() {
    super.initState();
    unawaited(widget.controller.initialize());
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: widget.controller,
      builder: (context, _) {
        final controller = widget.controller;
        if (!controller.initialized && controller.busy) {
          return const Scaffold(
            body: Center(child: CircularProgressIndicator()),
          );
        }
        final profile = controller.profile;
        if (profile == null) {
          return Scaffold(
            key: const Key('poc-setup-scaffold'),
            appBar: AppBar(title: const Text('Procurement POC Setup')),
            body: SafeArea(
              top: false,
              child: PocSetupScreen(controller: controller),
            ),
          );
        }
        final roleScreen = profile.displayRole == PocDisplayRole.operator
            ? OperatorSessionScreen(controller: controller)
            : OwnerSessionListScreen(controller: controller);
        final screens = <Widget>[
          roleScreen,
          PocSetupScreen(controller: controller),
          SyncDiagnosticsScreen(controller: controller),
        ];
        return Scaffold(
          appBar: AppBar(
            title: Text(
              profile.displayRole == PocDisplayRole.operator
                  ? 'Procurement POC · Operator'
                  : 'Procurement POC · Owner',
            ),
          ),
          body: SafeArea(
            top: false,
            child: Column(
              children: [
                const DevelopmentOnlyBanner(),
                if (controller.lastMessage != null)
                  _StatusMessage(
                    message: controller.lastMessage!,
                    errorCode: controller.lastErrorCode,
                  ),
                if (controller.busy) const LinearProgressIndicator(),
                Expanded(child: screens[_selectedIndex]),
              ],
            ),
          ),
          bottomNavigationBar: NavigationBar(
            selectedIndex: _selectedIndex,
            onDestinationSelected: (value) {
              setState(() => _selectedIndex = value);
            },
            destinations: const [
              NavigationDestination(
                icon: Icon(Icons.work_outline),
                label: 'POC',
              ),
              NavigationDestination(
                icon: Icon(Icons.settings_outlined),
                label: 'Setup',
              ),
              NavigationDestination(
                icon: Icon(Icons.sync_alt),
                label: 'Diagnostics',
              ),
            ],
          ),
        );
      },
    );
  }
}

final class DevelopmentOnlyBanner extends StatelessWidget {
  const DevelopmentOnlyBanner({super.key});

  @override
  Widget build(BuildContext context) {
    return Semantics(
      label: 'Development Only',
      child: Container(
        width: double.infinity,
        color: Theme.of(context).colorScheme.errorContainer,
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        child: Text(
          'DEVELOPMENT ONLY · Temporary IDs are not authentication',
          textAlign: TextAlign.center,
          style: TextStyle(
            color: Theme.of(context).colorScheme.onErrorContainer,
            fontWeight: FontWeight.w700,
          ),
        ),
      ),
    );
  }
}

final class _StatusMessage extends StatelessWidget {
  const _StatusMessage({required this.message, required this.errorCode});

  final String message;
  final String? errorCode;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: errorCode == null
          ? Theme.of(context).colorScheme.secondaryContainer
          : Theme.of(context).colorScheme.errorContainer,
      child: ListTile(
        dense: true,
        leading: Icon(
          errorCode == null ? Icons.info_outline : Icons.warning_amber,
        ),
        title: Text(message),
        subtitle: errorCode == null ? null : Text(errorCode!),
      ),
    );
  }
}

final class PocSetupScreen extends StatefulWidget {
  const PocSetupScreen({required this.controller, super.key});

  final ProcurementPocController controller;

  @override
  State<PocSetupScreen> createState() => _PocSetupScreenState();
}

final class _PocSetupScreenState extends State<PocSetupScreen> {
  late final TextEditingController _baseUrl;
  late final TextEditingController _workspace;
  late final TextEditingController _device;
  late final TextEditingController _label;
  PocDisplayRole _role = PocDisplayRole.operator;

  @override
  void initState() {
    super.initState();
    final profile = widget.controller.profile;
    _baseUrl = TextEditingController(
      text: profile?.backendBaseUrl ?? 'http://10.0.2.2:5000',
    );
    _workspace = TextEditingController(text: profile?.workspaceId);
    _device = TextEditingController(text: profile?.deviceId);
    _label = TextEditingController(text: profile?.displayLabel);
    _role = profile?.displayRole ?? PocDisplayRole.operator;
  }

  @override
  void dispose() {
    _baseUrl.dispose();
    _workspace.dispose();
    _device.dispose();
    _label.dispose();
    super.dispose();
  }

  void _useBootstrapDevice(PocDisplayRole role) {
    final result = widget.controller.bootstrapResult;
    if (result == null) {
      return;
    }
    setState(() {
      _workspace.text = result.workspaceId;
      _device.text = role == PocDisplayRole.operator
          ? result.operatorDeviceId
          : result.ownerDeviceId;
      _role = role;
    });
  }

  @override
  Widget build(BuildContext context) {
    final bootstrap = widget.controller.bootstrapResult;
    return ListView(
      key: const Key('poc-setup-screen'),
      padding: const EdgeInsets.all(16),
      children: [
        const DevelopmentOnlyBanner(),
        const SizedBox(height: 12),
        Text('POC Setup', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 8),
        const Text(
          'Workspace and Device IDs are insecure development context. This '
          'screen is not login, sign-in, authentication, or authorization.',
        ),
        const SizedBox(height: 16),
        TextField(
          key: const Key('poc-base-url'),
          controller: _baseUrl,
          keyboardType: TextInputType.url,
          decoration: const InputDecoration(
            labelText: 'Backend base URL',
            helperText:
                'Emulator: 10.0.2.2. Physical phone: use the computer LAN IP.',
            border: OutlineInputBorder(),
          ),
        ),
        const SizedBox(height: 12),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            FilledButton.tonalIcon(
              onPressed: widget.controller.busy
                  ? null
                  : () => widget.controller.bootstrap(_baseUrl.text),
              icon: const Icon(Icons.build_circle_outlined),
              label: const Text('Bootstrap development IDs'),
            ),
            OutlinedButton.icon(
              onPressed:
                  widget.controller.busy || widget.controller.profile == null
                  ? null
                  : widget.controller.testConnection,
              icon: const Icon(Icons.lan_outlined),
              label: const Text('Test connection'),
            ),
          ],
        ),
        if (bootstrap != null) ...[
          const SizedBox(height: 12),
          Text(
            'Bootstrap result (selectable/copyable)',
            style: Theme.of(context).textTheme.titleMedium,
          ),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(12),
              child: SelectableText(
                const JsonEncoder.withIndent('  ').convert(<String, Object?>{
                  'workspaceId': bootstrap.workspaceId,
                  'companyId': bootstrap.companyId,
                  'branchId': bootstrap.branchId,
                  'operatorDeviceId': bootstrap.operatorDeviceId,
                  'ownerDeviceId': bootstrap.ownerDeviceId,
                  'setupCode': bootstrap.setupCode,
                }),
              ),
            ),
          ),
          Wrap(
            spacing: 8,
            children: [
              OutlinedButton(
                onPressed: () => _useBootstrapDevice(PocDisplayRole.operator),
                child: const Text('Use Operator ID'),
              ),
              OutlinedButton(
                onPressed: () => _useBootstrapDevice(PocDisplayRole.owner),
                child: const Text('Use Owner ID'),
              ),
            ],
          ),
        ],
        const SizedBox(height: 12),
        TextField(
          key: const Key('poc-workspace-id'),
          controller: _workspace,
          decoration: const InputDecoration(
            labelText: 'Workspace ID',
            border: OutlineInputBorder(),
          ),
        ),
        const SizedBox(height: 12),
        TextField(
          key: const Key('poc-device-id'),
          controller: _device,
          decoration: const InputDecoration(
            labelText: 'Device ID',
            border: OutlineInputBorder(),
          ),
        ),
        const SizedBox(height: 12),
        DropdownButtonFormField<PocDisplayRole>(
          key: const Key('poc-display-role'),
          initialValue: _role,
          isExpanded: true,
          decoration: const InputDecoration(
            labelText: 'Local display role',
            border: OutlineInputBorder(),
          ),
          items: PocDisplayRole.values
              .map(
                (role) => DropdownMenuItem(
                  value: role,
                  child: Text(role.storageValue),
                ),
              )
              .toList(growable: false),
          onChanged: (role) {
            if (role != null) {
              setState(() => _role = role);
            }
          },
        ),
        const SizedBox(height: 12),
        TextField(
          controller: _label,
          decoration: const InputDecoration(
            labelText: 'Optional display label',
            border: OutlineInputBorder(),
          ),
        ),
        const SizedBox(height: 16),
        FilledButton.icon(
          onPressed: widget.controller.busy
              ? null
              : () => widget.controller.saveProfile(
                  backendBaseUrl: _baseUrl.text,
                  workspaceId: _workspace.text,
                  deviceId: _device.text,
                  displayRole: _role,
                  displayLabel: _label.text,
                ),
          icon: const Icon(Icons.save_outlined),
          label: const Text('Save development profile'),
        ),
      ],
    );
  }
}

final class OperatorSessionScreen extends StatelessWidget {
  const OperatorSessionScreen({required this.controller, super.key});

  final ProcurementPocController controller;

  @override
  Widget build(BuildContext context) {
    final session = controller.activeLocalSession;
    final cloud = controller.activeCloudState;
    final counts = <OutboxOperationStatus, int>{
      for (final status in OutboxOperationStatus.values)
        status: controller.localOperations
            .where((operation) => operation.status == status)
            .length,
    };
    return ListView(
      key: const Key('operator-session-screen'),
      padding: const EdgeInsets.all(16),
      children: [
        Text(
          'Operator Session',
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        const Text('Development Only · foreground synchronization POC'),
        const SizedBox(height: 12),
        if (session == null)
          FilledButton.icon(
            onPressed: controller.busy ? null : controller.createLocalSession,
            icon: const Icon(Icons.add),
            label: const Text('Create local Receiving Session'),
          )
        else ...[
          _DetailsCard(
            children: [
              _Detail('Local session ID', session.id),
              _Detail(
                'Cloud reference',
                cloud?.cloudReference ?? 'Not synchronized',
              ),
              _Detail('Local status', session.localStatus.storageValue),
              _Detail('Cloud status', cloud?.cloudStatus ?? 'Queued locally'),
              _Detail(
                'Lease expiry',
                cloud?.leaseExpiresAtUtc?.toIso8601String() ?? 'No lease',
              ),
              _Detail(
                'Heartbeat',
                _heartbeatStatus(controller.controlCommands, cloud),
              ),
              _Detail('Next local sequence', '${session.nextLocalSequence}'),
              _Detail('Entry count', '${session.activeEntryCount}'),
              _Detail(
                'Exact processed total',
                '${session.processedTotalWeightKg} kg',
              ),
            ],
          ),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              FilledButton.tonalIcon(
                key: const Key('operator-sync-now'),
                onPressed: controller.busy ? null : controller.synchronizeNow,
                icon: const Icon(Icons.sync),
                label: const Text('Manual Sync Now'),
              ),
              FilterChip(
                key: const Key('pause-automatic-sync'),
                selected: controller.profile!.automaticSyncPaused,
                label: const Text('Pause Automatic Sync'),
                onSelected: controller.busy
                    ? null
                    : controller.setAutomaticSyncPaused,
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            'Outbox · queued ${counts[OutboxOperationStatus.pending]} · '
            'sending ${counts[OutboxOperationStatus.sending]} · '
            'completed ${counts[OutboxOperationStatus.accepted]} · '
            'attention ${counts[OutboxOperationStatus.needsAttention]}',
          ),
          if (session.localStatus.storageValue == 'Open') ...[
            const SizedBox(height: 16),
            ManualEntryForm(controller: controller),
            const SizedBox(height: 8),
            FilledButton.icon(
              key: const Key('operator-submit'),
              onPressed: controller.busy || controller.localEntries.isEmpty
                  ? null
                  : controller.submitLocalSession,
              icon: const Icon(Icons.send_outlined),
              label: const Text('Submit session'),
            ),
          ],
          const SizedBox(height: 16),
          Text(
            'Recent immutable entries',
            style: Theme.of(context).textTheme.titleMedium,
          ),
          for (final entry in controller.localEntries.reversed.take(3))
            ListTile(
              leading: CircleAvatar(child: Text('${entry.localSequence}')),
              title: Text(
                '${entry.productReference} · ${entry.displayWeightKg} kg',
              ),
              subtitle: Text(
                'raw ${entry.rawWeightKg} · ${entry.processingMethod} · '
                '${entry.weightSource.storageValue}',
              ),
            ),
        ],
      ],
    );
  }

  static String _heartbeatStatus(
    List<PocControlCommand> commands,
    ReceivingSessionCloudState? cloud,
  ) {
    if (cloud?.leaseId == null) {
      return cloud?.cloudStatus == 'SubmittedForReview'
          ? 'Stopped after submission'
          : 'Waiting for Start lease';
    }
    final heartbeat = commands
        .where(
          (command) =>
              command.commandType == PocControlCommandType.heartbeat &&
              command.sessionId == cloud!.localSessionId,
        )
        .firstOrNull;
    return heartbeat?.status.storageValue ?? 'Scheduled in foreground';
  }
}

final class ManualEntryForm extends StatefulWidget {
  const ManualEntryForm({required this.controller, super.key});

  final ProcurementPocController controller;

  @override
  State<ManualEntryForm> createState() => _ManualEntryFormState();
}

final class _ManualEntryFormState extends State<ManualEntryForm> {
  final _product = TextEditingController(text: 'PADDY-POC');
  final _bagType = TextEditingController(text: 'JUTE-50');
  final _bagCount = TextEditingController(text: '10');
  final _rawWeight = TextEditingController(text: '50.237');
  final _capturedAt = TextEditingController(
    text: DateTime.now().toUtc().toIso8601String(),
  );
  var _decimalPlaces = 2;
  var _method = WeightProcessingMethod.standard;
  var _source = WeightSource.manualSpike;

  @override
  void dispose() {
    _product.dispose();
    _bagType.dispose();
    _bagCount.dispose();
    _rawWeight.dispose();
    _capturedAt.dispose();
    super.dispose();
  }

  String get _preview {
    try {
      final result = WeightProcessor.process(
        rawWeightKg: _rawWeight.text,
        decimalPlaces: _decimalPlaces,
        method: _method.contractName,
      );
      return 'Processed ${result.processedWeightKg} kg · '
          'display ${result.displayWeightKg} kg';
    } on WeightProcessingException catch (error) {
      return '${error.errorCode}: ${error.message}';
    }
  }

  @override
  Widget build(BuildContext context) {
    return Card(
      key: const Key('operator-entry-form'),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Manual immutable weight',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const SizedBox(height: 8),
            TextField(
              controller: _product,
              decoration: const InputDecoration(labelText: 'Product reference'),
            ),
            TextField(
              controller: _bagType,
              decoration: const InputDecoration(
                labelText: 'Bag type reference',
              ),
            ),
            TextField(
              controller: _bagCount,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(labelText: 'Bag count'),
            ),
            TextField(
              key: const Key('raw-weight-input'),
              controller: _rawWeight,
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
              onChanged: (_) => setState(() {}),
              decoration: const InputDecoration(
                labelText: 'Raw weight kg (preserved exactly)',
              ),
            ),
            DropdownButtonFormField<int>(
              initialValue: _decimalPlaces,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Decimal places'),
              items: const [
                DropdownMenuItem(value: 1, child: Text('1')),
                DropdownMenuItem(value: 2, child: Text('2')),
                DropdownMenuItem(value: 3, child: Text('3')),
              ],
              onChanged: (value) {
                if (value != null) {
                  setState(() => _decimalPlaces = value);
                }
              },
            ),
            DropdownButtonFormField<WeightProcessingMethod>(
              initialValue: _method,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Processing method'),
              items: WeightProcessingMethod.values
                  .map(
                    (method) => DropdownMenuItem(
                      value: method,
                      child: Text(method.contractName),
                    ),
                  )
                  .toList(growable: false),
              onChanged: (value) {
                if (value != null) {
                  setState(() => _method = value);
                }
              },
            ),
            DropdownButtonFormField<WeightSource>(
              initialValue: _source,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Weight source'),
              items: WeightSource.values
                  .map(
                    (source) => DropdownMenuItem(
                      value: source,
                      child: Text(source.storageValue),
                    ),
                  )
                  .toList(growable: false),
              onChanged: (value) {
                if (value != null) {
                  setState(() => _source = value);
                }
              },
            ),
            TextField(
              controller: _capturedAt,
              decoration: const InputDecoration(
                labelText: 'Captured UTC timestamp',
              ),
            ),
            const SizedBox(height: 8),
            Text(_preview, key: const Key('processed-weight-preview')),
            const SizedBox(height: 8),
            FilledButton(
              key: const Key('save-manual-entry'),
              onPressed: widget.controller.busy
                  ? null
                  : () => widget.controller.recordManualWeight(
                      productReference: _product.text,
                      bagTypeReference: _bagType.text,
                      bagCount: int.tryParse(_bagCount.text) ?? 0,
                      rawWeightKg: _rawWeight.text,
                      decimalPlaces: _decimalPlaces,
                      processingMethod: _method,
                      weightSource: _source,
                      capturedAtDeviceUtcText: _capturedAt.text,
                    ),
              child: const Text('Save on device'),
            ),
          ],
        ),
      ),
    );
  }
}

final class OwnerSessionListScreen extends StatelessWidget {
  const OwnerSessionListScreen({required this.controller, super.key});

  final ProcurementPocController controller;

  @override
  Widget build(BuildContext context) {
    return ListView(
      key: const Key('owner-session-list-screen'),
      padding: const EdgeInsets.all(16),
      children: [
        Text(
          'Owner Session List',
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        const Text(
          'Read-only projection from MobileSync, verified by server list/live '
          'view. No receiving-entry editing is available.',
        ),
        const SizedBox(height: 8),
        Wrap(
          spacing: 8,
          children: [
            FilledButton.tonalIcon(
              onPressed: controller.busy ? null : controller.synchronizeNow,
              icon: const Icon(Icons.sync),
              label: const Text('Poll events now'),
            ),
            OutlinedButton.icon(
              onPressed: controller.busy
                  ? null
                  : controller.refreshOwnerSessions,
              icon: const Icon(Icons.refresh),
              label: const Text('Refresh server list'),
            ),
          ],
        ),
        const SizedBox(height: 8),
        for (final session in controller.remoteSessions)
          Card(
            child: ListTile(
              title: Text('${session.cloudReference} · ${session.status}'),
              subtitle: Text(
                '${session.entryCount} entries · '
                '${session.processedTotalWeightKg} kg · '
                'v${session.cloudVersion}',
              ),
              trailing: const Icon(Icons.chevron_right),
              onTap: () async {
                await controller.openRemoteSession(session.sessionId);
                if (context.mounted) {
                  await Navigator.of(context).push<void>(
                    MaterialPageRoute(
                      builder: (_) =>
                          OwnerLiveViewScreen(controller: controller),
                    ),
                  );
                }
              },
            ),
          ),
        if (controller.remoteSessions.isEmpty)
          const Padding(
            padding: EdgeInsets.all(24),
            child: Text('No cloud session projection has been discovered.'),
          ),
      ],
    );
  }
}

final class OwnerLiveViewScreen extends StatelessWidget {
  const OwnerLiveViewScreen({required this.controller, super.key});

  final ProcurementPocController controller;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: controller,
      builder: (context, _) {
        final session = controller.selectedRemoteSession;
        return Scaffold(
          appBar: AppBar(title: const Text('Owner Live View')),
          body: SafeArea(
            top: false,
            child: Column(
              children: [
                const DevelopmentOnlyBanner(),
                if (controller.busy) const LinearProgressIndicator(),
                Expanded(
                  child: session == null
                      ? const Center(child: Text('No session selected.'))
                      : ListView(
                          key: const Key('owner-live-view-screen'),
                          padding: const EdgeInsets.all(16),
                          children: [
                            const Text(
                              'Read-only monitoring · no receiving-entry '
                              'editing controls',
                            ),
                            const SizedBox(height: 8),
                            _DetailsCard(
                              children: [
                                _Detail(
                                  'Cloud reference',
                                  session.cloudReference,
                                ),
                                _Detail('Status', session.status),
                                _Detail(
                                  'Editor device',
                                  session.editorDeviceId,
                                ),
                                _Detail(
                                  'Lease expiry',
                                  session.leaseExpiresAtUtc
                                          ?.toIso8601String() ??
                                      'No active lease',
                                ),
                                _Detail('Entry count', '${session.entryCount}'),
                                _Detail(
                                  'Exact total',
                                  '${session.processedTotalWeightKg} kg',
                                ),
                                _Detail(
                                  'Cloud version',
                                  '${session.cloudVersion}',
                                ),
                                _Detail(
                                  'Last cloud update',
                                  session.lastCloudUpdateAtUtc
                                      .toIso8601String(),
                                ),
                                if (session.finalizationId != null)
                                  _Detail(
                                    'Finalization ID',
                                    session.finalizationId!,
                                  ),
                              ],
                            ),
                            const SizedBox(height: 8),
                            Wrap(
                              spacing: 8,
                              runSpacing: 8,
                              children: [
                                OutlinedButton.icon(
                                  onPressed: controller.busy
                                      ? null
                                      : () => controller.refreshLiveView(
                                          session.sessionId,
                                        ),
                                  icon: const Icon(Icons.refresh),
                                  label: const Text('Refresh live view'),
                                ),
                                if (session.status == 'SubmittedForReview')
                                  FilledButton.icon(
                                    key: const Key('owner-approve'),
                                    onPressed: controller.busy
                                        ? null
                                        : controller.approveSelected,
                                    icon: const Icon(
                                      Icons.check_circle_outline,
                                    ),
                                    label: const Text('Approve'),
                                  ),
                                if (session.status == 'Approved')
                                  FilledButton.icon(
                                    key: const Key('owner-finalize'),
                                    onPressed: controller.busy
                                        ? null
                                        : controller.finalizeSelected,
                                    icon: const Icon(Icons.task_alt),
                                    label: const Text('Finalize POC session'),
                                  ),
                                if (session.status == 'Finalized' &&
                                    controller.canRetrySameFinalization)
                                  FilledButton.tonalIcon(
                                    key: const Key('owner-retry-finalization'),
                                    onPressed: controller.busy
                                        ? null
                                        : controller.retrySameFinalization,
                                    icon: const Icon(Icons.replay),
                                    label: const Text(
                                      'Retry Same Finalization',
                                    ),
                                  ),
                              ],
                            ),
                            const SizedBox(height: 16),
                            Text(
                              'Recent entries (maximum five)',
                              style: Theme.of(context).textTheme.titleMedium,
                            ),
                            for (final entry in session.recentEntries.take(5))
                              ListTile(
                                leading: CircleAvatar(
                                  child: Text('${entry.localSequence}'),
                                ),
                                title: Text(
                                  '${entry.productReference} · '
                                  '${entry.processedWeightKg} kg',
                                ),
                                subtitle: Text(
                                  '${entry.bagTypeReference} · '
                                  '${entry.bagCount} bags',
                                ),
                              ),
                          ],
                        ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}

final class SyncDiagnosticsScreen extends StatelessWidget {
  const SyncDiagnosticsScreen({required this.controller, super.key});

  final ProcurementPocController controller;

  @override
  Widget build(BuildContext context) {
    final diagnostics = controller.diagnostics;
    return ListView(
      key: const Key('sync-diagnostics-screen'),
      padding: const EdgeInsets.all(16),
      children: [
        Text(
          'Sync Diagnostics',
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        const Text(
          'Safe development diagnostics only. No secrets, database strings, '
          'stack traces, or raw HTML are shown.',
        ),
        if (diagnostics != null)
          _DetailsCard(
            children: [
              _Detail('Queued', '${diagnostics.pending}'),
              _Detail('Sending', '${diagnostics.sending}'),
              _Detail('Completed', '${diagnostics.completed}'),
              _Detail('Needs Attention', '${diagnostics.needsAttention}'),
              _Detail('Rejected', '${diagnostics.rejected}'),
              _Detail('Durable event cursor', '${diagnostics.eventCursor}'),
              _Detail(
                'Last network / polling error',
                diagnostics.lastNetworkOrPollingErrorCode ?? 'None',
              ),
            ],
          ),
        const SizedBox(height: 8),
        FilledButton.tonalIcon(
          onPressed: controller.busy ? null : controller.synchronizeNow,
          icon: const Icon(Icons.sync),
          label: const Text('Manual foreground cycle'),
        ),
        const SizedBox(height: 12),
        Text(
          'Persistent control commands',
          style: Theme.of(context).textTheme.titleMedium,
        ),
        for (final command in controller.controlCommands)
          ListTile(
            title: Text(
              '${command.commandType.storageValue} · '
              '${command.status.storageValue}',
            ),
            subtitle: Text(
              'key ${command.commandId}\n'
              'attempts ${command.attemptCount}'
              '${command.lastErrorCode == null ? '' : ' · ${command.lastErrorCode}'}',
            ),
            isThreeLine: true,
          ),
      ],
    );
  }
}

final class _DetailsCard extends StatelessWidget {
  const _DetailsCard({required this.children});

  final List<_Detail> children;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: LayoutBuilder(
          builder: (context, constraints) {
            final useStackedDetails =
                constraints.maxWidth < 360 ||
                MediaQuery.textScalerOf(context).scale(14) >= 24;
            return Column(
              children: [
                for (final detail in children)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 3),
                    child: useStackedDetails
                        ? Column(
                            crossAxisAlignment: CrossAxisAlignment.stretch,
                            children: [
                              Text(
                                detail.label,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                              SelectableText(detail.value),
                            ],
                          )
                        : Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              SizedBox(
                                width: 145,
                                child: Text(
                                  detail.label,
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ),
                              Expanded(child: SelectableText(detail.value)),
                            ],
                          ),
                  ),
              ],
            );
          },
        ),
      ),
    );
  }
}

final class _Detail {
  const _Detail(this.label, this.value);

  final String label;
  final String value;
}

extension<T> on Iterable<T> {
  T? get firstOrNull {
    final iterator = this.iterator;
    return iterator.moveNext() ? iterator.current : null;
  }
}
