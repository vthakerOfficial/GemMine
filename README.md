**How to run game and get results:**

**If on Windows, do:**

```powershell
cd "GemMine_Unity_code_2025-08-05"
python -u tools\serve_webgl_data.py --host 127.0.0.1 --port 8000 --build-dir WebGL_Build --data-root data
```

**If on Mac, do:**

```bash
cd "GemMine_Unity_code_2025-08-05"
python -u tools/serve_webgl_data.py --host 127.0.0.1 --port 8000 --build-dir WebGL_Build --data-root data
```

The command above will make the game run. Head here to play it:

```text
http://127.0.0.1:8000/
```

As the game is played, the Python script records results here:

```text
data\Goldmine\<participant>\session_<number>\events.jsonl
```

To verify it is recording in PowerShell, do:

```powershell
Get-ChildItem -Recurse data\Goldmine -Filter events.jsonl | Sort-Object LastWriteTime -Descending | Select-Object -First 5 FullName,Length,LastWriteTime
```
