# Ion Chamber Calibration: Masters-Finder

Modern C# ASP.NET Core implementation of the Masters-Finder calibration tool for ion chamber systems.

## Features

- **Fixed kV Mode**: Take ONE measurement at fixed kV, optimize multiple times without re-measuring
- **Polynomial Fitting**: Configurable polynomial order (3-8) for calibration curves
- **Iterative Optimization**: Adjusts master thicknesses mathematically to improve R² values
- **Multi-Range Support**: Handle multiple calibration ranges in a single session
- **Real-time Feedback**: Live R² tracking and optimization progress
- **Export Results**: Save optimized magazine values for deployment

## Architecture

### Backend (C#)
- **Program.cs**: ASP.NET Core minimal API setup with endpoints
- **CalibrationOptimizer.cs**: Core optimization algorithm with polynomial fitting
- **DataParser.cs**: Parses magazine values and calibration data files
- **Records**: Type-safe data models for requests/responses

### Frontend (Vanilla JavaScript)
- **wwwroot/index.html**: Clean, responsive UI with modern styling
- **wwwroot/app.js**: Client-side application logic and API integration

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later

### Running the Application

1. Build and run:
```bash
dotnet run
```

2. Open browser to: `http://localhost:5000` (or the port shown in console)

3. Workflow:
   - Load Magazine Values file (`.txt` format)
   - Load Calibration Coefficients/Data file
   - Adjust polynomial order and max correction percentage
   - Click "Optimize" to run the algorithm
   - Review R² improvement and percentage changes
   - Click "Apply" to use optimized values
   - Click "Export" to save results

## API Endpoints

### POST /api/optimize
Runs the optimization algorithm.

**Request:**
```json
{
  "masters": [25.46, 47.65, ...],
  "ranges": [
    {
      "id": 1,
      "pts": [
        { "v": 4.5, "t": 25.46, "idx": 1 }
      ]
    }
  ],
  "order": 6,
  "maxPct": 3.0
}
```

**Response:**
```json
{
  "opt": [25.47, 47.66, ...],
  "pct": [0.04, 0.02, ...],
  "origRSq": 0.99999850,
  "optRSq": 0.99999920,
  "imp": 0.0000007
}
```

### POST /api/parse-magazine
Parses magazine values file.

**Request:** Multipart form with file

**Response:**
```json
{
  "values": [25.46, 47.65, ...]
}
```

### POST /api/parse-calibration
Parses calibration data file.

**Request:** Multipart form with file and masters JSON

**Response:**
```json
{
  "ranges": [
    {
      "id": 1,
      "pts": [...]
    }
  ]
}
```

## File Formats

The tool supports both legacy and modern INI-based file formats.

### Magazine Values Formats

**INI Format (Perfecta II):**
```ini
[__________ General __________]
Time Stamp=10/16/2025 4:17:24 PM
[__________ Magazine Thickness Values __________]
MagStandard00=0.00101000003814697
MagStandard01=0.00203999996185303
MagStandard02=0.004025
...
```

**Legacy Format:**
```
// Comment lines start with //
1, 25.461380
2, 47.649871
3, 97.231679
...
END
```

### Calibration Data Formats

**INI Format (Perfecta II):**
```ini
[__________ General __________]
Time Stamp=10/20/2025 3:28:38 PM
CalibrationStart=0.014
CalibrationEnd=0.299997194388778
CalibrationStep=0.004
CalibrationOverlap=0
[__________ Range 0__________]
Samples=21
KV=65.4633178710938
Gain=0.100000001490116
ThickHigh=0.0910729989409447
ThickLow=0.0110039999708533
Thickness0=0.0110039999708533
Voltage0=19.0028648376465
ln(Voltage)0=2.94458985328674
Masters0=17
...
```

**Legacy Format:**
```
Range 1
Voltage, Thickness
4.500000, 25.461
4.350000, 47.650
...
```

### Masters Index Format

The INI format includes a `Masters` field for each calibration point that represents the binary combination of masters used:
- Each bit position represents a master (bit 0 = master 0, bit 1 = master 1, etc.)
- Example: `Masters0=17` means binary `10001` = masters 0 and 4 were used
- This allows for non-sequential master combinations, improving calibration flexibility

## Algorithm Details

### Optimization Process
1. Calculate all possible master combinations (2^n combinations)
2. For each calibration range, fit polynomial to voltage vs thickness
3. Calculate residuals and distribute errors across masters
4. Iteratively adjust master values (max 100 iterations)
5. Track best R² achieved and return optimized values

### Polynomial Fitting
- Uses Vandermonde matrix approach
- Gaussian elimination for solving linear system
- Calculates R² (coefficient of determination) for fit quality

### Convergence
- Stops when changes < 1e-8 or after 100 iterations
- Respects max correction percentage constraint
- Returns best solution found during optimization

## Advantages Over Original

1. **Performance**: C# backend handles heavy computation efficiently
2. **Scalability**: Can handle larger datasets and more masters
3. **Maintainability**: Strongly-typed, testable code structure
4. **Deployment**: Standard ASP.NET Core deployment options
5. **Security**: Server-side validation and processing
6. **Extensibility**: Easy to add new features or algorithms

## Development

### Project Structure
```
MastersFinder/
├── Program.cs                 # API endpoints
├── CalibrationOptimizer.cs    # Core algorithm
├── DataParser.cs              # File parsing
├── MastersFinder.csproj       # Project file
├── wwwroot/
│   ├── index.html            # UI
│   └── app.js                # Frontend logic
└── README.md
```

### Testing
Generate sample data using the "Sample Data" button to test without real calibration files.

## License

Based on the original MastersFinder.html tool, modernized for C# ASP.NET Core.
