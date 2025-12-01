# Changelog

## [Unreleased] - feature/new-file-format

### Added
- **INI File Format Support**: Full support for Perfecta II INI-based file formats
  - Magazine values in `[__________ Magazine Thickness Values __________]` section
  - Calibration data with detailed range metadata (KV, Gain, ThickHigh, ThickLow)
  - Explicit Masters index field for binary master combinations
  
- **FileExporter Class**: New export functionality supporting both formats
  - `ExportMagazineIni()`: Export magazine values in INI format
  - `ExportMagazineLegacy()`: Export magazine values in legacy CSV format
  - `ExportCalibrationIni()`: Export calibration data with full metadata
  
- **Format Auto-Detection**: Parsers automatically detect and handle both formats
  - `ParseMagazineIni()`: Parse INI-based magazine files
  - `ParseMagazineLegacy()`: Parse legacy CSV magazine files
  - `ParseCalibrationIni()`: Parse INI-based calibration files with Masters index
  - `ParseCalibrationLegacy()`: Parse legacy CSV calibration files
  
- **UI Format Selector**: Dropdown to choose export format (INI/Legacy)
  - Defaults to INI format for modern systems
  - Maintains backward compatibility with legacy systems

### Changed
- **DataParser.cs**: Refactored to support dual format parsing
  - Added format detection logic
  - Separated parsing logic for each format
  - Properly handles Masters index from INI files
  
- **Program.cs**: Added export endpoint
  - New `/api/export-magazine` endpoint for format-specific exports
  
- **wwwroot/app.js**: Enhanced export functionality
  - Async export with format selection
  - Better error handling and user feedback

### Technical Details

#### Masters Index Format
The INI format includes a `Masters` field that represents the binary combination of masters used:
- Each bit position represents a master (bit 0 = master 0, bit 1 = master 1, etc.)
- Example: `Masters0=17` (binary `10001`) means masters 0 and 4 were used
- This allows non-sequential master combinations, improving calibration flexibility

#### Backward Compatibility
- All existing legacy format files continue to work
- Legacy format uses `FindCombo()` to calculate master combinations
- INI format reads explicit Masters index values
- Export format can be selected per operation

## [1.0.0] - 2025-12-01

### Initial Release
- C# ASP.NET Core implementation of MastersFinder
- Polynomial fitting and optimization algorithms
- Multi-range calibration support
- REST API for optimization and file parsing
- Modern responsive web UI
- Sample data generation
- Legacy CSV format support
