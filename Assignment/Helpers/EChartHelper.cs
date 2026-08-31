using Assignment.Models;

namespace Assignment.Helpers
{
    public class EChartSeries
    {
        public string Name { get; set; } = string.Empty;
        public List<decimal> Data { get; set; } = [];
        public string? Color { get; set; }
        public bool Smooth { get; set; } = true;
        public bool Area { get; set; } = false;
        public string Type { get; set; } = "line";
        public string? Stack { get; set; }

        public EChartSeries() { }

        public EChartSeries(string name, IEnumerable<decimal> data, string? color = null, bool smooth = true, bool area = false, string type = "line", string? stack = null)
        {
            Name = name;
            Data = data.ToList();
            Color = color;
            Smooth = smooth;
            Area = area;
            Type = type;
            Stack = stack;
        }

        public EChartSeries(string name, IEnumerable<int> data, string? color = null, bool smooth = true, bool area = false, string type = "line", string? stack = null)
            : this(name, data.Select(x => (decimal)x), color, smooth, area, type, stack)
        {
        }

        public EChartSeries(string name, IEnumerable<double> data, string? color = null, bool smooth = true, bool area = false, string type = "line", string? stack = null)
            : this(name, data.Select(x => (decimal)x), color, smooth, area, type, stack)
        {
        }
    }

    public static class EChartHelper
    {
        public static EChartViewModel CreateLineChart(
            string title,
            IEnumerable<string> xLabels,
            IEnumerable<EChartSeries> series,
            string? yAxisName = null,
            string height = "380px",
            bool enableToolbox = true)
        {
            var seriesList = series.ToList();
            var legendData = seriesList.Select(s => s.Name).ToArray();

            var seriesObjects = seriesList.Select(s => new
            {
                name = s.Name,
                type = "line",
                smooth = s.Smooth,
                areaStyle = s.Area ? new { opacity = 0.2 } : null,
                color = s.Color,
                stack = s.Stack,
                data = s.Data
            }).ToArray();

            var option = new
            {
                title = new { text = title, left = "center" },
                tooltip = new { trigger = "axis" },
                legend = new { data = legendData, top = "bottom" },
                toolbox = enableToolbox ? new
                {
                    feature = new
                    {
                        magicType = new { type = new[] { "line", "bar" } },
                        dataView = new { readOnly = true },
                        saveAsImage = new { }
                    }
                } : null,
                xAxis = new
                {
                    type = "category",
                    data = xLabels.ToArray(),
                    boundaryGap = false
                },
                yAxis = new
                {
                    type = "value",
                    name = yAxisName,
                    minInterval = 1
                },
                series = seriesObjects
            };

            return new EChartViewModel(option, height);
        }

        public static EChartViewModel CreateBarChart(
            string title,
            IEnumerable<string> xLabels,
            IEnumerable<EChartSeries> series,
            string? yAxisName = null,
            bool isHorizontal = false,
            bool isStacked = false,
            string height = "380px",
            bool enableToolbox = true)
        {
            var seriesList = series.ToList();
            var legendData = seriesList.Select(s => s.Name).ToArray();

            var seriesObjects = seriesList.Select(s => new
            {
                name = s.Name,
                type = "bar",
                stack = isStacked ? (s.Stack ?? "total") : null,
                color = s.Color,
                data = s.Data
            }).ToArray();

            object xAxisConfig;
            object yAxisConfig;

            if (isHorizontal)
            {
                xAxisConfig = new { type = "value", name = yAxisName };
                yAxisConfig = new { type = "category", data = xLabels.ToArray() };
            }
            else
            {
                xAxisConfig = new { type = "category", data = xLabels.ToArray() };
                yAxisConfig = new { type = "value", name = yAxisName, minInterval = 1 };
            }

            var option = new
            {
                title = new { text = title, left = "center" },
                tooltip = new { trigger = "axis", axisPointer = new { type = "shadow" } },
                legend = new { data = legendData, top = "bottom" },
                toolbox = enableToolbox ? new
                {
                    feature = new
                    {
                        magicType = new { type = new[] { "bar", "line" } },
                        dataView = new { readOnly = true },
                        saveAsImage = new { }
                    }
                } : null,
                xAxis = xAxisConfig,
                yAxis = yAxisConfig,
                series = seriesObjects
            };

            return new EChartViewModel(option, height);
        }

        public static EChartViewModel CreatePieChart(
            string title,
            IEnumerable<KeyValuePair<string, decimal>> data,
            bool isDonut = true,
            string height = "380px",
            bool enableToolbox = true)
        {
            var dataItems = data.Select(kvp => new
            {
                name = kvp.Key,
                value = kvp.Value
            }).ToArray();

            var option = new
            {
                title = new { text = title, left = "center" },
                tooltip = new { trigger = "item", formatter = "{b}: {c} ({d}%)" },
                legend = new { top = "bottom" },
                toolbox = enableToolbox ? new
                {
                    feature = new
                    {
                        dataView = new { readOnly = true },
                        saveAsImage = new { }
                    }
                } : null,
                series = new[]
                {
                    new
                    {
                        name = title,
                        type = "pie",
                        radius = isDonut ? new[] { "40%", "70%" } : new[] { "0%", "70%" },
                        avoidLabelOverlap = true,
                        itemStyle = new { borderRadius = 6, borderColor = "#fff", borderWidth = 2 },
                        data = dataItems
                    }
                }
            };

            return new EChartViewModel(option, height);
        }

        public static EChartViewModel CreatePieChart(
            string title,
            IDictionary<string, decimal> data,
            bool isDonut = true,
            string height = "380px",
            bool enableToolbox = true)
        {
            return CreatePieChart(title, data.AsEnumerable(), isDonut, height, enableToolbox);
        }

        public static EChartViewModel CreatePieChart(
            string title,
            IDictionary<string, int> data,
            bool isDonut = true,
            string height = "380px",
            bool enableToolbox = true)
        {
            var decimalDict = data.ToDictionary(k => k.Key, v => (decimal)v.Value);
            return CreatePieChart(title, decimalDict, isDonut, height, enableToolbox);
        }
    }
}
