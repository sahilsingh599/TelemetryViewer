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

            var loader = new TelemetryDataService();
            var mainLap = await loader.LoadFromFileAsync(SelectedLap.FilePath);
            LapData? compLap = null;

            if (ComparisonLap != null && ComparisonLap.FilePath != SelectedLap.FilePath)
            {
                compLap = await loader.LoadFromFileAsync(ComparisonLap.FilePath);
            }

            var series = new List<ISeries>();

            // Primary lap series
            var mainSpeed = new List<ObservablePoint>();
            var mainThrottle = new List<ObservablePoint>();
            var mainBrake = new List<ObservablePoint>();

            foreach (var point in mainLap.data)
            {
                mainSpeed.Add(new ObservablePoint(point.Time, point.Speed));
                mainThrottle.Add(new ObservablePoint(point.Time, point.Throttle));
                mainBrake.Add(new ObservablePoint(point.Time, point.Brake));
            }

            series.Add(new LineSeries<ObservablePoint> { Values = mainSpeed, Name = $"Speed ({mainLap.driver})", Stroke = new SolidColorPaint(SKColors.Blue, 2), Fill = null });
            series.Add(new LineSeries<ObservablePoint> { Values = mainThrottle, Name = $"Throttle ({mainLap.driver})", Stroke = new SolidColorPaint(SKColors.Green, 2), Fill = null });
            series.Add(new LineSeries<ObservablePoint> { Values = mainBrake, Name = $"Brake ({mainLap.driver})", Stroke = new SolidColorPaint(SKColors.Red, 2), Fill = null });

            // Comparison lap series
            if (compLap != null)
            {
                var compSpeed = new List<ObservablePoint>();
                var compThrottle = new List<ObservablePoint>();
                var compBrake = new List<ObservablePoint>();

                foreach (var point in compLap.data)
                {
                    compSpeed.Add(new ObservablePoint(point.Time, point.Speed));
                    compThrottle.Add(new ObservablePoint(point.Time, point.Throttle));
                    compBrake.Add(new ObservablePoint(point.Time, point.Brake));
                }

                series.Add(new LineSeries<ObservablePoint>
                {
                    Values = compSpeed,
                    Name = $"Speed ({compLap.driver})",
                    Stroke = new SolidColorPaint(SKColors.LightBlue, 2),
                    Fill = null,
                    LineSmoothness = 0,
                    GeometrySize = 0
                });

                series.Add(new LineSeries<ObservablePoint>
                {
                    Values = compThrottle,
                    Name = $"Throttle ({compLap.driver})",
                    Stroke = new SolidColorPaint(SKColors.LightGreen, 2),
                    Fill = null,
                    LineSmoothness = 0,
                    GeometrySize = 0
                });

                series.Add(new LineSeries<ObservablePoint>
                {
                    Values = compBrake,
                    Name = $"Brake ({compLap.driver})",
                    Stroke = new SolidColorPaint(SKColors.OrangeRed, 2),
                    Fill = null,
                    LineSmoothness = 0,
                    GeometrySize = 0
                });
            }

            TelemetrySeries = series.ToArray();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
