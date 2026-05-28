#!/usr/bin/env python3
"""
Extract embedded .NET assemblies from the game executable,
name them by their module/assembly name, and copy to GameDll/.

Usage:
    python3 csharp/extract_dlls.py [--exe PATH] [--out DIR] [--dry-run]

Relies on the Module table (always table 0x00 in #~ metadata stream)
to get the module name, strips .dll/.exe suffix for assembly name.
"""

import os, sys, struct, shutil

PE32_MAGIC = 0x10B
PE32_PLUS_MAGIC = 0x20B
CLI_HDR_IDX = 14

# Assemblies that ship with .NET Framework / Mono runtime and
# need NOT be copied to GameDll (resolved from system at build time).
SYSTEM_PREFIXES = ('System.', 'Microsoft.Bcl.')
SYSTEM_NAMES = frozenset({
    'mscorlib', 'netstandard',
    'Microsoft.CSharp',
    'System',  # bare System.dll
})


def is_system_assembly(name):
    """Return True if the assembly is a system/Framework DLL."""
    if name in SYSTEM_NAMES:
        return True
    for p in SYSTEM_PREFIXES:
        if name.startswith(p):
            return True
    return False


def rva_to_offset(sections, rva):
    for va, vs, ra, rs in sections:
        if va <= rva < va + vs:
            return rva - va + ra
    return None


def parse_sections(data, pe_sig_off):
    num = struct.unpack_from('<H', data, pe_sig_off + 6)[0]
    opt_sz = struct.unpack_from('<H', data, pe_sig_off + 20)[0]
    tbl_off = pe_sig_off + 4 + 20 + opt_sz
    secs = []
    for i in range(num):
        s = tbl_off + i * 40
        va = struct.unpack_from('<I', data, s + 12)[0]
        vs = struct.unpack_from('<I', data, s + 8)[0]
        ra = struct.unpack_from('<I', data, s + 20)[0]
        rs = struct.unpack_from('<I', data, s + 16)[0]
        secs.append((va, vs, ra, rs))
    return secs


def pe_file_size(data, pe_sig_off):
    num = struct.unpack_from('<H', data, pe_sig_off + 6)[0]
    opt_sz = struct.unpack_from('<H', data, pe_sig_off + 20)[0]
    tbl_off = pe_sig_off + 4 + 20 + opt_sz
    max_end = 0
    for i in range(num):
        ra = struct.unpack_from('<I', data, tbl_off + i * 40 + 20)[0]
        rs = struct.unpack_from('<I', data, tbl_off + i * 40 + 16)[0]
        max_end = max(max_end, ra + rs)
    return max_end


def has_cli_header(data, pe_sig_off):
    magic = struct.unpack_from('<H', data, pe_sig_off + 24)[0]
    if magic == PE32_MAGIC:
        num_dir = struct.unpack_from('<I', data, pe_sig_off + 24 + 92)[0]
        cli_pos = pe_sig_off + 24 + 96 + CLI_HDR_IDX * 8
    else:
        num_dir = struct.unpack_from('<I', data, pe_sig_off + 24 + 108)[0]
        cli_pos = pe_sig_off + 24 + 112 + CLI_HDR_IDX * 8
    if CLI_HDR_IDX >= num_dir:
        return False
    return struct.unpack_from('<I', data, cli_pos)[0] != 0


