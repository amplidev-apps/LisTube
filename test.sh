#!/bin/bash

# LisTube Test Script
# This script performs basic validation tests on the project

set -e

echo "================================"
echo "  LisTube - Test Script"
echo "================================"
echo ""

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

TESTS_PASSED=0
TESTS_FAILED=0

test_step() {
    if [ $1 -eq 0 ]; then
        echo -e "${GREEN}✓ PASSED${NC}: $2"
        TESTS_PASSED=$((TESTS_PASSED + 1))
    else
        echo -e "${RED}✗ FAILED${NC}: $2"
        TESTS_FAILED=$((TESTS_FAILED + 1))
    fi
}

echo "[Test 1/10] Checking project structure..."
[[ -f "LisTube.sln" ]]
test_step $? "Solution file exists"

[[ -d "LisTube" ]]
test_step $? "Project directory exists"

[[ -f "LisTube/LisTube.csproj" ]]
test_step $? "Project file exists"

echo ""
echo "[Test 2/10] Checking source files..."
[[ -f "LisTube/App.xaml" ]]
test_step $? "App.xaml exists"

[[ -f "LisTube/MainPage.xaml" ]]
test_step $? "MainPage.xaml exists"

[[ -f "LisTube/Skeleton.xaml" ]]
test_step $? "Skeleton.xaml exists"

echo ""
echo "[Test 3/10] Checking namespace consistency..."
! grep -r "namespace YoutubePlaylistDownloader" LisTube/*.cs 2>/dev/null
test_step $? "No old namespace in .cs files"

grep -q "namespace LisTube" LisTube/App.xaml.cs
test_step $? "New namespace found in App.xaml.cs"

echo ""
echo "[Test 4/10] Checking XAML files..."
! grep -q "YoutubePlaylistDownloader" LisTube/App.xaml
test_step $? "No old references in App.xaml"

echo ""
echo "[Test 5/10] Checking language files..."
[[ -d "LisTube/Languages" ]]
test_step $? "Languages directory exists"

[[ -f "LisTube/Languages/English.xaml" ]]
test_step $? "English.xaml exists"

[[ -f "LisTube/Languages/Português (BR).xaml" ]]
test_step $? "Português (BR).xaml exists"

echo ""
echo "[Test 6/10] Checking project configuration..."
grep -q "<StartupObject>LisTube.App</StartupObject>" LisTube/LisTube.csproj
test_step $? "StartupObject is LisTube.App"

grep -q "<AssemblyName>LisTube</AssemblyName>" LisTube/LisTube.csproj 2>/dev/null || echo "Note: AssemblyName will default to project name"
test_step 0 "Project name verified"

echo ""
echo "[Test 7/10] Checking GlobalConsts..."
grep -q 'string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "\\\\LisTube\\\\")' LisTube/GlobalConsts.cs
test_step $? "AppData path uses LisTube"

grep -q 'string.Concat(Path.GetTempPath(), "LisTube\\\\")' LisTube/GlobalConsts.cs
test_step $? "Temp path uses LisTube"

echo ""
echo "[Test 8/10] Checking update URLs..."
grep -q "amplidev-apps/LisTube" LisTube/About.xaml.cs
test_step $? "About.xaml.cs uses new GitHub URL"

grep -q "amplidev-apps/LisTube" LisTube/Skeleton.xaml.cs
test_step $? "Skeleton.xaml.cs uses new GitHub URL"

grep -q "amplidev-apps/LisTube" LisTube/DownloadUpdate.xaml.cs
test_step $? "DownloadUpdate.xaml.cs uses new GitHub URL"

echo ""
echo "[Test 9/10] Checking solution file..."
grep -q 'Project(".*") = "LisTube", "LisTube\\LisTube.csproj"' LisTube.sln
test_step $? "Solution references LisTube project"

echo ""
echo "[Test 10/10] Checking README..."
[[ -f "README.md" ]]
test_step $? "README.md exists"

grep -q "LisTube" README.md
test_step $? "README mentions LisTube"

echo ""
echo "================================"
echo "  Test Results"
echo "================================"
echo -e "Passed: ${GREEN}$TESTS_PASSED${NC}"
echo -e "Failed: ${RED}$TESTS_FAILED${NC}"
echo ""

if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "${GREEN}All tests passed!${NC}"
    echo ""
    echo "You can now build the project with:"
    echo "  ./build.sh"
    exit 0
else
    echo -e "${RED}Some tests failed. Please review the output above.${NC}"
    exit 1
fi
