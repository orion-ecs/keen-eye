#!/bin/bash
# Script to measure binary sizes for AOT vs JIT comparison
#
# Usage: ./measure-binary-sizes.sh

set -e

echo "=== Binary Size Comparison: AOT vs JIT ==="
echo ""

# Use the startup benchmark as the test application
cd StartupTimeBenchmark

# Clean previous builds
echo "Cleaning previous builds..."
rm -rf bin/ obj/
echo ""

# Measure JIT self-contained
echo "--- JIT Self-Contained ---"
dotnet publish -c Release -r linux-x64 --self-contained -p:BenchmarkMode=JIT > /dev/null 2>&1
JIT_SIZE=$(du -sb bin/Release/net10.0/linux-x64/publish/ | cut -f1)
JIT_SIZE_MB=$(echo "scale=2; $JIT_SIZE / 1024 / 1024" | bc)
echo "Size: $JIT_SIZE_MB MB"
echo ""

# Clean
rm -rf bin/ obj/

# Measure AOT
echo "--- AOT Self-Contained ---"
dotnet publish -c Release -r linux-x64 --self-contained -p:BenchmarkMode=AOT > /dev/null 2>&1
AOT_SIZE=$(du -sb bin/Release/net10.0/linux-x64/publish/ | cut -f1)
AOT_SIZE_MB=$(echo "scale=2; $AOT_SIZE / 1024 / 1024" | bc)
echo "Size: $AOT_SIZE_MB MB"
echo ""

# Calculate difference
DIFF=$(echo "scale=2; ($JIT_SIZE - $AOT_SIZE) / $JIT_SIZE * 100" | bc)
echo "--- Summary ---"
echo "JIT:  $JIT_SIZE_MB MB"
echo "AOT:  $AOT_SIZE_MB MB"
echo "Difference: $DIFF% smaller for AOT"
echo ""

# Clean up
rm -rf bin/ obj/

echo "Done!"
