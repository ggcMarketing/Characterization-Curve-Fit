var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/optimize", (OptimizationRequest req) =>
{
    var optimizer = new CalibrationOptimizer();
    var result = optimizer.Optimize(req.Masters, req.Ranges, req.Order, req.MaxPct);
    return Results.Ok(result);
});

app.MapPost("/api/parse-magazine", async (IFormFile file) =>
{
    using var reader = new StreamReader(file.OpenReadStream());
    var content = await reader.ReadToEndAsync();
    var parser = new DataParser();
    var values = parser.ParseMagazine(content);
    return Results.Ok(new { values });
});

app.MapPost("/api/parse-calibration", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var file = form.Files["file"];
    var mastersJson = form["masters"];
    
    if (file == null || string.IsNullOrEmpty(mastersJson))
        return Results.BadRequest();
    
    using var reader = new StreamReader(file.OpenReadStream());
    var content = await reader.ReadToEndAsync();
    var masters = System.Text.Json.JsonSerializer.Deserialize<double[]>(mastersJson);
    
    var parser = new DataParser();
    var ranges = parser.ParseCalibration(content, masters!);
    return Results.Ok(new { ranges });
});

app.Run();

public record OptimizationRequest(double[] Masters, CalibrationRange[] Ranges, int Order, double MaxPct);
public record CalibrationPoint(double V, double T, int Idx);
public record CalibrationRange(int Id, CalibrationPoint[] Pts);
public record OptimizationResult(double[] Opt, double[] Pct, double OrigRSq, double OptRSq, double Imp);