def read_module_name(pe_data):
    """
    Parse .NET metadata to extract module name (from Module table, 0x00).
    Returns assembly name (module name stripped of .dll/.exe) or None.
    """
    if len(pe_data) < 0x200:
        return None

    pe_sig_off = struct.unpack_from('<I', pe_data, 0x3C)[0]
    if pe_data[pe_sig_off:pe_sig_off + 4] != b'PE\x00\x00':
        return None

    sections = parse_sections(pe_data, pe_sig_off)

    magic = struct.unpack_from('<H', pe_data, pe_sig_off + 24)[0]
    if magic == PE32_MAGIC:
        num_dir = struct.unpack_from('<I', pe_data, pe_sig_off + 24 + 92)[0]
        cli_pos = pe_sig_off + 24 + 96 + CLI_HDR_IDX * 8
    else:
        num_dir = struct.unpack_from('<I', pe_data, pe_sig_off + 24 + 108)[0]
        cli_pos = pe_sig_off + 24 + 112 + CLI_HDR_IDX * 8

    if CLI_HDR_IDX >= num_dir:
        return None
    cli_rva = struct.unpack_from('<I', pe_data, cli_pos)[0]
    if cli_rva == 0:
        return None

    cli_off = rva_to_offset(sections, cli_rva)
    if cli_off is None:
        return None

    md_rva = struct.unpack_from('<I', pe_data, cli_off + 8)[0]
    md_off = rva_to_offset(sections, md_rva)
    if md_off is None:
        return None

    # Metadata root
    sig = struct.unpack_from('<I', pe_data, md_off)[0]
    if sig != 0x424A5342:
        return None

    pos = md_off + 4
    pos += 4  # MajorVersion + MinorVersion
    pos += 4  # Reserved
    ver_len = struct.unpack_from('<I', pe_data, pos)[0]; pos += 4
    pos += ver_len; pos = (pos + 3) & ~3
    pos += 2  # Flags
    num_streams = struct.unpack_from('<H', pe_data, pos)[0]; pos += 2

    streams = {}
    for _ in range(num_streams):
        str_off = struct.unpack_from('<I', pe_data, pos)[0]
        str_size = struct.unpack_from('<I', pe_data, pos + 4)[0]
        pos += 8
        name_start = pos
        while pe_data[pos] != 0:
            pos += 1
        name = pe_data[name_start:pos].decode('ascii', errors='replace')
        pos += 1
        pos = (pos + 3) & ~3
        streams[name] = (str_off, str_size)

    if '#~' not in streams or '#Strings' not in streams:
        return None

    tbl_rel, tbl_sz = streams['#~']
    str_rel, str_sz = streams['#Strings']
    tbl_start = md_off + tbl_rel
    str_base = md_off + str_rel

    # Parse #~ header
    pos = tbl_start
    pos += 4  # Reserved
    pos += 2  # MajorVersion + MinorVersion
    heap_sizes = pe_data[pos]; pos += 1
    str_idx_sz = 4 if (heap_sizes & 1) else 2
    pos += 1  # Reserved
    valid = struct.unpack_from('<Q', pe_data, pos)[0]; pos += 8
    pos += 8  # Sorted

    # Row counts: skip through them
    num_present = bin(valid).count('1')
    pos += num_present * 4

    # Module table (0x00) is always the first table when present.
    if not (valid & 1):
        return None  # Module table must exist

    # Module row: Generation(I2) + Name(str) + Mvid(guid) + EncId(guid) + EncBaseId(guid)
    # guid idx size: 2 if heap_sizes bit 1 = 0, else 4
    guid_idx_sz = 4 if (heap_sizes & 2) else 2

    # Name at offset: I2(2) from start
    name_index_off = pos + 2

    # Read Name string index
    if str_idx_sz == 4:
        name_idx = struct.unpack_from('<I', pe_data, name_index_off)[0]
    else:
        name_idx = struct.unpack_from('<H', pe_data, name_index_off)[0]

    if name_idx == 0 or name_idx >= str_sz:
        return None

    # Read null-terminated string from #Strings
    addr = str_base + name_idx
    if addr >= len(pe_data):
        return None
    end = addr
    while end < len(pe_data) and pe_data[end] != 0:
        end += 1
    module_name = pe_data[addr:end].decode('utf-8', errors='replace').strip()
    if not module_name:
        return None

    # Strip .dll or .exe extension
    for ext in ('.dll', '.exe', '.DLL', '.EXE'):
        if module_name.lower().endswith(ext.lower()):
            module_name = module_name[:-len(ext)]
            break

    return module_name


def find_embedded_pe(data):
    """Find all embedded PE files in the binary."""
    results = []
    pos = 0
    while True:
        idx = data.find(b'MZ', pos)
        if idx == -1:
            break
        if idx + 0x40 >= len(data):
            pos = idx + 2; continue
        pe_off = struct.unpack_from('<I', data, idx + 0x3C)[0]
        pe_addr = idx + pe_off
        if pe_addr + 4 >= len(data) or data[pe_addr:pe_addr + 4] != b'PE\x00\x00':
            pos = idx + 2; continue
        magic = struct.unpack_from('<H', data, pe_addr + 24)[0]
        if magic not in (PE32_MAGIC, PE32_PLUS_MAGIC):
            pos = idx + 2; continue
        sz = struct.unpack_from('<I', data, pe_addr + 0x50)[0]
        if 2048 <= sz <= 256 * 1024 * 1024:
            results.append(idx)
        pos = idx + 2
    return results


