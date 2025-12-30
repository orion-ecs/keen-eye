import xml.etree.ElementTree as ET
import os

coverage_path = 'tests/KeenEyes.Core.Tests/bin/Debug/net10.0/TestResults/coverage-core.xml'
tree = ET.parse(coverage_path)
root = tree.getroot()

packages = root.findall('.//package')
for pkg in packages:
    name = pkg.get('name')
    if 'KeenEyes.Core' in name and 'Tests' not in name:
        classes = pkg.findall('.//class')
        class_data = []
        for cls in classes:
            filename = cls.get('filename', '')
            lines = cls.findall('.//line')
            if not lines:
                continue
            total = len(lines)
            covered = sum(1 for l in lines if int(l.get('hits', 0)) > 0)
            uncovered = total - covered
            coverage = (covered / total * 100) if total > 0 else 0
            if uncovered > 0 and coverage < 100:
                basename = os.path.basename(filename)
                class_data.append((coverage, uncovered, basename))

        class_data.sort(key=lambda x: (x[0], -x[1]))
        print('Classes needing coverage improvement (sorted by coverage %, then by uncovered lines):')
        for cov, uncov, fname in class_data[:25]:
            print(f'  {fname}: {cov:.1f}% ({uncov} uncovered)')

# Also show overall stats
total_lines = 0
covered_lines = 0
for pkg in packages:
    name = pkg.get('name')
    if 'KeenEyes.Core' in name and 'Tests' not in name:
        for cls in pkg.findall('.//class'):
            for line in cls.findall('.//line'):
                total_lines += 1
                if int(line.get('hits', 0)) > 0:
                    covered_lines += 1

print(f'\nOverall: {covered_lines}/{total_lines} = {covered_lines/total_lines*100:.1f}%')
print(f'Need to cover {int(total_lines * 0.95) - covered_lines} more lines to reach 95%')
