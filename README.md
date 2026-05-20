**How to run game and get results:**


**If on windows do:** 
cd "GemMine_Unity_code_2025-08-05"
python -u tools\serve_webgl_data.py --host 127.0.0.1 --port 8000 --build-dir WebGL_Build --data-root data
**If on mac do:**
cd "GemMine_Unity_code_2025-08-05"
python -u tools/serve_webgl_data.py --host 127.0.0.1 --port 8000 --build-dir WebGL_Build --data-root data

Above command will make game run, head here to play it:
http://127.0.0.1:8000/

As game is played python script records results here: 
data\Goldmine\<participant>\session_<number>\events.jsonl

To verify it is recording (in powershell) do:
Get-ChildItem -Recurse data\Goldmine -Filter events.jsonl | Sort-Object LastWriteTime -Descending | Select-Object -First 5 FullName,Length,LastWriteTime
