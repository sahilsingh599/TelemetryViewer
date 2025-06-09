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
                if (value != null)
                {
                    _ = LoadTelemetryFromFileAsync(value.FilePath);
                }
            }
        }

        public MainViewModel()
        {
            XAxes = new Axis[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColors.Black)
                }
            };

            YAxes = new Axis[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColors.Black)
                }
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

        public async Task LoadTelemetryFromFileAsync(string path)
        {
            var loader = new TelemetryDataService();
            var lap = await loader.LoadFromFileAsync(path);

            var speed = new List<ObservablePoint>();
            var throttle = new List<ObservablePoint>();
            var brake = new List<ObservablePoint>();

            foreach (var point in lap.data)
            {
                speed.Add(new ObservablePoint(point.Time, point.Speed));
                throttle.Add(new ObservablePoint(point.Time, point.Throttle));
                brake.Add(new ObservablePoint(point.Time, point.Brake));
            }

            TelemetrySeries = new ISeries[]
            {
                new LineSeries<ObservablePoint> { Values = speed, Name = "Speed", Stroke = new SolidColorPaint(SKColors.Blue, 2), Fill = null },
                new LineSeries<ObservablePoint> { Values = throttle, Name = "Throttle", Stroke = new SolidColorPaint(SKColors.Green, 2), Fill = null },
                new LineSeries<ObservablePoint> { Values = brake, Name = "Brake", Stroke = new SolidColorPaint(SKColors.Red, 2), Fill = null }
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
