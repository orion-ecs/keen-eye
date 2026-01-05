import xml.etree.ElementTree as ET
import sys

coverage_file = sys.argv[1] if len(sys.argv) > 1 else 'tests/KeenEyes.Network.Abstractions.Tests/bin/Debug/net10.0/TestResults/coverage.xml'
target_assembly = sys.argv[2] if len(sys.argv) > 2 else 'KeenEyes.Network.Abstractions'

tree = ET.parse(coverage_file)
root = tree.getroot()

for package in root.findall('.//package'):
    name = package.get('name', '')
    if name == target_assembly:
        print(f"\n{name} - Uncovered Classes/Methods:")
        print("=" * 70)
        for cls in package.findall('.//class'):
            cls_name = cls.get('name', '')
            filename = cls.get('filename', '').replace('\\', '/')

            # Count lines
            total = 0
            covered = 0
            uncovered_lines = []
            for line in cls.findall('.//line'):
                total += 1
                hits = int(line.get('hits', 0))
                if hits > 0:
                    covered += 1
                else:
                    uncovered_lines.append(line.get('number'))

            if total > 0:
                rate = (covered / total) * 100
                if rate < 100:
                    short_file = filename.split('/')[-1] if filename else ''
                    uncovered_count = total - covered
                    print(f"\n  {cls_name} ({short_file})")
                    print(f"    Coverage: {rate:.1f}% ({covered}/{total}) - {uncovered_count} lines uncovered")
                    if uncovered_lines and len(uncovered_lines) <= 20:
                        print(f"    Uncovered lines: {', '.join(uncovered_lines[:20])}")
