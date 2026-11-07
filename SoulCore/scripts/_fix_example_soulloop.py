from pathlib import Path

p = Path(__file__).resolve().parents[1] / "SoulCore.Host" / "appsettings.Example.json"
t = p.read_text(encoding="utf-8")
old = (
    '  "SoulLoop": {\n'
    '    "Enabled": false, // RETIRED (BED-185) — Host forces false\n'
    '    "TickIntervalSeconds": 60,\n'
    '    "EpisodicRecallLimit": 3,\n'
    '    "ReflectionIntervalTicks": 5,\n'
    '    "ProactiveChatEnabled": false,\n'
    '    "ProactiveChatIntervalMinutes": 0,\n'
    '    "_note": "KILL SWITCH: Enabled=false by default. Scaffold want only — ProactiveChatEnabled stays false so phrase-bank pings never land in chat/SMS. No browser/MT4/email/file acts."\n'
    "  },"
)
new = (
    '  "SoulLoop": {\n'
    '    "Enabled": false,\n'
    '    "TickIntervalSeconds": 60,\n'
    '    "EpisodicRecallLimit": 3,\n'
    '    "ReflectionIntervalTicks": 5,\n'
    '    "ProactiveChatEnabled": false,\n'
    '    "ProactiveChatIntervalMinutes": 0,\n'
    '    "_note": "RETIRED (BED-185). Keep Enabled=false. Program binds this section as-is (does not force-override). Scaffold want only — ProactiveChatEnabled stays false so phrase-bank pings never land in chat/SMS."\n'
    "  },"
)
if old not in t:
    raise SystemExit("old SoulLoop block not found")
p.write_text(t.replace(old, new), encoding="utf-8")
print("ok")
