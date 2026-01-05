import xml.etree.ElementTree as ET
import glob
import os

results = []
for f in glob.glob('tests/*/bin/Debug/net10.0/TestResults/coverage.xml'):
    parts = f.replace('\\', '/').split('/')
    proj = parts[1].replace('.Tests', '')
    try:
        tree = ET.parse(f)
        root = tree.getroot()
        rate = float(root.get('line-rate', 0))
        results.append((proj, rate * 100))
    except Exception as e:
        print(f'Error: {f} - {e}')

results.sort(key=lambda x: x[1])
print("\nCoverage by Project (lowest first):")
print("=" * 50)
for proj, pct in results:
    bar = '#' * int(pct / 5)
    print(f'{proj:40} {pct:5.1f}% {bar}')
print("=" * 50)
