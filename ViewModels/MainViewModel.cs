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
using System.Windows.Input;

namespace TelemetryViewer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Country, Year, Session, Lap selection
        public ObservableCollection<string> Countries { get; set; } = new() { "Bahrain", "Belgium", "Italy", "Monaco", "Great Britain", "Hungary", "Netherlands", "Japan", "Singapore", "Australia", "Canada", "Austria", "Spain", "Azerbaijan", "Saudi Arabia", "United States", "Mexico", "Brazil", "Abu Dhabi", "Qatar" };
        public ObservableCollection<int> Years { get; set; } = new() { 2023, 2022, 2021, 2020, 2019 };
        public ObservableCollection<string> SessionTypes { get; set; } = new() { "Race", "Sprint", "Qualifying", "Practice 1", "Practice 2", "Practice 3" };
        private string _selectedCountry = "Belgium";
        public string SelectedCountry { get => _selectedCountry; set { _selectedCountry = value; OnPropertyChanged(); } }
        private int _selectedYear = 2023;
        public int SelectedYear { get => _selectedYear; set { _selectedYear = value; OnPropertyChanged(); } }
        private string _selectedSessionType = "Sprint";
        public string SelectedSessionType { get => _selectedSessionType; set { _selectedSessionType = value; OnPropertyChanged(); } }
        private string _selectedLapNumber = "1";
        public string SelectedLapNumber { get => _selectedLapNumber; set { _selectedLapNumber = value; OnPropertyChanged(); } }

        public ObservableCollection<SessionInfo> AvailableSessions { get; set; } = new();
        private SessionInfo _selectedSession;
        public SessionInfo SelectedSession { get => _selectedSession; set { _selectedSession = value; OnPropertyChanged(); } }

        // OpenF1 driver selection
        public ObservableCollection<DriverInfo> OpenF1Drivers { get; set; } = new()
        {
            new DriverInfo { Number = 44, Name = "Lewis Hamilton" },
            new DriverInfo { Number = 4, Name = "Lando Norris" },
            new DriverInfo { Number = 81, Name = "Oscar Piastri" },
            new DriverInfo { Number = 1, Name = "Max Verstappen" },
            new DriverInfo { Number = 55, Name = "Carlos Sainz" },
            new DriverInfo { Number = 63, Name = "George Russell" },
            new DriverInfo { Number = 11, Name = "Sergio Perez" },
            new DriverInfo { Number = 16, Name = "Charles Leclerc" },
            new DriverInfo { Number = 77, Name = "Valtteri Bottas" },
            new DriverInfo { Number = 24, Name = "Zhou Guanyu" },
            new DriverInfo { Number = 31, Name = "Esteban Ocon" },
            new DriverInfo { Number = 10, Name = "Pierre Gasly" },
            new DriverInfo { Number = 20, Name = "Kevin Magnussen" },
            new DriverInfo { Number = 27, Name = "Nico Hulkenberg" },
            new DriverInfo { Number = 22, Name = "Yuki Tsunoda" },
            new DriverInfo { Number = 23, Name = "Alex Albon" },
            new DriverInfo { Number = 2, Name = "Logan Sargeant" },
            new DriverInfo { Number = 3, Name = "Daniel Ricciardo" },
            new DriverInfo { Number = 14, Name = "Fernando Alonso" },
            new DriverInfo { Number = 18, Name = "Lance Stroll" }
        };
        private DriverInfo _selectedOpenF1Driver1;
        public DriverInfo SelectedOpenF1Driver1
        {
            get => _selectedOpenF1Driver1;
            set
            {
                if (_selectedOpenF1Driver1 != value)
                {
                    _selectedOpenF1Driver1 = value;
                    OnPropertyChanged();
                }
            }
        }
        private DriverInfo _selectedOpenF1Driver2;
        public DriverInfo SelectedOpenF1Driver2
        {
            get => _selectedOpenF1Driver2;
            set
            {
                if (_selectedOpenF1Driver2 != value)
                {
                    _selectedOpenF1Driver2 = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand LoadSessionsCommand => new RelayCommand(async _ => await LoadSessionsAsync());
        public ICommand LoadOpenF1DataCommand => new RelayCommand(async _ => await LoadOpenF1DataAsync());

        public MainViewModel() {
            _selectedOpenF1Driver1 = OpenF1Drivers.First(d => d.Number == 44);
            _selectedOpenF1Driver2 = OpenF1Drivers.First(d => d.Number == 4);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private async Task LoadSessionsAsync()
        {
            var openF1 = new OpenF1Service();
            var sessions = await openF1.GetSessionsAsync(SelectedCountry, SelectedSessionType, SelectedYear);
            AvailableSessions.Clear();
            foreach (var s in sessions)
                AvailableSessions.Add(s);
            SelectedSession = AvailableSessions.FirstOrDefault();
        }

        // OpenF1 data loading logic
        private async Task LoadOpenF1DataAsync()
        {
            if (SelectedSession == null) { LapComparisonSummary = ""; return; }
            if (!int.TryParse(SelectedLapNumber, out int lapNumber) || lapNumber <= 0) { LapComparisonSummary = ""; return; }
            var openF1 = new OpenF1Service();
            var driverInfos = new[] { SelectedOpenF1Driver1, SelectedOpenF1Driver2 };
            var series = new List<ISeries>();
            Sections.Clear();

            double?[] lapTimes = new double?[2];

            for (int i = 0; i < driverInfos.Length; i++)
            {
                var driver = driverInfos[i];
                var laps = await openF1.GetLapSummariesAsync(SelectedSession.session_key, driver.Number);
                if (laps.Count > 0)
                {
                    var filteredLaps = laps.Where(l => l.lap_number == lapNumber).ToList();
                    if (!filteredLaps.Any()) continue;
                    lapTimes[i] = filteredLaps[0].lap_duration;
                    series.Add(new LineSeries<ObservablePoint>
                    {
                        Values = filteredLaps
                            .Where(l => l.lap_duration.HasValue && l.lap_number.HasValue)
                            .Select(l => new ObservablePoint(l.lap_number.Value, l.lap_duration.Value)).ToList(),
                        Name = $"Lap Time ({driver.Name})",
                        Stroke = new SolidColorPaint(SKColors.Blue, 2),
                        Fill = null
                    });
                }
            }
            TelemetrySeries = series.ToArray();

            // Set summary
            if (lapTimes[0].HasValue && lapTimes[1].HasValue)
            {
                double diff = lapTimes[0].Value - lapTimes[1].Value;
                if (Math.Abs(diff) < 0.001)
                {
                    LapComparisonSummary = $"Both drivers set the same lap time: {lapTimes[0]:0.000} s.";
                }
                else if (diff < 0)
                {
                    LapComparisonSummary = $"{SelectedOpenF1Driver1.Name} was faster by {Math.Abs(diff):0.000} s.";
                }
                else
                {
                    LapComparisonSummary = $"{SelectedOpenF1Driver2.Name} was faster by {Math.Abs(diff):0.000} s.";
                }
            }
            else
            {
                LapComparisonSummary = "Lap time data not available for both drivers.";
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

        private string _lapComparisonSummary;
        public string LapComparisonSummary
        {
            get => _lapComparisonSummary;
            set { _lapComparisonSummary = value; OnPropertyChanged(); }
        }
    }

    // RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
