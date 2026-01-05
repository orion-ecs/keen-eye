import xml.etree.ElementTree as ET
import glob
import os

for f in glob.glob('tests/*/bin/Debug/net10.0/TestResults/coverage.xml'):
    parts = f.replace('\\', '/').split('/')
    test_proj = parts[1]
    print(f"\n{test_proj}:")
    print("-" * 60)
    try:
        tree = ET.parse(f)
        root = tree.getroot()
        for package in root.findall('.//package'):
            name = package.get('name', 'unknown')
            rate = float(package.get('line-rate', 0)) * 100
            if 'KeenEyes' in name and '.Tests' not in name:
                # Get line counts
                lines_valid = 0
                lines_covered = 0
                for cls in package.findall('.//class'):
                    for line in cls.findall('.//line'):
                        lines_valid += 1
                        if int(line.get('hits', 0)) > 0:
                            lines_covered += 1
                if lines_valid > 0:
                    actual_rate = (lines_covered / lines_valid) * 100
                    print(f"  {name:45} {actual_rate:5.1f}% ({lines_covered}/{lines_valid})")
    except Exception as e:
        print(f"  Error: {e}")
