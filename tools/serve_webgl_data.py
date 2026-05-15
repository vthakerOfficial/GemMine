#!/usr/bin/env python3
"""Serve the Unity WebGL build and receive Goldmine event JSONL."""

from __future__ import annotations

import argparse
import json
import mimetypes
import re
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, urlparse


SAFE_PART = re.compile(r"[^A-Za-z0-9_.-]+")


def sanitize_part(value: str, fallback: str) -> str:
    cleaned = SAFE_PART.sub("_", value.strip())
    cleaned = cleaned.strip("._")
    return cleaned or fallback


class GoldmineHandler(SimpleHTTPRequestHandler):
    data_root: Path

    def do_OPTIONS(self) -> None:
        self.send_response(204)
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "POST, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "Content-Type")
        self.end_headers()

    def do_POST(self) -> None:
        parsed = urlparse(self.path)
        if parsed.path != "/goldmine/events":
            self.send_error(404, "Unknown endpoint")
            return

        params = parse_qs(parsed.query)
        experiment = self._required_param(params, "experiment")
        participant = self._required_param(params, "participant")
        session = self._required_param(params, "session")

        if experiment is None or participant is None or session is None:
            self.send_error(400, "Missing experiment, participant, or session")
            return

        length_header = self.headers.get("Content-Length")
        if length_header is None:
            self.send_error(400, "Missing Content-Length")
            return

        try:
            length = int(length_header)
        except ValueError:
            self.send_error(400, "Invalid Content-Length")
            return

        body = self.rfile.read(length).decode("utf-8")
        lines = [line for line in body.splitlines() if line.strip()]

        try:
            for line in lines:
                json.loads(line)
        except json.JSONDecodeError as exc:
            self.send_error(400, f"Invalid JSONL payload: {exc}")
            return

        experiment_dir = sanitize_part(experiment, "Goldmine")
        participant_dir = sanitize_part(participant, "U001")
        session_dir = "session_" + sanitize_part(session, "0").removeprefix("session_")
        output_dir = self.data_root / experiment_dir / participant_dir / session_dir

        try:
            output_dir.mkdir(parents=True, exist_ok=True)
            with (output_dir / "events.jsonl").open("a", encoding="utf-8", newline="\n") as out:
                for line in lines:
                    out.write(line)
                    out.write("\n")
        except OSError as exc:
            self.send_error(500, f"Could not write events: {exc}")
            return

        self.send_response(200)
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Content-Type", "application/json")
        self.end_headers()
        self.wfile.write(b'{"ok":true}\n')

    @staticmethod
    def _required_param(params: dict[str, list[str]], name: str) -> str | None:
        values = params.get(name)
        if not values or values[0].strip() == "":
            return None
        return values[0]


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--host", default="localhost")
    parser.add_argument("--port", type=int, default=8000)
    parser.add_argument("--build-dir", default="WebGL_Build")
    parser.add_argument("--data-root", default="data")
    args = parser.parse_args()

    build_dir = Path(args.build_dir).resolve()
    if not build_dir.is_dir():
        raise SystemExit(f"WebGL build directory not found: {build_dir}")

    mimetypes.add_type("application/wasm", ".wasm")
    mimetypes.add_type("application/octet-stream", ".data")
    mimetypes.add_type("application/javascript", ".js")

    GoldmineHandler.data_root = Path(args.data_root).resolve()
    handler = partial(GoldmineHandler, directory=str(build_dir))
    server = ThreadingHTTPServer((args.host, args.port), handler)

    print(f"Serving {build_dir} at http://{args.host}:{args.port}/")
    print(f"Writing events under {GoldmineHandler.data_root}")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nStopping server")


if __name__ == "__main__":
    main()
