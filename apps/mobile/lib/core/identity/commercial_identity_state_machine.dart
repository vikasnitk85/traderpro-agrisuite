import 'commercial_authentication_status.dart';
import 'commercial_identity_ports.dart';

final class CommercialIdentityStartupFacts {
  const CommercialIdentityStartupFacts({
    required this.hasBinding,
    required this.hasSnapshot,
    required this.deviceState,
    required this.refreshState,
  });

  final bool hasBinding;
  final bool hasSnapshot;
  final CommercialCredentialStoreState deviceState;
  final CommercialCredentialStoreState refreshState;
}

final class CommercialIdentityStateMachine {
  const CommercialIdentityStateMachine();

  CommercialAuthenticationStatus classifyStartup(
    CommercialIdentityStartupFacts facts,
  ) {
    if (_isFailClosed(facts.deviceState) || _isFailClosed(facts.refreshState)) {
      return CommercialAuthenticationStatus.lockedFailClosed;
    }
    if (facts.deviceState == CommercialCredentialStoreState.retired) {
      return CommercialAuthenticationStatus.deviceRevoked;
    }
    if (facts.deviceState == CommercialCredentialStoreState.empty) {
      return facts.hasBinding
          ? CommercialAuthenticationStatus.lockedFailClosed
          : CommercialAuthenticationStatus.deviceUnregistered;
    }
    if (facts.deviceState != CommercialCredentialStoreState.valid) {
      return CommercialAuthenticationStatus.lockedFailClosed;
    }
    if (facts.refreshState == CommercialCredentialStoreState.empty) {
      return CommercialAuthenticationStatus.deviceRegisteredNoSession;
    }
    if (facts.refreshState == CommercialCredentialStoreState.valid) {
      return CommercialAuthenticationStatus.refreshing;
    }
    return CommercialAuthenticationStatus.lockedFailClosed;
  }

  CommercialAuthenticationStatus classifyOffline({
    required bool hasBinding,
    required bool hasSnapshot,
  }) => hasBinding && hasSnapshot
      ? CommercialAuthenticationStatus.boundOfflineRevalidationRequired
      : CommercialAuthenticationStatus.authRequired;

  static bool _isFailClosed(CommercialCredentialStoreState state) =>
      state == CommercialCredentialStoreState.missing ||
      state == CommercialCredentialStoreState.malformed ||
      state == CommercialCredentialStoreState.unavailable ||
      state == CommercialCredentialStoreState.invalidated ||
      state == CommercialCredentialStoreState.inconsistent;
}
