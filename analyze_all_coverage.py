#!/usr/bin/env python3
"""Analyze code coverage across all KeenEyes test projects."""

import xml.etree.ElementTree as ET
import os
from pathlib import Path
from collections import defaultdict

def analyze_coverage(coverage_file, project_filter=None):
    """Analyze a single coverage XML file."""
    if not os.path.exists(coverage_file):
        return None

    tree = ET.parse(coverage_file)
    root = tree.getroot()

    results = {}

    for pkg in root.findall('.//package'):
        pkg_name = pkg.get('name', '')

        # Skip test assemblies
        if 'Tests' in pkg_name:
            continue

        # Filter by project if specified
        if project_filter and project_filter not in pkg_name:
            continue

        classes = pkg.findall('.//class')
        for cls in classes:
            filename = cls.get('filename', '')
            lines = cls.findall('.//line')
            if not lines:
                continue

            total = len(lines)
            covered = sum(1 for l in lines if int(l.get('hits', 0)) > 0)

            # Group by assembly
            if pkg_name not in results:
                results[pkg_name] = {'total': 0, 'covered': 0, 'files': []}

            results[pkg_name]['total'] += total
            results[pkg_name]['covered'] += covered

            if total > covered:
                basename = os.path.basename(filename)
                uncovered = total - covered
                coverage_pct = (covered / total * 100) if total > 0 else 0
                results[pkg_name]['files'].append({
                    'name': basename,
                    'total': total,
                    'covered': covered,
                    'uncovered': uncovered,
                    'coverage': coverage_pct
                })

    return results

def main():
    base_path = Path('tests')
    coverage_files = list(base_path.glob('**/TestResults/coverage.xml'))

    # Aggregate results by assembly
    all_results = defaultdict(lambda: {'total': 0, 'covered': 0, 'files': []})

    for coverage_file in coverage_files:
        results = analyze_coverage(str(coverage_file))
        if results:
            for pkg_name, data in results.items():
                all_results[pkg_name]['total'] += data['total']
                all_results[pkg_name]['covered'] += data['covered']
                all_results[pkg_name]['files'].extend(data['files'])

    # Print summary by assembly
    print("=" * 80)
    print("KEENEYES CODE COVERAGE ANALYSIS")
    print("=" * 80)
    print()

    # Sort by coverage percentage
    sorted_assemblies = sorted(
        all_results.items(),
        key=lambda x: (x[1]['covered'] / x[1]['total'] * 100) if x[1]['total'] > 0 else 0
    )

    total_all = 0
    covered_all = 0

    print(f"{'Assembly':<45} {'Coverage':>10} {'Lines':>15}")
    print("-" * 75)

    for pkg_name, data in sorted_assemblies:
        if data['total'] == 0:
            continue

        coverage = (data['covered'] / data['total'] * 100)
        total_all += data['total']
        covered_all += data['covered']

        # Status marker based on coverage
        if coverage >= 95:
            status = "[OK]"
        elif coverage >= 85:
            status = "[--]"
        else:
            status = "[!!]"

        short_name = pkg_name.replace('KeenEyes.', '')
        print(f"{status} {short_name:<43} {coverage:>8.1f}% {data['covered']:>6}/{data['total']:<6}")

    print("-" * 75)
    overall = (covered_all / total_all * 100) if total_all > 0 else 0
    print(f"{'OVERALL':<45} {overall:>8.1f}% {covered_all:>6}/{total_all:<6}")
    print()

    # Show assemblies below 90% with their uncovered files
    print("=" * 80)
    print("ASSEMBLIES BELOW 90% - DETAILS")
    print("=" * 80)

    for pkg_name, data in sorted_assemblies:
        if data['total'] == 0:
            continue

        coverage = (data['covered'] / data['total'] * 100)
        if coverage >= 90:
            continue

        short_name = pkg_name.replace('KeenEyes.', '')
        print(f"\n{short_name}: {coverage:.1f}%")
        print("-" * 40)

        # Sort files by uncovered lines (most uncovered first)
        sorted_files = sorted(data['files'], key=lambda x: -x['uncovered'])

        # Deduplicate files (same file may appear multiple times)
        seen_files = {}
        for f in sorted_files:
            if f['name'] not in seen_files:
                seen_files[f['name']] = f
            else:
                seen_files[f['name']]['total'] += f['total']
                seen_files[f['name']]['covered'] += f['covered']
                seen_files[f['name']]['uncovered'] += f['uncovered']

        # Re-sort after deduplication
        sorted_files = sorted(seen_files.values(), key=lambda x: -x['uncovered'])

        # Show top 10 files with most uncovered lines
        for f in sorted_files[:10]:
            is_generated = '.g.cs' in f['name']
            marker = " [generated]" if is_generated else ""
            print(f"  {f['name']}: {f['coverage']:.1f}% ({f['uncovered']} uncovered){marker}")

    print()
    print("=" * 80)
    print("LEGEND: [OK] >= 95%  |  [--] 85-95%  |  [!!] < 85%")
    print("=" * 80)

if __name__ == '__main__':
    main()
