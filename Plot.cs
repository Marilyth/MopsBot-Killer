using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OxyPlot;
using OxyPlot.Axes;
using System.Threading.Tasks;

namespace MopsKiller
{
    /// <summary>
    /// A Class that handles drawing plots.
    /// </summary>
    public class DatePlot
    {
        private PlotModel viewerChart;
        private List<OxyPlot.Series.AreaSeries> areaSeries;
        private static string COLLECTIONNAME = "TwitchTracker";
        //public List<KeyValuePair<string, double>> PlotPoints;
        public List<KeyValuePair<string, KeyValuePair<double, double>>> PlotDataPoints;
        private DateTime? StartTime;

        public string ID;
        public bool MultipleLines;

        public DatePlot(string name, string xName = "x", string yName = "y", string format = "HH:mm", bool relativeTime = true, bool multipleLines = false)
        {
            ID = name;
            MultipleLines = multipleLines;
            //PlotPoints = new List<KeyValuePair<string, double>>();
            PlotDataPoints = new List<KeyValuePair<string, KeyValuePair<double, double>>>();
            InitPlot(xName, yName, format, relative: relativeTime);
        }

        public void InitPlot(string xAxis = "Time", string yAxis = "Viewers", string format = "HH:mm", bool relative = true)
        {
            if (PlotDataPoints == null) PlotDataPoints = new List<KeyValuePair<string, KeyValuePair<double, double>>>();

            viewerChart = new PlotModel();
            viewerChart.TextColor = OxyColor.FromRgb(175, 175, 175);
            viewerChart.PlotAreaBorderThickness = new OxyThickness(0);
            var valueAxisY = new OxyPlot.Axes.TimeSpanAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                TicklineColor = OxyColor.FromRgb(125, 125, 155),
                Title = yAxis,
                FontSize = 26,
                TitleFontSize = 26,
                AxislineThickness = 3,
                MinorGridlineThickness = 5,
                MajorGridlineThickness = 5,
                MajorGridlineStyle = LineStyle.Solid,
                FontWeight = 700,
                TitleFontWeight = 700,
                AxislineStyle = LineStyle.Solid,
                AxislineColor = OxyColor.FromRgb(125, 125, 155)
            };
            if (relative) valueAxisY.Minimum = 0;

            var valueAxisX = new OxyPlot.Axes.DateTimeAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                TicklineColor = OxyColor.FromRgb(125, 125, 155),
                Title = xAxis,
                FontSize = 26,
                TitleFontSize = 26,
                AxislineThickness = 3,
                MinorGridlineThickness = 5,
                MajorGridlineThickness = 5,
                MajorGridlineStyle = LineStyle.Solid,
                FontWeight = 700,
                TitleFontWeight = 700,
                AxislineStyle = LineStyle.Solid,
                AxislineColor = OxyColor.FromRgb(125, 125, 155),
                StringFormat = format
            };

            viewerChart.Axes.Add(valueAxisY);
            viewerChart.Axes.Add(valueAxisX);
            viewerChart.LegendFontSize = 24;
            viewerChart.LegendPosition = LegendPosition.BottomCenter;
            viewerChart.LegendBorder = OxyColor.FromRgb(125, 125, 155);
            viewerChart.LegendBackground = OxyColor.FromArgb(200, 46, 49, 54);
            viewerChart.LegendTextColor = OxyColor.FromRgb(175, 175, 175);

            areaSeries = new List<OxyPlot.Series.AreaSeries>();
            /*foreach (var plotPoint in PlotPoints)
            {
                AddValue(plotPoint.Key, plotPoint.Value, false);
            }*/

