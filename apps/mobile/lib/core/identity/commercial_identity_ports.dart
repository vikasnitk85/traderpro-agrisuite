import 'commercial_identity_models.dart';

final class CommercialIdentityTransportRequest {
  const CommercialIdentityTransportRequest({
    required this.method,
    required this.relativePath,
    this.jsonBody,
    this.bearerAccessToken,
    this.idempotencyKey,
  });

  final String method;
  final String relativePath;
  final Map<String, Object?>? jsonBody;
  final String? bearerAccessToken;
  final String? idempotencyKey;
}

final class CommercialIdentityTransportResponse {
  const CommercialIdentityTransportResponse({
    required this.statusCode,
    required this.correlationId,
    required this.jsonBody,
    required this.headers,
  });

  final int statusCode;
  final String correlationId;
  final Map<String, Object?>? jsonBody;
  final Map<String, String> headers;
}

abstract interface class CommercialIdentityTransport {
  Future<CommercialIdentityTransportResponse> send(
    CommercialIdentityTransportRequest request,
  );

  void dispose();
}

enum CommercialCredentialStoreState {
  empty,
  valid,
  retired,
  missing,
  malformed,
  unavailable,
  invalidated,
  inconsistent,
}

final class CommercialCredentialRead<T> {
  const CommercialCredentialRead(this.state, {this.value});

  final CommercialCredentialStoreState state;
  final T? value;
}

abstract interface class CommercialIdentityCredentialStore {
  Future<CommercialCredentialRead<DeviceCredential>> readDeviceCredential();

  Future<void> replaceDeviceCredential(
    DeviceCredential credential, {
    String? bindingFingerprint,
  });

  Future<void> retireDeviceCredential({required DeviceId deviceId});

  Future<CommercialCredentialRead<RefreshCredential>> readRefreshCredential();

  Future<void> replaceRefreshCredential(RefreshCredential credential);

  Future<void> clearRefreshCredential();
}

abstract interface class CommercialActivationAttemptStore {
  Future<CommercialCredentialRead<CommercialActivationAttempt>>
  readActivationAttempt();

  Future<void> writeActivationAttempt(CommercialActivationAttempt attempt);

  Future<void> clearActivationAttempt();
}

abstract interface class CommercialIdentityBindingPort {
  Future<CommercialAccountBinding?> readBinding();

  Future<CommercialIdentitySnapshot?> readSnapshot();

  Future<void> bindOrMatch(
    CommercialAccountBinding binding,
    CommercialIdentitySnapshot snapshot,
  );
}
