using System.Collections.Concurrent;
using ExcelDna.Integration;
using ExcelDna.Integration.CustomUI;
using Microsoft.Extensions.AI;

namespace Cellm.AddIn.UserInterface.Ribbon;

public partial class RibbonMain
{
    internal enum ModelGroupStatisticsControlIds
    {
        StatisticsTokensContainer,
        StatisticsSpeedContainer,
        TokensLabel,
        TokenStatistics,
        SpeedLabel,
        SpeedStatistics,
        TPS,
        RPS
    }

    internal static ConcurrentDictionary<string, double> _statistics = new()
    {
        [nameof(UsageDetails.InputTokenCount)] = 0,
        [nameof(UsageDetails.OutputTokenCount)] = 0,
        [nameof(ModelGroupStatisticsControlIds.TPS)] = 0,
        [nameof(ModelGroupStatisticsControlIds.RPS)] = 0
    };

    private string ModelGroupStatistics()
    {
        return $"""
            <box id="{nameof(ModelGroupStatisticsControlIds.StatisticsTokensContainer)}" boxStyle="horizontal">
                        <labelControl id="{nameof(ModelGroupStatisticsControlIds.TokensLabel)}" label="Tokens:" />
                        <labelControl id="{nameof(ModelGroupStatisticsControlIds.TokenStatistics)}" getLabel="{nameof(GetTokenStatisticsText)}" supertip="Total input and output token usage this session" />
                    </box>
                    <box id="{nameof(ModelGroupStatisticsControlIds.StatisticsSpeedContainer)}" boxStyle="horizontal">
                        <labelControl id="{nameof(ModelGroupStatisticsControlIds.SpeedLabel)}" label="Speed:" />
                        <labelControl id="{nameof(ModelGroupStatisticsControlIds.SpeedStatistics)}" getLabel="{nameof(GetSpeedStatisticsText)}" supertip="Average Tokens Per Second (TPS) per request and average Requests Per Second" />
            </box>
            """;
    }

    public string GetTokenStatisticsText(IRibbonControl control)
    {
        return $"{FormatCount(_statistics[nameof(UsageDetails.InputTokenCount)])} in / {FormatCount(_statistics[nameof(UsageDetails.OutputTokenCount)])} out";
    }

    public string GetSpeedStatisticsText(IRibbonControl control)
    {
        return $"{_statistics[nameof(ModelGroupStatisticsControlIds.TPS)]:F0} TPS x {_statistics[nameof(ModelGroupStatisticsControlIds.RPS)]:F1} RPS";
    }

    public static void UpdateTokenStatistics(long inputTokens, long outputTokens)
    {
        _statistics[nameof(UsageDetails.InputTokenCount)] = inputTokens;
        _statistics[nameof(UsageDetails.OutputTokenCount)] = outputTokens;

        ExcelAsyncUtil.QueueAsMacro(() =>
        {
            _ribbonUi?.InvalidateControl(nameof(ModelGroupStatisticsControlIds.TokenStatistics));
        });
    }

    public static void UpdateSpeedStatistics(double tokensPerSecond, double requestsPerBusySecond)
    {
        _statistics[nameof(ModelGroupStatisticsControlIds.TPS)] = tokensPerSecond;
        _statistics[nameof(ModelGroupStatisticsControlIds.RPS)] = requestsPerBusySecond;

        ExcelAsyncUtil.QueueAsMacro(() =>
        {
            _ribbonUi?.InvalidateControl(nameof(ModelGroupStatisticsControlIds.SpeedStatistics));
        });
    }

    public static string FormatCount(double number)
    {
        if (number == 0) return "0";

        string[] suffixes = { "", "K", "M", "B", "T", "P", "E" }; // Kilo, Mega, Giga, Tera, Peta, Exa

        // The log base 1000 of the number gives us the magnitude
        var magnitude = (int)Math.Log(Math.Abs(number), 1000);

        // Don't go beyond the available suffixes
        if (magnitude >= suffixes.Length)
        {
            magnitude = suffixes.Length - 1;
        }

        // Scale the number down to the 1-999 range
        var scaledNumber = number / Math.Pow(1000, magnitude);

        // Format the number with one optional decimal place and append the correct suffix
        return $"{scaledNumber:0.#}{suffixes[magnitude]}";
    }
}
