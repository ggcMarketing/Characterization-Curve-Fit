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

### Magazine Values Format
```
// Comment lines start with //
1, 25.461380
2, 47.649871
3, 97.231679
...
END
```

### Calibration Data Format
```
Range 1
Voltage, Thickness
4.500000, 25.461
4.350000, 47.650
...
```

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
