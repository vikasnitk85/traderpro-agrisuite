import 'package:flutter/material.dart';

import '../core/identity/commercial_authentication_status.dart';
import 'commercial_identity_controller.dart';

final class CommercialIdentityScreen extends StatefulWidget {
  const CommercialIdentityScreen({required this.controller, super.key});

  final CommercialIdentityController controller;

  @override
  State<CommercialIdentityScreen> createState() =>
      _CommercialIdentityScreenState();
}

final class _CommercialIdentityScreenState
    extends State<CommercialIdentityScreen> {
  final _activationWorkspace = TextEditingController();
  final _activationCode = TextEditingController();
  final _deviceLabel = TextEditingController();
  final _loginWorkspace = TextEditingController();
  final _login = TextEditingController();
  final _password = TextEditingController();

  @override
  void dispose() {
    _activationWorkspace.dispose();
    _activationCode.dispose();
    _deviceLabel.dispose();
    _loginWorkspace.dispose();
    _login.dispose();
    _password.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.controller,
      builder: (context, _) => Scaffold(
        appBar: AppBar(title: const Text('TraderPro AgriSuite')),
        body: SafeArea(
          child: Center(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 480),
                child: _content(context),
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _content(BuildContext context) => switch (widget.controller.status) {
    CommercialAuthenticationStatus.deviceUnregistered => _activationForm(),
    CommercialAuthenticationStatus.activating => _progress(
      'Activating this Device securely…',
    ),
    CommercialAuthenticationStatus.activationOutcomeUnknown =>
      _activationRecovery(),
    CommercialAuthenticationStatus.deviceRegisteredNoSession ||
    CommercialAuthenticationStatus.authRequired => _loginForm(),
    CommercialAuthenticationStatus.authenticating => _progress(
      'Authenticating…',
    ),
    CommercialAuthenticationStatus.identityContextLoading ||
    CommercialAuthenticationStatus.refreshing => _progress(
      'Revalidating identity…',
    ),
    CommercialAuthenticationStatus.boundOfflineRevalidationRequired =>
      _offlineStatus(),
    CommercialAuthenticationStatus.identityContextMismatch => _blockingStatus(
      icon: Icons.gpp_bad_outlined,
      title: 'Identity does not match this installation',
      message:
          'This installation remains bound to its original account. No local data was changed.',
    ),
    CommercialAuthenticationStatus.deviceRevoked => _blockingStatus(
      icon: Icons.phonelink_erase_outlined,
      title: 'Device is inactive',
      message:
          'An Owner or administrator must reactivate this Device before it can sign in.',
    ),
    CommercialAuthenticationStatus.lockedFailClosed ||
    CommercialAuthenticationStatus.storageUnavailable => _blockingStatus(
      icon: Icons.lock_outline,
      title: 'Secure identity storage unavailable',
      message:
          'TraderPro kept encrypted local data unchanged. Resolve the secure-storage issue and restart.',
    ),
    CommercialAuthenticationStatus.identityContextReady => _readyStatus(),
    CommercialAuthenticationStatus.loggingOut => _progress('Signing out…'),
    CommercialAuthenticationStatus.closed => _blockingStatus(
      icon: Icons.power_settings_new,
      title: 'TraderPro is closed',
      message: 'Restart the application to continue.',
    ),
    CommercialAuthenticationStatus.sessionRevoked => _loginForm(),
  };

  Widget _activationForm() => AutofillGroup(
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const Icon(Icons.phonelink_lock_outlined, size: 48),
        const SizedBox(height: 16),
        Text(
          'Activate Device',
          style: Theme.of(context).textTheme.headlineSmall,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: 24),
        TextField(
          key: const Key('activation-workspace-code'),
          controller: _activationWorkspace,
          textCapitalization: TextCapitalization.characters,
          decoration: const InputDecoration(
            labelText: 'Workspace code',
            border: OutlineInputBorder(),
          ),
          textInputAction: TextInputAction.next,
        ),
        const SizedBox(height: 16),
        TextField(
          key: const Key('activation-code'),
          controller: _activationCode,
          obscureText: true,
          enableSuggestions: false,
          autocorrect: false,
          decoration: const InputDecoration(
            labelText: 'Activation code',
            border: OutlineInputBorder(),
          ),
          textInputAction: TextInputAction.next,
        ),
        const SizedBox(height: 16),
        TextField(
          key: const Key('activation-device-label'),
          controller: _deviceLabel,
          decoration: const InputDecoration(
            labelText: 'Device label',
            border: OutlineInputBorder(),
          ),
          textInputAction: TextInputAction.done,
          onSubmitted: (_) => _submitActivation(),
        ),
        const SizedBox(height: 20),
        FilledButton.icon(
          key: const Key('activate-device'),
          onPressed: _submitActivation,
          icon: const Icon(Icons.lock_open_outlined),
          label: const Text('Activate securely'),
        ),
        _safeFailure(),
      ],
    ),
  );

  Widget _activationRecovery() => _statusCard(
    icon: Icons.sync_lock_outlined,
    title: 'Activation outcome needs recovery',
    message:
        'TraderPro will retry the exact original activation transaction. Do not request or enter a different code yet.',
    actions: [
      FilledButton.icon(
        key: const Key('recover-activation'),
        onPressed: widget.controller.recoverActivation,
        icon: const Icon(Icons.refresh),
        label: const Text('Recover activation'),
      ),
    ],
  );

  Widget _loginForm() => AutofillGroup(
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const Icon(Icons.verified_user_outlined, size: 48),
        const SizedBox(height: 16),
        Text(
          'Workspace sign in',
          style: Theme.of(context).textTheme.headlineSmall,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: 24),
        TextField(
          key: const Key('login-workspace-code'),
          controller: _loginWorkspace,
          textCapitalization: TextCapitalization.characters,
          decoration: const InputDecoration(
            labelText: 'Workspace code',
            border: OutlineInputBorder(),
          ),
          textInputAction: TextInputAction.next,
        ),
        const SizedBox(height: 16),
        TextField(
          key: const Key('login-name'),
          controller: _login,
          enableSuggestions: false,
          autocorrect: false,
          decoration: const InputDecoration(
            labelText: 'Login',
            border: OutlineInputBorder(),
          ),
          textInputAction: TextInputAction.next,
        ),
        const SizedBox(height: 16),
        TextField(
          key: const Key('login-password'),
          controller: _password,
          obscureText: true,
          enableSuggestions: false,
          autocorrect: false,
          decoration: const InputDecoration(
            labelText: 'Password',
            border: OutlineInputBorder(),
          ),
          textInputAction: TextInputAction.done,
          onSubmitted: (_) => _submitLogin(),
        ),
        const SizedBox(height: 20),
        FilledButton.icon(
          key: const Key('sign-in'),
          onPressed: _submitLogin,
          icon: const Icon(Icons.login),
          label: const Text('Sign in'),
        ),
        _safeFailure(),
      ],
    ),
  );

  Widget _readyStatus() {
    final identity = widget.controller.identity;
    return _statusCard(
      icon: Icons.verified_user,
      title: 'Identity confirmed',
      message: identity == null
          ? 'The current identity is confirmed.'
          : '${identity.displayName}\n${identity.workspaceCode} • ${identity.role}\n${identity.deviceLabel}',
      actions: [
        OutlinedButton.icon(
          key: const Key('sign-out'),
          onPressed: () => widget.controller.logout(allSessions: false),
          icon: const Icon(Icons.logout),
          label: const Text('Sign out'),
        ),
        TextButton.icon(
          key: const Key('sign-out-all'),
          onPressed: () => widget.controller.logout(allSessions: true),
          icon: const Icon(Icons.phonelink_erase_outlined),
          label: const Text('Sign out all sessions'),
        ),
      ],
    );
  }

  Widget _offlineStatus() {
    final identity = widget.controller.identity;
    return _statusCard(
      icon: Icons.cloud_off_outlined,
      title: 'Online identity revalidation required',
      message:
          '${identity == null ? '' : '${identity.displayName}\n${identity.workspaceCode}\n\n'}No Commercial business operation is authorized while this state is active.',
      actions: [
        FilledButton.icon(
          key: const Key('retry-revalidation'),
          onPressed: widget.controller.revalidate,
          icon: const Icon(Icons.refresh),
          label: const Text('Retry revalidation'),
        ),
      ],
    );
  }

  Widget _blockingStatus({
    required IconData icon,
    required String title,
    required String message,
  }) => _statusCard(
    icon: icon,
    title: title,
    message: message,
    actions: const <Widget>[],
  );

  Widget _progress(String label) => Semantics(
    label: label,
    liveRegion: true,
    child: Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        const CircularProgressIndicator(),
        const SizedBox(height: 20),
        Text(label, textAlign: TextAlign.center),
      ],
    ),
  );

  Widget _statusCard({
    required IconData icon,
    required String title,
    required String message,
    required List<Widget> actions,
  }) => Column(
    mainAxisSize: MainAxisSize.min,
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      Icon(icon, size: 52),
      const SizedBox(height: 16),
      Text(
        title,
        style: Theme.of(context).textTheme.headlineSmall,
        textAlign: TextAlign.center,
      ),
      const SizedBox(height: 12),
      Text(message, textAlign: TextAlign.center),
      if (widget.controller.safeFailureCode != null) ...[
        const SizedBox(height: 12),
        Text(
          widget.controller.safeFailureCode!,
          textAlign: TextAlign.center,
          style: Theme.of(context).textTheme.bodySmall,
        ),
      ],
      for (final action in actions) ...[const SizedBox(height: 16), action],
    ],
  );

  Widget _safeFailure() {
    final code = widget.controller.safeFailureCode;
    return code == null
        ? const SizedBox.shrink()
        : Padding(
            padding: const EdgeInsets.only(top: 12),
            child: Semantics(
              liveRegion: true,
              child: Text(code, textAlign: TextAlign.center),
            ),
          );
  }

  Future<void> _submitActivation() async {
    FocusManager.instance.primaryFocus?.unfocus();
    await widget.controller.activate(
      workspaceCode: _activationWorkspace.text,
      activationCode: _activationCode.text,
      deviceLabel: _deviceLabel.text,
    );
    _activationCode.clear();
  }

  Future<void> _submitLogin() async {
    FocusManager.instance.primaryFocus?.unfocus();
    try {
      await widget.controller.login(
        workspaceCode: _loginWorkspace.text,
        login: _login.text,
        password: _password.text,
      );
    } finally {
      _password.clear();
    }
  }
}
