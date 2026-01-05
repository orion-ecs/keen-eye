#!/usr/bin/env python3
"""Analyze per-project code coverage for KeenEyes."""

import xml.etree.ElementTree as ET
import os
from pathlib import Path

# Map test projects to their primary source assembly
PROJECT_MAPPING = {
    'KeenEyes.Core.Tests': 'KeenEyes.Core',
    'KeenEyes.Physics.Tests': 'KeenEyes.Physics',
    'KeenEyes.UI.Tests': 'KeenEyes.UI',
    'KeenEyes.Animation.Tests': 'KeenEyes.Animation',
    'KeenEyes.Replay.Tests': 'KeenEyes.Replay',
    'KeenEyes.Parallelism.Tests': 'KeenEyes.Parallelism',
    'KeenEyes.Spatial.Tests': 'KeenEyes.Spatial',
    'KeenEyes.Particles.Tests': 'KeenEyes.Particles',
    'KeenEyes.Network.Tests': 'KeenEyes.Network',
    'KeenEyes.Debugging.Tests': 'KeenEyes.Debugging',
    'KeenEyes.Common.Tests': 'KeenEyes.Common',
    'KeenEyes.Logging.Tests': 'KeenEyes.Logging',
    'KeenEyes.Persistence.Tests': 'KeenEyes.Persistence',
    'KeenEyes.Generators.Tests': 'KeenEyes.Generators',
    'KeenEyes.Testing.Tests': 'KeenEyes.Testing',
    'KeenEyes.Graphics.Tests': 'KeenEyes.Graphics',
    'KeenEyes.Input.Abstractions.Tests': 'KeenEyes.Input.Abstractions',
    'KeenEyes.Network.Abstractions.Tests': 'KeenEyes.Network.Abstractions',
}

def analyze_project_coverage(coverage_file, target_assembly):
    """Analyze coverage for a specific assembly in a coverage file."""
    if not os.path.exists(coverage_file):
        return None

    tree = ET.parse(coverage_file)
    root = tree.getroot()

    total = 0
    covered = 0
    uncovered_files = []

    for pkg in root.findall('.//package'):
        pkg_name = pkg.get('name', '')

        # Only analyze the target assembly
        if target_assembly not in pkg_name or 'Tests' in pkg_name:
            continue

        for cls in pkg.findall('.//class'):
            filename = cls.get('filename', '')
            lines = cls.findall('.//line')
            if not lines:
                continue

            file_total = len(lines)
            file_covered = sum(1 for l in lines if int(l.get('hits', 0)) > 0)
            file_uncovered = file_total - file_covered

            total += file_total
            covered += file_covered

            if file_uncovered > 0:
                basename = os.path.basename(filename)
                is_generated = '.g.cs' in basename
                uncovered_files.append({
                    'name': basename,
                    'uncovered': file_uncovered,
                    'coverage': (file_covered / file_total * 100) if file_total > 0 else 0,
                    'generated': is_generated
                })

    if total == 0:
        return None

    # Deduplicate and aggregate files
    file_map = {}
    for f in uncovered_files:
        if f['name'] not in file_map:
            file_map[f['name']] = f
        else:
            file_map[f['name']]['uncovered'] += f['uncovered']

    return {
        'total': total,
        'covered': covered,
        'coverage': (covered / total * 100) if total > 0 else 0,
        'uncovered_files': sorted(file_map.values(), key=lambda x: -x['uncovered'])[:5]
    }

def main():
    print("=" * 80)
    print("KEENEYES PER-PROJECT CODE COVERAGE")
    print("=" * 80)
    print()

    results = []

    for test_project, source_assembly in PROJECT_MAPPING.items():
        coverage_file = f'tests/{test_project}/bin/Debug/net10.0/TestResults/coverage.xml'
        data = analyze_project_coverage(coverage_file, source_assembly)

        if data:
            results.append({
                'project': source_assembly.replace('KeenEyes.', ''),
                'data': data
            })

    # Sort by coverage percentage
    results.sort(key=lambda x: x['data']['coverage'])

    print(f"{'Project':<30} {'Coverage':>10} {'Lines':>15} {'Status':>8}")
    print("-" * 70)

    total_all = 0
    covered_all = 0

    for r in results:
        d = r['data']
        total_all += d['total']
        covered_all += d['covered']

        if d['coverage'] >= 95:
            status = "[OK]"
        elif d['coverage'] >= 85:
            status = "[--]"
        else:
            status = "[!!]"

        print(f"{r['project']:<30} {d['coverage']:>8.1f}% {d['covered']:>6}/{d['total']:<6} {status}")

    print("-" * 70)
    overall = (covered_all / total_all * 100) if total_all > 0 else 0
    print(f"{'TOTAL':<30} {overall:>8.1f}% {covered_all:>6}/{total_all:<6}")

    # Show details for projects below 95%
    print()
    print("=" * 80)
    print("PROJECTS BELOW 95% - TOP UNCOVERED FILES")
    print("=" * 80)

    for r in results:
        d = r['data']
        if d['coverage'] >= 95:
            continue

        print(f"\n{r['project']}: {d['coverage']:.1f}%")
        print("-" * 50)

        generated_uncovered = 0
        regular_uncovered = 0

        for f in d['uncovered_files']:
            marker = " [generated]" if f['generated'] else ""
            print(f"  {f['name']}: {f['uncovered']} uncovered{marker}")
            if f['generated']:
                generated_uncovered += f['uncovered']
            else:
                regular_uncovered += f['uncovered']

        if generated_uncovered > 0:
            print(f"  (Generated code: {generated_uncovered} lines)")

    print()
    print("=" * 80)
    print("LEGEND: [OK] >= 95%  |  [--] 85-95%  |  [!!] < 85%")
    print("=" * 80)

if __name__ == '__main__':
    main()
