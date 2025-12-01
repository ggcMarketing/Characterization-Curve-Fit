public class CalibrationOptimizer
{
    public OptimizationResult Optimize(double[] masters, CalibrationRange[] ranges, int order, double maxPct)
    {
        int n = masters.Length;
        double maxF = maxPct / 100.0;
        double[] current = (double[])masters.Clone();
        
        double origRSq = CalculateRSquared(masters, ranges, order);
        double bestRSq = origRSq;
        double[] best = (double[])masters.Clone();
        
        for (int iter = 0; iter < 100; iter++)
        {
            var combos = CalculateCombinations(current);
            double[] errorSum = new double[n];
            int[] errorCount = new int[n];
            
            foreach (var range in ranges)
            {
                if (range.Pts.Length < order + 1) continue;
                
                var xValues = range.Pts.Select(p => p.V).ToArray();
                var yValues = range.Pts.Select(p => p.Idx == 0 ? p.T : combos[p.Idx]).ToArray();
                
                var fit = PolynomialFit(xValues, yValues, order);
                if (fit == null) continue;
                
                for (int i = 0; i < range.Pts.Length; i++)
                {
                    var pt = range.Pts[i];
                    if (pt.Idx == 0) continue;
                    
                    var mastersUsed = GetMastersUsed(pt.Idx, n);
                    double errorPerMaster = fit.Residuals[i] / mastersUsed.Length;
                    
                    foreach (int m in mastersUsed)
                    {
                        errorSum[m] += errorPerMaster;
                        errorCount[m]++;
                    }
                }
            }
            
            double maxChange = 0;
            double[] next = new double[n];
            
            for (int i = 0; i < n; i++)
            {
                if (errorCount[i] > 0)
                {
                    double adjustment = 0.3 * errorSum[i] / errorCount[i];
                    double newValue = current[i] + adjustment;
                    newValue = Math.Max(masters[i] * (1 - maxF), Math.Min(masters[i] * (1 + maxF), newValue));
                    maxChange = Math.Max(maxChange, Math.Abs(newValue - current[i]) / current[i]);
                    next[i] = newValue;
                }
                else
                {
                    next[i] = current[i];
                }
            }
            
            double newRSq = CalculateRSquared(next, ranges, order);
            if (newRSq > bestRSq)
            {
                bestRSq = newRSq;
                best = (double[])next.Clone();
            }
            
            if (maxChange < 1e-8) break;
            current = next;
        }
        
        double[] percentChanges = new double[n];
        for (int i = 0; i < n; i++)
        {
            percentChanges[i] = ((best[i] - masters[i]) / masters[i]) * 100.0;
        }
        
        return new OptimizationResult(best, percentChanges, origRSq, bestRSq, bestRSq - origRSq);
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
    
    private int[] GetMastersUsed(int idx, int n)
    {
        var used = new List<int>();
        for (int j = 0; j < n; j++)
        {
            if ((idx & (1 << j)) != 0)
            {
                used.Add(j);
            }
        }
        return used.ToArray();
    }
    
    private double CalculateRSquared(double[] masters, CalibrationRange[] ranges, int order)
    {
        var combos = CalculateCombinations(masters);
        double totalRSq = 0;
        int count = 0;
        
        foreach (var range in ranges)
        {
            if (range.Pts.Length < order + 1) continue;
            
            var xValues = range.Pts.Select(p => p.V).ToArray();
            var yValues = range.Pts.Select(p => p.Idx == 0 ? p.T : combos[p.Idx]).ToArray();
            
            var fit = PolynomialFit(xValues, yValues, order);
            if (fit != null)
            {
                totalRSq += fit.RSquared;
                count++;
            }
        }
        
        return count > 0 ? totalRSq / count : 0;
    }
    
    private PolyFitResult? PolynomialFit(double[] x, double[] y, int order)
    {
        int n = x.Length;
        if (n <= order) return null;
        
        // Build Vandermonde matrix
        double[][] V = new double[n][];
        for (int i = 0; i < n; i++)
        {
            V[i] = new double[order + 1];
            for (int j = 0; j <= order; j++)
            {
                V[i][j] = Math.Pow(x[i], j);
            }
        }
        
        // Calculate V^T * V and V^T * y
        double[][] VtV = new double[order + 1][];
        double[] Vty = new double[order + 1];
        
        for (int i = 0; i <= order; i++)
        {
            VtV[i] = new double[order + 1];
            for (int j = 0; j <= order; j++)
            {
                double sum = 0;
                for (int k = 0; k < n; k++)
                {
                    sum += V[k][i] * V[k][j];
                }
                VtV[i][j] = sum;
            }
            
            double sumY = 0;
            for (int k = 0; k < n; k++)
            {
                sumY += V[k][i] * y[k];
            }
            Vty[i] = sumY;
        }
        
        // Gaussian elimination
        double[][] aug = new double[order + 1][];
        for (int i = 0; i <= order; i++)
        {
            aug[i] = new double[order + 2];
            for (int j = 0; j <= order; j++)
            {
                aug[i][j] = VtV[i][j];
            }
            aug[i][order + 1] = Vty[i];
        }
        
        for (int col = 0; col <= order; col++)
        {
            int maxRow = col;
            for (int row = col + 1; row <= order; row++)
            {
                if (Math.Abs(aug[row][col]) > Math.Abs(aug[maxRow][col]))
                {
                    maxRow = row;
                }
            }
            
            (aug[col], aug[maxRow]) = (aug[maxRow], aug[col]);
            
            if (Math.Abs(aug[col][col]) < 1e-10) return null;
            
            for (int row = col + 1; row <= order; row++)
            {
                double factor = aug[row][col] / aug[col][col];
                for (int j = col; j <= order + 1; j++)
                {
                    aug[row][j] -= factor * aug[col][j];
                }
            }
        }
        
        // Back substitution
        double[] coeffs = new double[order + 1];
        for (int i = order; i >= 0; i--)
        {
            coeffs[i] = aug[i][order + 1];
            for (int j = i + 1; j <= order; j++)
            {
                coeffs[i] -= aug[i][j] * coeffs[j];
            }
            coeffs[i] /= aug[i][i];
        }
        
        // Calculate residuals and R²
        double[] fitted = new double[n];
        double[] residuals = new double[n];
        for (int i = 0; i < n; i++)
        {
            fitted[i] = 0;
            for (int j = 0; j <= order; j++)
            {
                fitted[i] += coeffs[j] * Math.Pow(x[i], j);
            }
            residuals[i] = fitted[i] - y[i];
        }
        
        double ssRes = residuals.Sum(r => r * r);
        double yMean = y.Average();
        double ssTot = y.Sum(yi => Math.Pow(yi - yMean, 2));
        double rSquared = ssTot > 0 ? 1 - ssRes / ssTot : 1;
        
        return new PolyFitResult(rSquared, residuals);
    }
    
    private record PolyFitResult(double RSquared, double[] Residuals);
}
