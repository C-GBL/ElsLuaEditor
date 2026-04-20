#!/usr/bin/env python3
"""
Compile helper for LuaEditor.
Called by the C# app: python compile_helper.py <input.lua> <output.ljbc>

Uses luajit.exe string.dump to produce LuaJIT 2.0 bytecode, then patches
the 4-byte header from standard \x1bLJ\x01 to Elsword's \x1bKL\x81.
"""
import sys
import os
import subprocess
import tempfile
import traceback

_DUMPER = r"""
local src, dst = arg[1], arg[2]
local f, err = loadfile(src)
if not f then
    io.stderr:write(tostring(err) .. "\n")
    os.exit(1)
end
local bc = string.dump(f)
local fout = assert(io.open(dst, "wb"))
fout:write(bc)
fout:close()
"""

def main():
    if len(sys.argv) != 3:
        print("Usage: compile_helper.py <input.lua> <output.ljbc>", file=sys.stderr)
        sys.exit(1)

    src_path = sys.argv[1]
    out_path  = sys.argv[2]

    _dir    = os.path.dirname(os.path.abspath(__file__))
    luajit  = os.path.join(_dir, "luajit.exe")

    dumper_path = None
    tmp_out     = None
    try:
        with tempfile.NamedTemporaryFile(suffix='.lua', mode='w', delete=False, encoding='utf-8') as tf:
            dumper_path = tf.name
            tf.write(_DUMPER)

        with tempfile.NamedTemporaryFile(suffix='.ljbc', delete=False) as tf:
            tmp_out = tf.name

        result = subprocess.run(
            [luajit, dumper_path, src_path, tmp_out],
            capture_output=True, text=True, timeout=15
        )

        if result.returncode != 0:
            msg = (result.stderr.strip() or result.stdout.strip()
                   or "Compilation failed (no output)")
            print("--- Compilation failed ---", file=sys.stderr)
            print(msg, file=sys.stderr)
            sys.exit(1)

        with open(tmp_out, 'rb') as f:
            bytecode = bytearray(f.read())

        if len(bytecode) < 4:
            print("Compiler produced no output.", file=sys.stderr)
            sys.exit(1)

        if bytecode[0:3] != b'\x1bLJ':
            print(f"Unexpected bytecode magic: {bytes(bytecode[0:4]).hex()}", file=sys.stderr)
            sys.exit(1)

        # Patch header: \x1bLJ\x01 → \x1bKL\x81
        bytecode[1] = 0x4b  # 'K'
        bytecode[2] = 0x4c  # 'L'
        bytecode[3] = 0x81  # Elsword version

        with open(out_path, 'wb') as f:
            f.write(bytecode)

    except Exception:
        print("--- Compilation failed ---", file=sys.stderr)
        print(traceback.format_exc(), file=sys.stderr)
        sys.exit(1)
    finally:
        for p in (dumper_path, tmp_out):
            try:
                if p and os.path.exists(p):
                    os.unlink(p)
            except Exception:
                pass

if __name__ == '__main__':
    main()
