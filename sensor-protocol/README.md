# USB sensor protocol v1

One UTF-8/ASCII JSON object per newline at 115200 baud, maximum 1024 characters. Exact field definitions are in the approved Unity USB plan; fixtures here are canonical and copied byte-for-byte into Unity test assets. Complete valid packets only update the latest state; diagnostics/errors/duplicates never refresh freshness.

Commands: t/T (optionally newline) tares both unloaded load cells. Boot identity and tareId distinguish new calibration requests from stale queued data. Load N/age are null whenever that hand is invalid; percentage is relative input, not force. No sensor data is sent to a broker by this protocol.
