using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Defaults;
using SkiaSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading.Tasks;
using TelemetryViewer.Models;
using TelemetryViewer.Services;
using System;
using System.Linq;
using LiveChartsCore.SkiaSharpView.Painting.Effects;


namespace TelemetryViewer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ISeries[] _telemetrySeries = [];
        public ISeries[] TelemetrySeries
        {
            get => _telemetrySeries;
            set
            {
                _telemetrySeries = value;
                OnPropertyChanged();
            }
        }

        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        public ObservableCollection<LapFileEntry> AvailableLaps { get; set; } = new();

        private LapFileEntry? _selectedLap;
        public LapFileEntry? SelectedLap
        {
            get => _selectedLap;
            set
            {
                _selectedLap = value;
                OnPropertyChanged();
                LoadBothLaps();
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
                LoadBothLaps();
            }
        }

        private SyncMode _syncMode = SyncMode.alignStart;
        public SyncMode SyncMode
        {
            get => _syncMode;
            set
            {
                _syncMode = value;
                OnPropertyChanged();
                LoadBothLaps();
            }
        }


        public MainViewModel()
        {
            XAxes = new Axis[]
            {
                new Axis { LabelsPaint = new SolidColorPaint(SKColors.Black) }
            };

            YAxes = new Axis[]
            {
                new Axis { LabelsPaint = new SolidColorPaint(SKColors.Black) }
            };

            LoadLapList();
        }

        private void LoadLapList()
        {
            string lapFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Laps");
            if (Directory.Exists(lapFolder))
            {
                var files = Directory.GetFiles(lapFolder, "*.json");
                foreach (var file in files)
                {
                    var display = Path.GetFileNameWithoutExtension(file).Replace("_", " ");
                    AvailableLaps.Add(new LapFileEntry { FilePath = file, DisplayName = display });
                }

                if (AvailableLaps.Count > 0)
                {
                    SelectedLap = AvailableLaps[0];
                }
            }
        }

        private async void LoadBothLaps()
        {
            if (SelectedLap == null) return;

            Console.WriteLine($"Loading primary lap: {SelectedLap.FilePath}");
            var loader = new TelemetryDataService();
            var mainLap = await loader.LoadFromFileAsync(SelectedLap.FilePath);
            LapData? compLap = null;

            if (ComparisonLap != null && ComparisonLap.FilePath != SelectedLap.FilePath)
            {
                Console.WriteLine($"Loading comparison lap: {ComparisonLap.FilePath}");
                compLap = await loader.LoadFromFileAsync(ComparisonLap.FilePath);
            }

            var series = new List<ISeries>();
            var deltaPoints = new List<ObservablePoint>();
            double offset = 0;

            if (mainLap?.data?.Count > 0)
            {
                var speed = new List<ObservablePoint>();
                var throttle = new List<ObservablePoint>();
                var brake = new List<ObservablePoint>();
                var gear = new List<ObservablePoint>();
                double cumulative = 0;

                foreach (var point in mainLap.data)
                {
                    speed.Add(new ObservablePoint(point.Time, point.Speed));
                    throttle.Add(new ObservablePoint(point.Time, point.Throttle));
                    brake.Add(new ObservablePoint(point.Time, point.Brake));
                    gear.Add(new ObservablePoint(point.Time, point.Gear));
                }

                series.Add(new LineSeries<ObservablePoint> { Values = speed, Name = $"Speed ({mainLap.driver})", Stroke = new SolidColorPaint(SKColors.Blue, 2), Fill = null });
                series.Add(new LineSeries<ObservablePoint> { Values = throttle, Name = $"Throttle ({mainLap.driver})", Stroke = new SolidColorPaint(SKColors.Green, 2), Fill = null });
                series.Add(new LineSeries<ObservablePoint> { Values = brake, Name = $"Brake ({mainLap.driver})", Stroke = new SolidColorPaint(SKColors.Red, 2), Fill = null });
                series.Add(new LineSeries<ObservablePoint> { Values = gear, Name = $"Gear ({mainLap.driver})", Stroke = new SolidColorPaint(SKColors.Purple, 2), Fill = null });

                for (int i = 1; i < mainLap.data.Count; i++)
                {
                    var prev = mainLap.data[i - 1];
                    var current = mainLap.data[i];

                    double dt = current.Time - prev.Time; // seconds
                    cumulative += current.Speed * dt / 3.6; // convert km/h to m/s

                    current.Distance = cumulative;
                }
                mainLap.data[0].Distance = 0; // ensure first point is zero

            }

            if (compLap?.data?.Count > 0)
            {


                if (SyncMode == SyncMode.alignEnd)
                {
                    offset = mainLap.data[^1].Time - compLap.data[^1].Time;
                }

                var compSpeed = new List<ObservablePoint>();
                var compThrottle = new List<ObservablePoint>();
                var compBrake = new List<ObservablePoint>();
                var compGear = new List<ObservablePoint>();
                double cumulative = 0;

                foreach (var point in compLap.data)
                {
                    double t = point.Time + offset;
                    compSpeed.Add(new ObservablePoint(t, point.Speed));
                    compThrottle.Add(new ObservablePoint(t, point.Throttle));
                    compBrake.Add(new ObservablePoint(t, point.Brake));
                    compGear.Add(new ObservablePoint(t, point.Gear));
                }

                for (int i = 1; i < compLap.data.Count; i++)
                {
                    var prev = compLap.data[i - 1];
                    var current = compLap.data[i];

                    double dt = current.Time - prev.Time; // seconds
                    cumulative += current.Speed * dt / 3.6; // convert km/h to m/s

                    current.Distance = cumulative;
                }
                compLap.data[0].Distance = 0;

                series.Add(new LineSeries<ObservablePoint> { Values = compSpeed, Name = $"Speed ({compLap.driver})", Stroke = new SolidColorPaint(SKColors.LightBlue, 2), Fill = null });
                series.Add(new LineSeries<ObservablePoint> { Values = compThrottle, Name = $"Throttle ({compLap.driver})", Stroke = new SolidColorPaint(SKColors.LightGreen, 2), Fill = null });
                series.Add(new LineSeries<ObservablePoint> { Values = compBrake, Name = $"Brake ({compLap.driver})", Stroke = new SolidColorPaint(SKColors.OrangeRed, 2), Fill = null });
                series.Add(new LineSeries<ObservablePoint> { Values = compGear, Name = $"Gear ({compLap.driver})", Stroke = new SolidColorPaint(SKColors.MediumPurple, 2), Fill = null });
            }

            if (mainLap.data.Count > 0 && compLap?.data?.Count > 0)
            {
                // Time-based delta (Speed difference)
                var speedDeltaPoints = new List<ObservablePoint>();

                foreach (var mainPoint in mainLap.data)
                {
                    var closestCompPoint = compLap.data
                        .OrderBy(p => Math.Abs((p.Time + offset) - mainPoint.Time))
                        .FirstOrDefault();

                    if (closestCompPoint != null)
                    {
                        double deltaSpeed = mainPoint.Speed - closestCompPoint.Speed;
                        speedDeltaPoints.Add(new ObservablePoint(mainPoint.Time, deltaSpeed));
                    }
                }

                series.Add(new LineSeries<ObservablePoint>
                {
                    Values = speedDeltaPoints,
                    Name = $"Speed Δ ({mainLap.driver} - {compLap.driver})",
                    Stroke = new SolidColorPaint(SKColors.DarkMagenta, 2),
                    Fill = null,
                    GeometrySize = 0
                });

                // Distance-based cumulative time delta
                var mainSample = Resample(mainLap.data, 5); // every 5 meters
                var compSample = Resample(compLap.data, 5);
                var timeDeltaPoints = new List<ObservablePoint>();

                for (int i = 0; i < Math.Min(mainSample.Count, compSample.Count); i++)
                {
                    var delta = compSample[i].Time - mainSample[i].Time;
                    timeDeltaPoints.Add(new ObservablePoint(mainSample[i].Distance, delta));
                }

                series.Add(new LineSeries<ObservablePoint>
                {
                    Values = timeDeltaPoints,
                    Name = $"Δt ({compLap.driver} - {mainLap.driver})",
                    Stroke = new SolidColorPaint(SKColors.DarkRed, 2)
                    {
                        PathEffect = new DashEffect(new float[] { 4, 4 })
                    },
                    Fill = null,
                    GeometrySize = 0
                });
            }


            Console.WriteLine($"Final series count: {series.Count}");
            TelemetrySeries = series.ToArray();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
