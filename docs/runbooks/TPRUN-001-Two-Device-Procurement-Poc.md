# TPRUN-001: Two-Device Procurement POC Field Procedure

## Warning and prerequisites

This procedure is **Development Only**. Temporary Workspace and Device headers
are not authentication, bootstrap is not onboarding, and HTTP is permitted
only by the debug Android manifest. Do not use production data, credentials,
networks, or devices.

Prepare:

- one Windows development computer with this repository;
- PostgreSQL 18 and the reviewed migrations;
- the local development connection setting described in the root README,
  supplied privately in the current shell;
- two independent Android phones, or one phone and one emulator, on a trusted
  development network;
- USB debugging or another ordinary debug APK installation path.

Do not print a connection string into diagnostic output. Do not create a
firewall rule automatically.

## Network preparation

Find the computer's LAN IPv4 address:

```powershell
Get-NetIPAddress -AddressFamily IPv4 `
  | Where-Object {
      $_.IPAddress -notlike '127.*' -and
      $_.IPAddress -notlike '169.254.*'
    } `
  | Select-Object InterfaceAlias, IPAddress
```

A physical phone cannot reach the computer through `localhost`.

- Android emulator to computer: use `http://10.0.2.2:5000`.
- Physical phone to computer: use `http://<computer-LAN-IP>:5000`.
- Phone and computer must be on the same network, without client isolation.

Windows Firewall may require an explicit, temporary development inbound rule
for the chosen port. A developer or administrator must review and create that
rule manually, scope it to the trusted network, and remove it after testing.

## Field test

Run commands from the repository root unless a step says otherwise.

1. Start PostgreSQL without deleting or recreating its volume:

   ```powershell
   docker compose `
     --env-file .\infrastructure\docker\.env `
     -f .\infrastructure\docker\compose.yml `
     up -d postgres
   ```

2. Confirm the reviewed database migrations have been applied using the
   repository migration workflow. Do not reset PostgreSQL.

3. In the shell that will start the API, privately set the existing local
   `ConnectionStrings__TraderPro` value as documented in `README.md`. Do not
   echo it.

4. Start the backend with both spike flags and a LAN binding:

   ```powershell
   powershell -ExecutionPolicy Bypass `
     -File .\scripts\poc\start-two-device-procurement-backend.ps1 `
     -BindAddress 0.0.0.0 `
     -Port 5000
   ```

5. Verify the launcher reports Development Only and does not print a
   credential. It starts with `--no-launch-profile`, uses the requested URL
   binding, and restores every process environment variable when it exits.
   Leave this process running.

6. Resolve and build the debug POC APK:

   ```powershell
   flutter pub get --directory .\apps\mobile

   Push-Location .\apps\mobile
   flutter build apk `
     --debug `
     --dart-define=TRADERPRO_PROCUREMENT_POC=true
   Pop-Location
   ```

