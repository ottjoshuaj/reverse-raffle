# NuGet Package Path Fix

## Problem
The project file had inconsistent NuGet package paths - some referenced `packages\` (local folder) while others referenced `..\packages\` (parent folder). This caused the error:

```
This project references NuGet package(s) that are missing on this computer.
The missing file is ..\packages\EntityFramework.6.5.1\build\EntityFramework.props.
```

## Root Cause
The packages are actually located in the **local** `packages\` folder (same directory as the .csproj file), but some references were looking for them in `..\packages\` (one directory up).

## What Was Fixed

Updated all package references in `ReverseRaffler.csproj` to use consistent paths:

### 1. EntityFramework Import (Line 3)
**Before:**
```xml
<Import Project="..\packages\EntityFramework.6.5.1\build\EntityFramework.props" ... />
```

**After:**
```xml
<Import Project="packages\EntityFramework.6.5.1\build\EntityFramework.props" ... />
```

### 2. Package References (Lines 85-160)
Fixed HintPath for all packages:
- ✅ EntityFramework.dll
- ✅ EntityFramework.SqlServer.dll
- ✅ Newtonsoft.Json.Bson.dll
- ✅ Owin.dll
- ✅ System.Net.Http.Formatting.dll
- ✅ System.Web.Http.dll
- ✅ System.Web.Http.Owin.dll

**Example Fix:**
```xml
<!-- Before -->
<HintPath>..\packages\EntityFramework.6.5.1\lib\net45\EntityFramework.dll</HintPath>

<!-- After -->
<HintPath>packages\EntityFramework.6.5.1\lib\net45\EntityFramework.dll</HintPath>
```

### 3. EntityFramework Targets (Lines 377-380)
**Before:**
```xml
<Error Condition="!Exists('..\packages\EntityFramework.6.5.1\build\EntityFramework.props')" ... />
<Error Condition="!Exists('..\packages\EntityFramework.6.5.1\build\EntityFramework.targets')" ... />
<Import Project="..\packages\EntityFramework.6.5.1\build\EntityFramework.targets" ... />
```

**After:**
```xml
<Error Condition="!Exists('packages\EntityFramework.6.5.1\build\EntityFramework.props')" ... />
<Error Condition="!Exists('packages\EntityFramework.6.5.1\build\EntityFramework.targets')" ... />
<Import Project="packages\EntityFramework.6.5.1\build\EntityFramework.targets" ... />
```

### 4. Cleanup
- ✅ Removed old `packages\EPPlus.6.2.10\` folder (no longer needed)
- ✅ Kept `packages\EPPlus.5.2.1\` (correct version)

## Verification

All required files are present:
```
packages/
├── EntityFramework.6.5.1/
│   ├── build/
│   │   ├── EntityFramework.props ✓
│   │   └── EntityFramework.targets ✓
│   └── lib/
│       └── net45/
│           ├── EntityFramework.dll ✓
│           └── EntityFramework.SqlServer.dll ✓
├── EPPlus.5.2.1/
│   └── lib/
│       └── net45/
│           └── EPPlus.dll ✓
├── CsvHelper.33.0.1/ ✓
├── Microsoft.Owin.4.2.2/ ✓
├── Newtonsoft.Json.13.0.3/ ✓
└── [all other packages] ✓
```

## How to Build Now

### Option 1: Visual Studio (Recommended)
1. Open `ReverseRaffler.sln` in Visual Studio
2. Right-click solution → **Restore NuGet Packages** (if prompted)
3. Press **F5** or click **Build** → **Build Solution**
4. Should build without errors! ✅

### Option 2: MSBuild Command Line
```bash
# In Windows Command Prompt or PowerShell
cd D:\reverse-raffle
msbuild ReverseRaffler.csproj /t:Restore
msbuild ReverseRaffler.csproj /p:Configuration=Debug
```

## Expected Result

✅ **No errors** about missing NuGet packages
✅ **No errors** about EntityFramework.props
✅ **No errors** about PresentationCore (EPPlus fixed)
✅ Project builds successfully
✅ Both refactorings work:
   - Canvas-based number board with overlays
   - Excel (.xlsx) import support

## If You Still Get Errors

### Clean Build
1. Close Visual Studio
2. Delete these folders:
   - `bin\`
   - `obj\`
3. Reopen Visual Studio
4. Restore NuGet packages
5. Build again

### Nuclear Option (Full Reset)
1. Close Visual Studio
2. Delete `packages\` folder
3. Delete `bin\` and `obj\` folders
4. Reopen Visual Studio
5. Right-click solution → **Restore NuGet Packages**
6. Build solution

This will re-download all packages fresh.

## Summary

All NuGet package references now point to the correct location (`packages\` folder in the project directory). The project should build successfully in Visual Studio.

**Status:** ✅ Fixed - Ready to Build
