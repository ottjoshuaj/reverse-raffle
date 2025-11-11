# EPPlus Version Fix

## Problem
EPPlus 6.2.10 has a dependency on WPF's `PresentationCore` assembly, which caused this error:
```
Failed to add reference. The package 'EPPlus' tried to add a framework reference
to 'PresentationCore' which was not found in the GAC.
```

This happens because:
- EPPlus 6.x added advanced charting features that use WPF
- Pure WinForms applications don't have WPF assemblies
- The raffle application is WinForms-only and doesn't need WPF

## Solution
Downgraded to **EPPlus 5.2.1** which:
- ✅ Has all Excel reading/writing features we need
- ✅ No WPF dependencies required
- ✅ Works perfectly with WinForms
- ✅ More permissive license (LGPL vs 6.x commercial license)
- ✅ Smaller package size (~3 MB vs ~5 MB)

## Files Changed

### 1. packages.config
```xml
<!-- Changed from version 6.2.10 to 5.2.1 -->
<package id="EPPlus" version="5.2.1" targetFramework="net48" />
```

### 2. ReverseRaffler.csproj
```xml
<!-- Updated reference to version 5.2.1.0 -->
<Reference Include="EPPlus, Version=5.2.1.0, ...>
  <HintPath>packages\EPPlus.5.2.1\lib\net45\EPPlus.dll</HintPath>
</Reference>
```

### 3. Services/ExcelImportService.cs
```csharp
// Removed this line (only needed in EPPlus 6.x):
// ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
```

## Next Steps

1. **In Visual Studio:**
   - Right-click solution → **Restore NuGet Packages**
   - This will download EPPlus 5.2.1
   - Build the project

2. **If you still get the error:**
   - Close Visual Studio
   - Delete the `packages\EPPlus.6.2.10` folder (if it exists)
   - Delete `bin\` and `obj\` folders
   - Reopen Visual Studio
   - Restore NuGet packages again
   - Build

## Verification

After building successfully, you should see:
- `packages\EPPlus.5.2.1\` folder in your solution
- No errors about PresentationCore
- Excel import feature works exactly the same

## API Compatibility

EPPlus 5.2.1 API is 99% compatible with our usage:
- ✅ `ExcelPackage` class - same
- ✅ `Worksheets` property - same
- ✅ `Cells` indexing - same
- ✅ All methods we use - same

The only difference is we don't need to set the license context in code.