7. Install the resulting debug APK independently on Phone A and Phone B. Use
   `flutter devices` and a device-specific `flutter run` if convenient:

   ```powershell
   Push-Location .\apps\mobile
   flutter devices
   flutter run `
     --debug `
     --dart-define=TRADERPRO_PROCUREMENT_POC=true `
     -d <device-id>
   Pop-Location
   ```

   Repeat for the second device. Do not claim a two-device result unless both
   installations actually run.

   The app opens Drift schema version 3 non-destructively. Existing schema-1
   Task 4 bytes are retained; schema-2 POC rows are retained and bound to the
   existing active source/device. If an old POC row has no unambiguous active
   context, startup fails explicitly instead of guessing or deleting data.

8. Open the Development Only POC setup on one device. Enter the backend URL
   appropriate for that device and call **Bootstrap development IDs**.

9. Record the selectable bootstrap result in safe test notes. It contains IDs
   only: Workspace, company, branch, Operator Device, Owner Device, and setup
   code. It contains no password or token.

10. Configure Phone A with the common backend URL/Workspace ID, the returned
    Operator Device ID, and local display role **Operator**. Run
    **Test connection**.

11. Configure Phone B with the same backend URL/Workspace ID, the returned
    Owner Device ID, and local display role **Owner**. Run **Test connection**.
    Do not reuse Phone A's Device ID.

12. On Phone A, create a local Receiving Session. Confirm it shows a UUIDv7
    local ID, a temporary local reference, no cloud reference, and one queued
    Start operation.

13. Enable **Pause Automatic Sync**. This pauses POC timers only; it does not
    fake Android connectivity. Do not press Manual Sync Now during the offline
    queueing demonstration.

14. Record exactly five manual entries. For each, choose product reference,
    bag type reference, positive bag count, raw decimal text, precision 1-3,
    Standard/Floor/Ceiling, ManualSpike or TestScale source, and UTC capture
    time. The capture time must be valid ISO-8601 with explicit `Z` or zero
    offset. Missing, malformed, or offset-free input must show
    `POC_CAPTURE_TIMESTAMP_INVALID` and create no entry/outbox operation.
    Confirm the processed/display preview before saving.

15. Confirm Phone A shows five immutable entries, the exact six-decimal
    running total, and six queued operations: Start plus five entries.

16. Force-stop Phone A from Android settings. Restart it. Confirm the same
    session ID, five raw/processed entries, exact total, and queued operations
    reappear. No entry should be recreated.

17. Turn off **Pause Automatic Sync** or press the clearly separate
    **Manual Sync Now** action. In Sync Diagnostics, confirm Start completes
    first and returns a cloud reference, cloud version, lease ID/expiry state.
    Then confirm the five entries synchronize in local-sequence order using
    lease envelope metadata. When automatic sync is active, these fields and
    operation counts must update without pressing Manual Sync Now.

18. On Phone B, allow foreground polling to update the visible list, then use
    **Poll events now** and **Refresh server list** only as explicit checks.
    Open the read-only live view. Confirm cloud reference, editor device, lease
    expiry, entry count 5, exact total, cloud version, and at most five recent
    entries. Confirm there is no entry form and no Submit control.

19. On Phone A, press **Submit session**, then Manual Sync Now if necessary.
    Confirm cloud status becomes `SubmittedForReview`, the active lease clears,
    and heartbeat stops.

20. On Phone B, poll/refresh. Confirm **Approve** appears only while submitted.
    Press it once and verify the status becomes `Approved`.

21. Confirm **Finalize POC session** appears only while approved. Press it
    once. Record the displayed finalization ID, count, total, and cloud
    version. This is a POC completion, not a Purchase Bill.

22. Confirm **Retry Same Finalization** appears only because this active
    source/device has its own completed persisted Finalize command. Press it
    and confirm Sync Diagnostics retains the same command/idempotency key and
    the live view shows the same finalization ID and totals. A Finalized status
    learned only through events/list/live view must not show the replay action.
    Another Device ID cannot see, requeue, or transmit this command.

23. Capture safe diagnostics: local/cloud status, sequences, operation status
    counts, event cursor, stable error codes, cloud/finalization references,
    and exact totals. Do not capture connection strings, secrets, raw SQL,
    stack traces, or personal/production data. Stop the API launcher after the
    test; its process-scoped spike flags then disappear. Do not enable the
    flags in committed settings.

The enabled POC runtime, not an individual route, owns the database,
controller, timer, and lifecycle observer. Only Android/Flutter `resumed`
permits foreground cycles. Inactive, hidden, paused, and detached states stop
sync, polling, and heartbeat work; returning to resumed restarts it. Popping
and re-entering the POC route must not change that lifecycle behavior.

## Expected MobileSync event order

For exactly five entries, Phone B should observe:

```text
ReceivingSessionPocStarted
ReceivingEntryPocAccepted
ReceivingEntryPocAccepted
ReceivingEntryPocAccepted
ReceivingEntryPocAccepted
ReceivingEntryPocAccepted
ReceivingSessionPocSubmitted
ReceivingSessionPocApproved
ReceivingSessionPocFinalized
```

Global event sequence values may have gaps. Per-workspace events must remain
in ascending order. `ReceivingSessionPocStarted` must not contain `leaseId`.

## Optional response-loss evidence

Automated tests provide deterministic response-loss injection. Do not corrupt
a device database or edit payload JSON during a field run. If a natural
network interruption occurs after a request:

1. record only the safe operation ID/status;
2. force-stop and restart the app;
3. run Manual Sync Now;
4. confirm the same operation ID/payload/hash completes through
   `PreviouslyProcessed` or the stored original result.

## Troubleshooting

### Physical phone cannot reach the backend

- Do not use `localhost` on the phone.
- Confirm phone/computer are on the same LAN.
- Confirm the API is bound to `0.0.0.0` or the correct LAN interface.
- Try the computer LAN IPv4 address and port.
- Check VPN, guest Wi-Fi, access-point client isolation, and Windows Firewall.
- Emulator uses `10.0.2.2`, not the physical LAN rule.

### Windows Firewall blocks the port

Request a reviewed temporary inbound development rule for the selected TCP
port and trusted network profile. The launcher deliberately does not create a
rule. Remove the rule after testing.

### Wrong Workspace or Device ID

Recheck the bootstrap JSON. Phone A must use Operator Device ID; Phone B must
use Owner Device ID. IDs are canonical UUIDs and not authentication. Profile
identity cannot change while unresolved local work exists; finish or inspect
that work rather than bypassing the guard.

### Backend POC routes return 404

Confirm environment is Development and both flags are true in the launcher:

```text
TraderPro__Spikes__Enabled
TraderPro__Spikes__ProcurementPoc__Enabled
```

Production never maps these routes.

### HTTP cleartext is blocked

Use a debug build with the POC define. Cleartext is enabled only in the debug
manifest. Do not edit the main/release manifest to work around this error.

### Lease expired

The operator shows `RECEIVING_POC_LEASE_EXPIRED` and Needs Attention. Keep the
immutable operations. Task 6B does not steal, transfer, or silently replace a
lease/session.

### Stale expected version

Refresh Phone B's live view, inspect the stored Approve/Finalize command and
stable conflict code, and do not rewrite the existing command. A distinct
logical command requires an explicit reviewed action; it is not generated
automatically.

### Cursor does not advance

Check the stable polling error. A malformed known event intentionally prevents
advancement beyond that sequence. Confirm backend/Workspace source did not
change. Unknown future events should be stored as skipped and advance safely.

### Pending Needs Attention operation

Inspect its stable code and the earlier sequence for the same session. Later
operations remain blocked deliberately. Do not delete, supersede, rebuild, or
edit the immutable operation as a troubleshooting shortcut.
