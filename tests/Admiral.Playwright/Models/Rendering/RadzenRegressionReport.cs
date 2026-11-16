namespace Admiral.Playwright.Models.Rendering;

internal sealed class RadzenRegressionReport
{
    public ChartSeriesSnapshot[] Series { get; set; } = [];
    public double TickContrast { get; set; }
    public double SummaryContrast { get; set; }
    public double CalendarTitleContrast { get; set; }
    public double TocContrast { get; set; }
    public Dictionary<string, ButtonContrast> ButtonContrast { get; } = [];
    public List<string> Warnings { get; } = [];
}

internal sealed record ButtonContrast(double DemoContrast, double CardContrast);
