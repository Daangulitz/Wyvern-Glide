# Legacy

Frozen originals from before the SOLID refactor of `/YawVR/Scripts/`, kept as a
rollback/diff reference — **not compiled into the active SDK path** in the sense that
nothing under `Scripts/` references these types; they're independent classes under the
`YawVR.Legacy` namespace so they can sit in the project without colliding with the
active ones.

| File | What it was | Superseded by |
|---|---|---|
| `YawTCPClient.cs` | The original TCP client (`YawVR.YawTCPClient`, `IYawTCPClientDelegate`) | `YawVR.TcpReliableConnection` (`Scripts/Transport/`), implementing `IYawTCPClientConnection`\* — same wire behavior, plus three bug fixes (disconnect-detection was inverted, writes weren't serialized, Nagle wasn't disabled). See the class doc-comment there for details. |
| `YawUDPClient.cs` | The original UDP client (`YawVR.YawUDPClient`, `IYawUDPClientDelegate`) | `YawVR.UdpDatagramChannel` (`Scripts/Transport/`), implementing `IDatagramChannel` — pure relocation, no behavior change. |

\* interface is actually named `IReliableConnection` — see `Scripts/Contracts/`.

## Why these two specifically

The audit that preceded this refactor found no scripts that were "dead code kept only
for backward compatibility" — every file in the old `Scripts/` was either a live
MonoBehaviour bound to a prefab (which can't be archived without breaking Unity's
serialization — see the refactor summary) or actively called by that live code.
`YawTCPClient`/`YawUDPClient` are the one case where the *responsibility* is fully
superseded by a new abstraction, even though nothing outside the old `YawController`
ever referenced these two classes directly. They're archived here rather than deleted
so there's a quick side-by-side reference during the transition, on top of git history.

## If you're picking this up later

- Don't import from here into `Scripts/`. If you find yourself wanting to, that's a
  sign the new abstraction (`IReliableConnection` / `IDatagramChannel`) is missing
  something the old concrete class did — fix the abstraction, not this.
- Safe to delete once you're confident in the new transport layer (a few weeks of the
  new code running against real hardware without a regression is a reasonable bar).