def main():
    import argparse
    parser = argparse.ArgumentParser(description="Extract .NET assemblies from game exe")
    parser.add_argument("--exe", default=None, help="Path to game exe")
    parser.add_argument("--out", default=None, help="Output directory (GameDll/)")
    parser.add_argument("--dry-run", action="store_true", help="List only")
    parser.add_argument("--include-system", action="store_true",
                        help="Also copy system/Framework assemblies (e.g. System.*, mscorlib)")
    args = parser.parse_args()

    # Walk up from script dir to find project root (by CLAUDE.md)
    script_dir = os.path.dirname(os.path.abspath(__file__))
    root = script_dir
    while True:
        if os.path.exists(os.path.join(root, 'CLAUDE.md')):
            break
        parent = os.path.dirname(root)
        if parent == root:
            root = script_dir
            break
        root = parent
    exe_path = args.exe or os.path.join(root, "GameDir", "b1", "Binaries", "Win64",
                                         "b1-Win64-Shipping.exe")
    out_dir = args.out or os.path.join(root, "csharp", "B1CSharpLoader", "GameDll")

    if not os.path.exists(exe_path):
        print(f"EXE not found: {exe_path}")
        sys.exit(1)

    print(f"EXE: {exe_path}")
    print(f"OUT: {out_dir}")
    print()

    with open(exe_path, 'rb') as f:
        exe_data = f.read()

    offsets = find_embedded_pe(exe_data)
    print(f"Found {len(offsets)} embedded PE candidates")
    print()

    import tempfile
    found = []

    with tempfile.TemporaryDirectory() as tmp:
        for idx, offset in enumerate(offsets):
            chunk = exe_data[offset:]
            pe_sig_off = struct.unpack_from('<I', chunk, 0x3C)[0]
            if chunk[pe_sig_off:pe_sig_off + 4] != b'PE\x00\x00':
                continue

            if not has_cli_header(chunk, pe_sig_off):
                continue

            file_sz = pe_file_size(chunk, pe_sig_off)
            if file_sz < 2048:
                continue

            pe_data = exe_data[offset:offset + file_sz]

            name = read_module_name(pe_data)
            if not name or not name.strip():
                continue

            # Save to temp
            dll_path = os.path.join(tmp, f"{name}.dll")
            with open(dll_path, 'wb') as f:
                f.write(pe_data)

            found.append((name.strip(), offset, file_sz))

        if not found:
            print("No .NET assemblies identified.")
            return

        # Deduplicate by name (keep largest)
        deduped = {}
        for name, off, sz in found:
            if name not in deduped or sz > deduped[name][1]:
                deduped[name] = (off, sz)
        found = [(n, o, s) for n, (o, s) in deduped.items()]

        # Split into game vs system
        game = [(n, o, s) for n, o, s in found if not is_system_assembly(n)]
        system = [(n, o, s) for n, o, s in found if is_system_assembly(n)]

        # Print game assemblies
        print(f"Game assemblies to copy ({len(game)}):")
        print(f"{'Assembly':<45} {'Offset':>12} {'Size':>10}")
        print("-" * 69)
        for name, off, sz in sorted(game, key=lambda x: x[0].lower()):
            print(f"{name:<45} 0x{off:08x} {sz:>10,}")
        if not args.include_system:
            print(f"\nFiltered out system assemblies ({len(system)}):")
            for name, off, sz in sorted(system, key=lambda x: x[0].lower()):
                print(f"  SKIP {name:45s} (system)")
        print(f"\nTotal: {len(game)} game + {len(system)} system = {len(found)} unique .NET assemblies")

        if args.dry_run:
            return

        # Decide which list to copy
        to_copy = found if args.include_system else game

        os.makedirs(out_dir, exist_ok=True)
        copied = skipped = filtered = 0
        for name, offset, sz in sorted(to_copy, key=lambda x: x[0].lower()):
            src = os.path.join(tmp, f"{name}.dll")
            dst = os.path.join(out_dir, f"{name}.dll")
            if os.path.exists(dst):
                if os.path.getsize(dst) == os.path.getsize(src):
                    skipped += 1
                    continue
            shutil.copyfile(src, dst)
            print(f"  COPY {name}.dll")
            copied += 1

        if not args.include_system:
            filtered = len(found) - len(to_copy)

        print(f"\nDone: {copied} copied, {skipped} skipped")
        if filtered:
            print(f"      {filtered} system assemblies filtered out (use --include-system to copy)")
        if copied > 0:
            print(f"Output: {out_dir}")


if __name__ == '__main__':
    main()
