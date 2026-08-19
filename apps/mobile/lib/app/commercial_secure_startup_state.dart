import 'commercial_identity_controller.dart';

enum CommercialSecureStartupStatus { ready, unavailable, closed }

final class CommercialSecureStartupState {
  const CommercialSecureStartupState._({
    required this.status,
    this.safeFailureCode,
    this.identityController,
  });

  const CommercialSecureStartupState.ready([
    CommercialIdentityController? identityController,
  ]) : this._(
         status: CommercialSecureStartupStatus.ready,
         identityController: identityController,
       );

  const CommercialSecureStartupState.unavailable(String safeFailureCode)
    : this._(
        status: CommercialSecureStartupStatus.unavailable,
        safeFailureCode: safeFailureCode,
      );

  const CommercialSecureStartupState.closed()
    : this._(status: CommercialSecureStartupStatus.closed);

  final CommercialSecureStartupStatus status;
  final String? safeFailureCode;
  final CommercialIdentityController? identityController;

  bool get isReady => status == CommercialSecureStartupStatus.ready;
}
