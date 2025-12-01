public class DataParser
{
    public double[] ParseMagazine(string content)
    {
        // Detect format: INI or legacy CSV
        if (content.Contains("[__________ Magazine Thickness Values __________]"))
        {
            return ParseMagazineIni(content);
        }
        else
        {
            return ParseMagazineLegacy(content);
        }
    }
    
    private double[] ParseMagazineIni(string content)
    {
        var values = new List<double>();
        bool inMagazineSection = false;
        
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            
            if (trimmed.Contains("[__________ Magazine Thickness Values __________]"))
            {
                inMagazineSection = true;
                continue;
            }
            
            if (inMagazineSection && trimmed.StartsWith("["))
            {
                break; // End of magazine section
            }
            
            if (inMagazineSection && trimmed.StartsWith("MagStandard"))
            {
                var parts = trimmed.Split('=');
                if (parts.Length == 2 && double.TryParse(parts[1].Trim(), out double value) && value > 0)
                {
                    values.Add(value);
                }
            }
        }
        
        return values.ToArray();
    }
    
    private double[] ParseMagazineLegacy(string content)
    {
        var values = new List<double>();
        
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("//") || string.IsNullOrWhiteSpace(trimmed) || trimmed == "END")
                continue;
            
            var parts = trimmed.Split(',');
            if (parts.Length >= 2 && double.TryParse(parts[1].Trim(), out double value) && value > 0)
            {
                values.Add(value);
            }
        }
        
        return values.ToArray();
    }
    
    public CalibrationRange[] ParseCalibration(string content, double[] masters)
    {
        // Detect format: INI or legacy CSV
        if (content.Contains("[__________ Range"))
        {
            return ParseCalibrationIni(content, masters);
        }
        else
        {
            return ParseCalibrationLegacy(content, masters);
        }
    }
    
    private CalibrationRange[] ParseCalibrationIni(string content, double[] masters)
    {
        var ranges = new List<CalibrationRange>();
        var lines = content.Split('\n');
        int currentRangeId = -1;
        var currentPoints = new List<CalibrationPoint>();
        int samples = 0;
        
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            
            // Check for range section header
            var rangeMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"\[__________\s+Range\s+(\d+)__________\]");
            if (rangeMatch.Success)
            {
                // Save previous range if it has data
                if (currentPoints.Count > 0)
                {
                    ranges.Add(new CalibrationRange(currentRangeId + 1, currentPoints.ToArray()));
                }
                
                currentRangeId = int.Parse(rangeMatch.Groups[1].Value);
                currentPoints = new List<CalibrationPoint>();
                samples = 0;
                continue;
            }
            
            // Get sample count for current range
            if (trimmed.StartsWith("Samples="))
            {
                samples = int.Parse(trimmed.Split('=')[1]);
                continue;
            }
            
            // Parse data points (format: index=thickness,voltage,ln(voltage),masters)
            if (samples > 0 && System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+="))
            {
                var parts = trimmed.Split('=');
                if (parts.Length == 2)
                {
                    var values = parts[1].Split(',');
                    if (values.Length >= 4)
                    {
                        if (double.TryParse(values[0], out double thickness) &&
                            double.TryParse(values[1], out double voltage) &&
                            int.TryParse(values[3], out int mastersIdx))
                        {
                            currentPoints.Add(new CalibrationPoint(voltage, thickness, mastersIdx));
                        }
                    }
                }
            }
        }
        
        // Add last range if it has data
        if (currentPoints.Count > 0)
        {
            ranges.Add(new CalibrationRange(currentRangeId + 1, currentPoints.ToArray()));
        }
        
        return ranges.ToArray();
    }
    
    private CalibrationRange[] ParseCalibrationLegacy(string content, double[] masters)
    {
        var ranges = new List<CalibrationRange>();
        List<CalibrationPoint>? currentPoints = null;
        
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;
            
            // Check for range delimiter
            if (trimmed.Contains("Solved") || System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"Range\s+\d", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                if (currentPoints != null && currentPoints.Count > 0)
                {
                    ranges.Add(new CalibrationRange(ranges.Count + 1, currentPoints.ToArray()));
                }
                currentPoints = new List<CalibrationPoint>();
                continue;
            }
            
            // Skip header lines
            if (trimmed.Contains("Data") || trimmed.Contains("\"X") || 
                trimmed.Contains("Coefficients") || trimmed.Contains("r_squared") ||
                trimmed.Contains("-----") || trimmed.StartsWith("//") || trimmed.Contains("="))
            {
                continue;
            }
            
            // Parse data point
            var parts = trimmed.Split(',');
            if (parts.Length >= 2)
            {
                if (double.TryParse(parts[0].Trim(), out double voltage) &&
                    double.TryParse(parts[1].Trim(), out double thickness) &&
                    voltage > 0 && thickness > 0)
                {
                    if (currentPoints == null)
                    {
                        currentPoints = new List<CalibrationPoint>();
                    }
                    
                    int idx = FindCombo(thickness, masters);
                    currentPoints.Add(new CalibrationPoint(voltage, thickness, idx));
                }
            }
        }
        
        if (currentPoints != null && currentPoints.Count > 0)
        {
            ranges.Add(new CalibrationRange(ranges.Count + 1, currentPoints.ToArray()));
        }
        
        return ranges.ToArray();
    }
    
    private int FindCombo(double thickness, double[] masters)
    {
        var combos = CalculateCombinations(masters);
        int best = 0;
        double bestError = Math.Abs(thickness - combos[0]);
        
        for (int i = 1; i < combos.Length; i++)
        {
            double error = Math.Abs(thickness - combos[i]);
            if (error < bestError)
            {
                bestError = error;
                best = i;
            }
        }
        
        return best;
    }
    
    private double[] CalculateCombinations(double[] masters)
    {
        int n = masters.Length;
        int numCombos = (int)Math.Pow(2, n);
        double[] combos = new double[numCombos];
        
        for (int i = 0; i < numCombos; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if ((i & (1 << j)) != 0)
                {
                    combos[i] += masters[j];
                }
            }
        }
        
        return combos;
    }
}