            if (PlotDataPoints.Count > 0)
            {
                PlotDataPoints = PlotDataPoints.Skip(Math.Max(0, PlotDataPoints.Count - 2000)).ToList();
                StartTime = DateTimeAxis.ToDateTime(PlotDataPoints.First().Value.Key);
                foreach (var dataPoint in PlotDataPoints)
                {
                    if (!MultipleLines) AddValue(dataPoint.Key, dataPoint.Value.Value, DateTimeAxis.ToDateTime(dataPoint.Value.Key), false, relative);
                    else AddValueSeperate(dataPoint.Key, dataPoint.Value.Value, DateTimeAxis.ToDateTime(dataPoint.Value.Key), false, relative);
                }
            }
        }

        /// <summary>
        /// Saves the plot as a .png and returns the URL.
        /// </summary>
        /// <returns>The URL</returns>
        public void DrawPlot(bool returnPdf = false, string fileName = null)
        {
            using (var stream = File.Create($"//var//www//html//StreamCharts//MopsKillerPlot.pdf"))
            {
                var pdfExporter = new PdfExporter { Width = 800 + ((PlotDataPoints.Count)/4), Height = 400 };
                pdfExporter.Export(viewerChart, stream);
            }
        }

        public void AddValue(string name, double viewerCount, DateTime? xValue = null, bool savePlot = true, bool relative = true)
        {
            if (xValue == null) xValue = DateTime.UtcNow;
            if (StartTime == null) StartTime = xValue;
            var relativeXValue = relative ? new DateTime(1970, 01, 01).Add((xValue - StartTime).Value) : xValue;

            if (areaSeries.LastOrDefault()?.Title?.Equals(name) ?? false)
            {
                areaSeries.Last().Points.Add(new DataPoint(DateTimeAxis.ToDouble(relativeXValue), viewerCount));
            }

            else
            {
                var series = new OxyPlot.Series.AreaSeries();
                //series.InterpolationAlgorithm = InterpolationAlgorithms.CatmullRomSpline;

                var colour = StringToColour(name);
                series.Color = colour;
                series.Fill = OxyColor.FromAColor(100, colour);
                series.Color2 = OxyColors.Transparent;

                if (!areaSeries.Any(x => x.Title?.Equals(name) ?? false))
                    series.Title = name;

                series.StrokeThickness = 3;
                areaSeries.LastOrDefault()?.Points?.Add(new DataPoint(DateTimeAxis.ToDouble(relativeXValue), viewerCount));
                series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(relativeXValue), viewerCount));
                viewerChart.Series.Add(series);
                areaSeries.Add(series);
            }

            if (savePlot)
            {
                PlotDataPoints.Add(new KeyValuePair<string, KeyValuePair<double, double>>(name, new KeyValuePair<double, double>(DateTimeAxis.ToDouble(xValue), viewerCount)));
                AdjustAxisRange();
            }
        }

        public void AddValueSeperate(string name, double viewerCount, DateTime? xValue = null, bool savePlot = true, bool relative = true)
        {
            if (xValue == null) xValue = DateTime.UtcNow;
            if (StartTime == null) StartTime = xValue;
            var relativeXValue = relative ? new DateTime(1970, 01, 01).Add((xValue - StartTime).Value) : xValue;

            if (areaSeries.FirstOrDefault(x => x.Title.Equals(name)) != null)
                areaSeries.FirstOrDefault(x => x.Title.Equals(name)).Points.Add(new DataPoint(DateTimeAxis.ToDouble(relativeXValue), viewerCount));

            else
            {
                var series = new OxyPlot.Series.AreaSeries();
                //series.InterpolationAlgorithm = InterpolationAlgorithms.CatmullRomSpline;
                var colour = StringToColour(name);
                series.Color = colour;
                series.Fill = OxyColor.FromAColor(100, colour);
                series.Color2 = OxyColors.Transparent;

                if (!areaSeries.Any(x => x.Title?.Equals(name) ?? false))
                    series.Title = name;

                series.StrokeThickness = 3;
                series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(relativeXValue), viewerCount));
                viewerChart.Series.Add(series);
                areaSeries.Add(series);
            }

            if (savePlot)
            {
                PlotDataPoints.Add(new KeyValuePair<string, KeyValuePair<double, double>>(name, new KeyValuePair<double, double>(DateTimeAxis.ToDouble(xValue), viewerCount)));
                AdjustAxisRange();
            }
        }

        public void AdjustAxisRange()
        {
            var axis = viewerChart.Axes.First(x => x.Position == OxyPlot.Axes.AxisPosition.Bottom);
            var yaxis = viewerChart.Axes.First(x => x.Position == OxyPlot.Axes.AxisPosition.Left);
            var max = areaSeries.Max(x => x.Points.Max(y => y.X));
            var min = areaSeries.Min(x => x.Points.Min(y => y.X));
            var ymin = areaSeries.Min(x => x.Points.Min(y => y.Y));
            foreach (var series in areaSeries)
            {
                series.ConstantY2 = ymin;
            }
            axis.AbsoluteMaximum = max;
            axis.AbsoluteMinimum = min;
            yaxis.AbsoluteMinimum = ymin;
        }

        public DataPoint? SetMaximumLine()
        {
            try
            {
                OxyPlot.Series.LineSeries max = viewerChart.Series.FirstOrDefault(x => x.Title.Contains("Max Value")) as OxyPlot.Series.LineSeries;

                if (max == null)
                {
                    max = new OxyPlot.Series.LineSeries();
                    max.Color = OxyColor.FromRgb(200, 0, 0);
                    max.StrokeThickness = 1;
                    viewerChart.Series.Add(max);
                }
                else
                    max.Points.Clear();

                DataPoint maxPoint = new DataPoint(0, 0);
                foreach (var series in areaSeries)
                {
                    foreach (var point in series.Points)
                    {
                        if (point.Y >= maxPoint.Y) maxPoint = point;
                    }
                }

                var ymin = areaSeries.Min(x => x.Points.Min(y => y.Y));
                max.Points.Add(maxPoint);
                max.Points.Add(new DataPoint(DateTimeAxis.ToDouble(DateTimeAxis.ToDateTime(maxPoint.X).AddMilliseconds(-1)), ymin));
                max.Title = "Max Value: " + maxPoint.Y;

                return maxPoint;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Removes all files created by the plot class to function.
        /// </summary>
        public void RemovePlot()
        {
            viewerChart = null;
            areaSeries = null;
            // var file = new FileInfo($"mopsdata//plots//{ID}plot.json");
            // file.Delete();
            var dir = new DirectoryInfo("mopsdata//");
            var files = dir.GetFiles().Where(x => x.Extension.ToLower().Equals($"{ID.ToLower()}plot.pdf"));
            foreach (var f in files)
                f.Delete();
        }

        /// Forces r, g or b to be bright enough for darkmode
        public static OxyColor StringToColour(string name)
        {
            if(name.Contains("Heartbeat")) return OxyColor.FromRgb(238, 38, 59);
            else return OxyColor.FromRgb(117, 64, 191);
        }

        public void Dispose()
        {
            RemovePlot();
        }
    }
}