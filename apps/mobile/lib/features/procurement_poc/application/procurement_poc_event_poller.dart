// ignore_for_file: prefer_initializing_formals

import 'procurement_poc_feature.dart';
import 'procurement_poc_ports.dart';
import '../domain/procurement_poc_models.dart';

final class EventPollResult {
  const EventPollResult({
    required this.received,
    required this.appliedThroughCursor,
    required this.message,
  });

  final int received;
  final int appliedThroughCursor;
  final String message;
}

final class ProcurementPocEventPoller {
  ProcurementPocEventPoller({
    required ProcurementPocFeatureConfiguration feature,
    required ProcurementPocProfileStore profileStore,
    required ProcurementPocEventStore eventStore,
    required ProcurementPocApiFactory apiFactory,
    ProcurementPocClock clock = const SystemProcurementPocClock(),
  }) : _feature = feature,
       _profileStore = profileStore,
       _eventStore = eventStore,
       _apiFactory = apiFactory,
       _clock = clock;

  final ProcurementPocFeatureConfiguration _feature;
  final ProcurementPocProfileStore _profileStore;
  final ProcurementPocEventStore _eventStore;
  final ProcurementPocApiFactory _apiFactory;
  final ProcurementPocClock _clock;
  var _isPolling = false;

  Future<EventPollResult> pollOnce() async {
    if (!_feature.isEnabled) {
      return const EventPollResult(
        received: 0,
        appliedThroughCursor: 0,
        message: 'The development Procurement POC is disabled.',
      );
    }
    if (_isPolling) {
      return const EventPollResult(
        received: 0,
        appliedThroughCursor: 0,
        message: 'Event polling is already running.',
      );
    }
    _isPolling = true;
    PocDeviceProfile? profile;
    try {
      profile = await _profileStore.loadActiveProfile();
    } on Object {
      _isPolling = false;
      rethrow;
    }
    if (profile == null) {
      _isPolling = false;
      return const EventPollResult(
        received: 0,
        appliedThroughCursor: 0,
        message: 'Configure a development device profile first.',
      );
    }
    ProcurementPocApi? api;
    var received = 0;
    var cursor = 0;
    try {
      api = _apiFactory(profile);
      cursor = await _eventStore.loadEventCursor(profile);
      for (var pageNumber = 0; pageNumber < 100; pageNumber++) {
        final page = await api.readEvents(after: cursor, limit: 100);
        final ordered = List.of(page.events)
          ..sort((left, right) => left.sequence.compareTo(right.sequence));
        for (final event in ordered) {
          await _eventStore.applyMobileSyncEvent(
            profile: profile,
            event: event,
            receivedAtUtc: _clock.nowUtc(),
          );
          if (event.sequence > cursor) {
            cursor = event.sequence;
            received++;
          }
        }
        if (!page.hasMore) {
          await _eventStore.recordEventPollSuccess(
            profile: profile,
            nowUtc: _clock.nowUtc(),
          );
          return EventPollResult(
            received: received,
            appliedThroughCursor: cursor,
            message: 'MobileSync cursor polling completed.',
          );
        }
        if (page.nextCursor <= cursor && page.events.isEmpty) {
          throw const ProcurementPocApiException(
            code: 'POC_EVENT_PAGE_INVALID',
            message: 'The event page did not advance its cursor.',
            retryable: true,
            responseAmbiguous: false,
          );
        }
        cursor = page.nextCursor > cursor ? page.nextCursor : cursor;
      }
      throw const ProcurementPocApiException(
        code: 'POC_EVENT_PAGE_LIMIT',
        message: 'Event polling stopped at the safe page limit.',
        retryable: true,
        responseAmbiguous: false,
      );
    } on ProcurementPocApiException catch (error) {
      await _eventStore.recordEventPollFailure(
        profile: profile,
        errorCode: error.code,
        message: error.message,
        nowUtc: _clock.nowUtc(),
      );
      return EventPollResult(
        received: received,
        appliedThroughCursor: cursor,
        message: error.message,
      );
    } on Object {
      await _eventStore.recordEventPollFailure(
        profile: profile,
        errorCode: 'POC_EVENT_APPLY_FAILED',
        message:
            'An event could not be applied safely. The durable cursor was '
            'not advanced beyond it.',
        nowUtc: _clock.nowUtc(),
      );
      return EventPollResult(
        received: received,
        appliedThroughCursor: cursor,
        message:
            'Event application stopped safely without advancing beyond the '
            'invalid event.',
      );
    } finally {
      api?.dispose();
      _isPolling = false;
    }
  }
}
