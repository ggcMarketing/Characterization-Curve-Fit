using System.Text;

public class FileExporter
{
    public string ExportMagazineIni(double[] masters, string? originalContent = null)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("[__________ General __________]");
        sb.AppendLine($"Time Stamp={DateTime.Now:M/d/yyyy h:mm:ss tt}");
        sb.AppendLine("[__________ Magazine Thickness Values __________]");
        
        for (int i = 0; i < masters.Length; i++)
        {
            sb.AppendLine($"MagStandard{i:D2}={masters[i]:G17}");
        }
        
        // Pad with zeros up to 15 entries (standard format)
        for (int i = masters.Length; i < 15; i++)
        {
            sb.AppendLine($"MagStandard{i:D2}=0");
        }
        
        return sb.ToString();
    }
    
    public string ExportMagazineLegacy(double[] masters)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("// Optimized Magazine Values");
        sb.AppendLine($"// Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        
        for (int i = 0; i < masters.Length; i++)
        {
            sb.AppendLine($" {i + 1}, {masters[i]:F6}");
        }
        
        sb.AppendLine("END");
        
        return sb.ToString();
    }
    
    public string ExportCalibrationIni(CalibrationRange[] ranges, double[] masters, CalibrationMetadata? metadata = null)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("[__________ General __________]");
        sb.AppendLine($"Time Stamp={DateTime.Now:M/d/yyyy h:mm:ss tt}");
        
        if (metadata != null)
        {
            sb.AppendLine($"CalibrationStart={metadata.CalibrationStart:G17}");
            sb.AppendLine($"CalibrationEnd={metadata.CalibrationEnd:G17}");
            sb.AppendLine($"CalibrationStep={metadata.CalibrationStep:G17}");
            sb.AppendLine($"CalibrationOverlap={metadata.CalibrationOverlap}");
        }
        
        for (int rangeIdx = 0; rangeIdx < 10; rangeIdx++)
        {
            sb.AppendLine($"[__________ Range {rangeIdx}__________]");
            
            if (rangeIdx < ranges.Length)
            {
                var range = ranges[rangeIdx];
                sb.AppendLine($"Samples={range.Pts.Length}");
                
                if (metadata?.RangeMetadata != null && rangeIdx < metadata.RangeMetadata.Length)
                {
                    var rangeMeta = metadata.RangeMetadata[rangeIdx];
                    sb.AppendLine($"KV={rangeMeta.KV:G17}");
                    sb.AppendLine($"Gain={rangeMeta.Gain:G17}");
                    sb.AppendLine($"ThickHigh={rangeMeta.ThickHigh:G17}");
                    sb.AppendLine($"ThickLow={rangeMeta.ThickLow:G17}");
                }
                
                // Write individual data points
                for (int i = 0; i < range.Pts.Length; i++)
                {
                    var pt = range.Pts[i];
                    double lnVoltage = Math.Log(pt.V);
                    sb.AppendLine($"Thickness{i}={pt.T:G17}");
                    sb.AppendLine($"Voltage{i}={pt.V:G17}");
                    sb.AppendLine($"ln(Voltage){i}={lnVoltage:G17}");
                    sb.AppendLine($"Masters{i}={pt.Idx}");
                }
                
                // Write summary lines at end
                for (int i = 0; i < range.Pts.Length; i++)
                {
                    var pt = range.Pts[i];
                    double lnVoltage = Math.Log(pt.V);
                    sb.AppendLine($"{i}={pt.T:G17},{pt.V:G17},{lnVoltage:G17},{pt.Idx}");
                }
            }
            else
            {
                // Empty range
                sb.AppendLine("Samples=0");
                sb.AppendLine("KV=0");
                sb.AppendLine("Gain=0");
                sb.AppendLine("ThickHigh=0");
                sb.AppendLine("ThickLow=0");
            }
        }
        
        return sb.ToString();
    }
}

public record CalibrationMetadata(
    double CalibrationStart,
    double CalibrationEnd,
    double CalibrationStep,
    int CalibrationOverlap,
    RangeMetadata[]? RangeMetadata = null
);

public record RangeMetadata(
    double KV,
    double Gain,
    double ThickHigh,
    double ThickLow
);
