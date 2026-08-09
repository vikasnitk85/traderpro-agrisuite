enum CommercialSecureStartupStatus { ready, unavailable, closed }

final class CommercialSecureStartupState {
  const CommercialSecureStartupState._({
    required this.status,
    this.safeFailureCode,
  });

  const CommercialSecureStartupState.ready()
    : this._(status: CommercialSecureStartupStatus.ready);

  const CommercialSecureStartupState.unavailable(String safeFailureCode)
    : this._(
        status: CommercialSecureStartupStatus.unavailable,
        safeFailureCode: safeFailureCode,
      );

  const CommercialSecureStartupState.closed()
    : this._(status: CommercialSecureStartupStatus.closed);

  final CommercialSecureStartupStatus status;
  final String? safeFailureCode;

  bool get isReady => status == CommercialSecureStartupStatus.ready;
}
