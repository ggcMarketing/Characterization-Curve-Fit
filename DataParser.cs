public class DataParser
{
    public double[] ParseMagazine(string content)
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
