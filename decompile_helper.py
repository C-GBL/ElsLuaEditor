#!/usr/bin/env python3
"""
Decompile helper for LuaEditor.
Called by the C# app: python decompile_helper.py <input.ljbc> <output.lua>
"""
import sys
import os
import traceback

_dir = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(_dir, "ljd"))

if len(sys.argv) != 3:
    print("Usage: decompile_helper.py <input.ljbc> <output.lua>", file=sys.stderr)
    sys.exit(1)

try:
    from ljd.tools import process_file, set_luajit_version
    set_luajit_version(20)
    process_file(sys.argv[1], sys.argv[2])
except Exception as e:
    print("--- Decompilation failed ---", file=sys.stderr)
    print(traceback.format_exc(), file=sys.stderr)
    sys.exit(1)
