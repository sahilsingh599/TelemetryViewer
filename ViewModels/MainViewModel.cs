using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TelemetryViewer.Models;
using TelemetryViewer.Services;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;

namespace TelemetryViewer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<LapFileEntry> AvailableLaps { get; set; } = new();

        private LapFileEntry? _selectedLap;
        public LapFileEntry? SelectedLap
        {
            get => _selectedLap;
            set
            {
                _selectedLap = value;
                OnPropertyChanged();
                _ = LoadBothLapsAsync();
            }
        }

        private LapFileEntry? _comparisonLap;
        public LapFileEntry? ComparisonLap
        {
            get => _comparisonLap;
            set
            {
                _comparisonLap = value;
                OnPropertyChanged();
                _ = LoadBothLapsAsync();
            }
        }

        public ISeries[] TelemetrySeries
        {
            get => _telemetrySeries;
            set
            {
                _telemetrySeries = value;
                OnPropertyChanged();
            }
        }
        private ISeries[] _telemetrySeries = [];

        public Axis[] XAxes { get; set; } = new Axis[]
        {
            new Axis { LabelsPaint = new SolidColorPaint(SKColors.Black) }
        };

        public Axis[] YAxes { get; set; } = new Axis[]
        {
            new Axis { LabelsPaint = new SolidColorPaint(SKColors.Black) }
        };

        public ObservableCollection<RectangularSection> Sections { get; set; } = new();

        // Checkbox toggles
        public bool ShowSpeed { get; set; } = true;
        public bool ShowThrottle { get; set; } = true;
        public bool ShowBrake { get; set; } = true;
        public bool ShowGear { get; set; } = true;
        public bool ShowDelta { get; set; } = true;

        public MainViewModel() { }

        public async Task InitializeAsync()
        {
            await LoadLapListAsync();
        }

        private async Task LoadLapListAsync()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Laps");
            if (!Directory.Exists(folder)) return;

            var files = Directory.GetFiles(folder, "*.json");
            foreach (var file in files)
            {
                var display = Path.GetFileNameWithoutExtension(file).Replace("_", " ");
                AvailableLaps.Add(new LapFileEntry { FilePath = file, DisplayName = display });
            }

            if (AvailableLaps.Count > 0)
                SelectedLap = AvailableLaps[0];
        }

        private async Task LoadBothLapsAsync()
        {
            if (SelectedLap == null) return;

            var loader = new TelemetryDataService();
            var mainLap = await loader.LoadFromFileAsync(SelectedLap.FilePath);
            LapData? compLap = null;
            if (ComparisonLap != null && ComparisonLap.FilePath != SelectedLap.FilePath)
                compLap = await loader.LoadFromFileAsync(ComparisonLap.FilePath);

            var series = new List<ISeries>();
            Sections.Clear();

            // Calculate distance for both laps
            ComputeDistance(mainLap);
            if (compLap != null) ComputeDistance(compLap);

            double maxDist = mainLap.data.Last().Distance;
            double sectorLen = maxDist / 3;

            for (int i = 0; i < 3; i++)
            {
                Sections.Add(new RectangularSection
                {
                    Xi = i * sectorLen,
                    Xj = (i + 1) * sectorLen,
                    Label = $"Sector {i + 1}",
                    LabelPaint = new SolidColorPaint(SKColors.Black),
                    LabelSize = 14,
                    Fill = new SolidColorPaint(SKColors.LightGray.WithAlpha(40))
                });
            }

            if (mainLap.data.Count > 0 && ShowSpeed)
                series.Add(new LineSeries<ObservablePoint>
                {
                    Values = mainLap.data.Select(p => new ObservablePoint(p.Distance, p.Speed)).ToList(),
                    Name = $"Speed ({mainLap.driver})",
                    Stroke = new SolidColorPaint(SKColors.Blue, 2),
                    Fill = null
                });

            if (mainLap.data.Count > 0 && ShowThrottle)
                series.Add(new LineSeries<ObservablePoint>
                {
                    Values = mainLap.data.Select(p => new ObservablePoint(p.Distance, p.Throttle)).ToList(),
                    Name = $"Throttle ({mainLap.driver})",
                    Stroke = new SolidColorPaint(SKColors.Green, 2),
                    Fill = null
                });

            if (mainLap.data.Count > 0 && ShowBrake)
                series.Add(new LineSeries<ObservablePoint>
                {
                    Values = mainLap.data.Select(p => new ObservablePoint(p.Distance, p.Brake)).ToList(),
                    Name = $"Brake ({mainLap.driver})",
                    Stroke = new SolidColorPaint(SKColors.Red, 2),
                    Fill = null
                });

            if (mainLap.data.Count > 0 && ShowGear)
                series.Add(new LineSeries<ObservablePoint>
                {
                    Values = mainLap.data.Select(p => new ObservablePoint(p.Distance, p.Gear)).ToList(),
                    Name = $"Gear ({mainLap.driver})",
                    Stroke = new SolidColorPaint(SKColors.Purple, 2),
                    Fill = null,
                    GeometrySize = 0
                });

            if (compLap != null && compLap.data.Count > 0)
            {
                if (ShowSpeed)
                    series.Add(new LineSeries<ObservablePoint>
                    {
                        Values = compLap.data.Select(p => new ObservablePoint(p.Distance, p.Speed)).ToList(),
                        Name = $"Speed ({compLap.driver})",
                        Stroke = new SolidColorPaint(SKColors.LightBlue, 2),
                        Fill = null
                    });

                if (ShowThrottle)
                    series.Add(new LineSeries<ObservablePoint>
                    {
                        Values = compLap.data.Select(p => new ObservablePoint(p.Distance, p.Throttle)).ToList(),
                        Name = $"Throttle ({compLap.driver})",
                        Stroke = new SolidColorPaint(SKColors.LightGreen, 2),
                        Fill = null
                    });

                if (ShowBrake)
                    series.Add(new LineSeries<ObservablePoint>
                    {
                        Values = compLap.data.Select(p => new ObservablePoint(p.Distance, p.Brake)).ToList(),
                        Name = $"Brake ({compLap.driver})",
                        Stroke = new SolidColorPaint(SKColors.OrangeRed, 2),
                        Fill = null
                    });

                if (ShowGear)
                    series.Add(new LineSeries<ObservablePoint>
                    {
                        Values = compLap.data.Select(p => new ObservablePoint(p.Distance, p.Gear)).ToList(),
                        Name = $"Gear ({compLap.driver})",
                        Stroke = new SolidColorPaint(SKColors.MediumPurple, 2),
                        Fill = null,
                        GeometrySize = 0
                    });

                if (ShowDelta)
                {
                    var mainSample = Resample(mainLap.data, 5);
                    var compSample = Resample(compLap.data, 5);
                    var deltaPoints = new List<ObservablePoint>();
                    double cumulative = 0;

                    deltaPoints.Add(new ObservablePoint(mainSample[0].Distance, 0));
                    for (int i = 1; i < Math.Min(mainSample.Count, compSample.Count); i++)
                    {
                        double d = (compSample[i].Time - compSample[i - 1].Time) -
                                   (mainSample[i].Time - mainSample[i - 1].Time);
                        cumulative += d;
                        deltaPoints.Add(new ObservablePoint(mainSample[i].Distance, cumulative));
                    }

                    series.Add(new LineSeries<ObservablePoint>
                    {
                        Values = deltaPoints,
                        Name = $"Δt ({compLap.driver} - {mainLap.driver})",
                        Stroke = new SolidColorPaint(SKColors.Orange, 2)
                        {
                            PathEffect = new DashEffect(new float[] { 6, 6 })
                        },
                        Fill = null,
                        GeometrySize = 0
                    });
                }
            }

            TelemetrySeries = series.ToArray();
        }

        private void ComputeDistance(LapData lap)
        {
            double cumulative = 0;
            lap.data[0].Distance = 0;
            for (int i = 1; i < lap.data.Count; i++)
            {
                var dt = lap.data[i].Time - lap.data[i - 1].Time;
                cumulative += lap.data[i].Speed * dt / 3.6;
                lap.data[i].Distance = cumulative;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        List<(double Distance, double Time)> Resample(List<TelemetryPoint> data, double interval)
        {
            var result = new List<(double, double)>();
            double maxDist = data.Last().Distance;

            for (double d = 0; d <= maxDist; d += interval)
            {
                var left = data.LastOrDefault(p => p.Distance <= d);
                var right = data.FirstOrDefault(p => p.Distance >= d);

                if (left != null && right != null && left != right)
                {
                    double frac = (d - left.Distance) / (right.Distance - left.Distance);
                    double time = left.Time + frac * (right.Time - left.Time);
                    result.Add((d, time));
                }
            }

            return result;
        }
    }
}
