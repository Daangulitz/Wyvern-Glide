# Firmware TODO — YAW 3

Changes needed on the YAW 3 firmware side to fully support the new SDK design in
`/YawVR/Scripts/`. The SDK refactor (SOLID decomposition — see the accompanying
summary) intentionally shipped **structural-only**: same wire protocol, zero firmware
changes required for anything to keep working today. The items below are the gaps the
refactor surfaced but couldn't close from the SDK side alone, because closing them
means changing what firmware sends/expects on the wire.

Related doc for deeper background: `YAW3-transport-architecture-review.md` (full
TCP+UDP architecture review and the unified-UDP/RUDP evaluation) — a few items below
link back to specific sections there rather than repeating the reasoning.

---

## TCP reply framing

- [ ] **Add a length prefix or delimiter to variable-length TCP replies.**
  `CHECK_IN_ANS` (0x31) and `GET_STATE` (0xE5) replies are plain ASCII with no
  length field or terminator today; the SDK infers the reply's length from whatever
  one socket read happened to return. Two replies issued close together (e.g. the
  1Hz `GET_STATE`+`GET_TEMPS` heartbeat) can land in a single TCP segment and get
  concatenated, or one reply can be split across two reads. The SDK-side mitigations
  already in place (TCP_NODELAY, serialized writes — see `Scripts/Transport/TcpReliableConnection.cs`)
  reduce how often this happens but can't eliminate it without firmware cooperation.
  **Suggested fix:** prefix every TCP reply with a 2-byte big-endian length (payload
  length, not counting the length field itself), or terminate variable-length replies
  with a fixed delimiter byte that can't appear in the payload. Either way, tell us
  which so the SDK's read loop can be updated to match exactly.
  (Background: YAW3-transport-architecture-review.md §2.2, "No TCP message framing".)

- [ ] **Give `SET_POWER` (0x32) replies one canonical length, or a variant flag.**
  Today the reply body is either 5 bytes or a full 25-byte param dump depending on
  which request triggered it, and the SDK can only guess which by looking at how many
  bytes arrived — same coalescing/splitting exposure as above. **Suggested fix:** add
  a 1-byte variant/flag immediately after the command id so the SDK always knows the
  expected length before it starts reading the rest.

## Motion data reliability

- [ ] **Add a sequence number to the motion UDP packet**, and have firmware discard
  any received packet whose sequence number is not strictly newer than the last one
  applied. Motion is sent as `Y[...]P[...]R[...]V[...]F[...]` with no sequence
  number or timestamp today; UDP can reorder or duplicate packets, and a
  late-arriving stale packet overwriting a newer orientation reads as a physical
  jolt at the ~50Hz send rate. The LED command already carries a 16-bit counter for
  exactly this reason (see `udpLedCounter` in `Scripts/Protocol/CommandEncoder.cs`) —
  motion never got the same treatment. The SDK side is ready to add this the moment
  firmware can parse it: the one place it would need to change is
  `Scripts/Protocol/MotionDataCodec.cs` (implements `IMotionEncoder`), which is now
  isolated specifically so this kind of wire-format change doesn't touch anything else.
  **Suggested wire format:** either a 2-byte counter prefix (binary, like the LED
  command) or an additional `S[nnnnn]` field in the existing ASCII format — whichever
  is cheaper on your end; the SDK doesn't care which, just needs to know which.

- [ ] **Motion-loss watchdog.** If no motion packet is received for ~150ms, hold the
  last applied setpoint; if none for ~500ms, ramp to neutral/park rather than holding
  a stale pose indefinitely. Today firmware has no way to distinguish "game
  paused/crashed" from "still connected, orientation just isn't changing" — it will
  hold the last received pose forever if the game stops sending. This is a safety
  item (motion platform holding an arbitrary tilt with no upper bound on how long),
  not just a robustness one.

## Discovery

- [ ] **Confirm or fix the assumed UDP port in discovery replies.** The
  `YAWDEVICE;id;name;tcpPort;status` reply doesn't carry the device's actual UDP
  listening port — the SDK currently assumes it equals whatever port the discovery
  ping was broadcast on (see `YawDeviceDiscoveryService` in `Scripts/Connectivity/`,
  and `UdpMessageParser.TryParseDiscoveryReply`'s `assumedUdpPort` parameter). If the
  device's real UDP port can ever differ from the discovery port, add it as a 5th
  field: `YAWDEVICE;id;name;tcpPort;status;udpPort`, so the SDK doesn't have to guess.

## Housekeeping

- [ ] `CommandIds` (`Scripts/Protocol/CommandIds.cs`) still defines `RESET_PORTS`
  (0x01), `SET_SIMU_INPUT_PORT` (0x10), `SET_GAME_INPUT_PORT` (0x11),
  `SET_GAME_IP_ADDRESS` (0xA4), `SET_OUTPUT_PORT` (0x12), and `ERROR` (0xA5) — none
  of which the SDK currently sends or handles. These were audited as unused from the
  SDK side during the refactor and kept (not removed) purely as documentation of the
  ID space. If firmware expects any of these as part of a handshake the SDK isn't
  doing, flag it back to us — otherwise we'd like to know if they're safe to retire
  from the protocol entirely.

## Explicitly out of scope here

Whether to keep the TCP+UDP split at all, or move to a unified UDP transport with a
custom reliability layer on top, is a separate, larger decision — fully covered
(protocol design, ack/retransmit scheme, phased migration plan, effort estimate) in
`YAW3-transport-architecture-review.md`. This SDK refactor deliberately stayed
structural-only and doesn't assume or block either outcome; the abstractions it
introduced (`IReliableConnection`, `IDatagramChannel` in `Scripts/Contracts/`) are the
seam a future unified-transport implementation would plug into, whichever way that
decision goes.
