using System;
using System.ComponentModel;
using System.Windows;
using Debugging.Traps;

namespace Debugging.Traps.WpfDemo
{
    public class PlcSignal : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        private string _status = "OK";
        public string Status 
        { 
            get => _status; 
            set { _status = value; OnPropertyChanged(nameof(Status)); } 
        }

        private double _temperature;
        public double Temperature 
        { 
            get => _temperature; 
            set { _temperature = value; OnPropertyChanged(nameof(Temperature)); } 
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public partial class MainWindow : Window
    {
        // 1. Declare our TrapList (which now supports INotifyCollectionChanged!)
        private readonly TrapList<PlcSignal> _signals;
        private int _signalCounter = 1;
        private readonly Random _rand = new Random();

        public MainWindow()
        {
            InitializeComponent();

            // 2. Initialize the TrapList
            _signals = new TrapList<PlcSignal>();

            // 3. Bind it DIRECTLY to the DataGrid. 
            // Because of our recent commit, this just works out of the box!
            DeviceDataGrid.ItemsSource = _signals;

            // 4. Configure our Traps (The magic part!)
            ConfigureTraps();

            // Add some initial data
            AddInitialData();
        }

        private void ConfigureTraps()
        {
            // Scenario 1: Trap any newly added device that is already overheating (> 80°C)
            _signals.OnAdd()
                .When(s => s.Temperature > 80.0)
                .Do(() => LogTrapEvent("🚨 TRAP [ADD]: Device joined in Overheat state!", true));

            // Scenario 2: Trap any device that goes offline or enters an error state
            _signals.OnUpdate()
                .When(s => s.Status == "ERROR" || s.Status == "OFFLINE")
                .Do(() => LogTrapEvent("❌ TRAP [UPDATE]: Device went OFFLINE/ERROR!", true));
                
            // Scenario 3: Log ALL additions just for tracking
            _signals.OnAdd()
                .Do(() => LogTrapEvent("ℹ️ Info: New device added."));
        }

        private void LogTrapEvent(string message, bool isError = false)
        {
            // Ensure we update UI on the Dispatcher thread (though traps here run on UI thread anyway)
            Dispatcher.Invoke(() =>
            {
                var time = DateTime.Now.ToString("HH:mm:ss.fff");
                LogListBox.Items.Insert(0, $"[{time}] {message}");
                
                // Keep log from growing infinitely
                if (LogListBox.Items.Count > 50) LogListBox.Items.RemoveAt(50);
            });
        }

        private void AddInitialData()
        {
            _signals.Add(new PlcSignal { Id = _signalCounter++, Name = "PLC-Main-Assembly", Status = "OK", Temperature = 45.2 });
            _signals.Add(new PlcSignal { Id = _signalCounter++, Name = "Conveyor-Belt-A", Status = "OK", Temperature = 38.5 });
            _signals.Add(new PlcSignal { Id = _signalCounter++, Name = "Cooling-Pump-01", Status = "OK", Temperature = 42.1 });
        }

        private void BtnAddNormal_Click(object sender, RoutedEventArgs e)
        {
            var newSignal = new PlcSignal 
            { 
                Id = _signalCounter++, 
                Name = $"Sensor-Node-{_signalCounter}", 
                Status = "OK", 
                Temperature = 35.0 + _rand.NextDouble() * 15.0 // 35-50 C
            };
            
            // This will trigger the "Info" trap, and the UI will auto-update
            _signals.Add(newSignal);
        }

        private void BtnAddCritical_Click(object sender, RoutedEventArgs e)
        {
            var newSignal = new PlcSignal 
            { 
                Id = _signalCounter++, 
                Name = $"Furnace-Controller-{_signalCounter}", 
                Status = "WARNING", 
                Temperature = 85.0 + _rand.NextDouble() * 10.0 // 85-95 C (Overheat!)
            };

            // This will trigger BOTH the "Info" trap and the "Overheat" trap!
            _signals.Add(newSignal);
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_signals.Count == 0) return;

            // Pick a random signal and "break" it
            int index = _rand.Next(_signals.Count);
            
            // We replace the item to trigger OnUpdate trap (SetItem)
            var oldSignal = _signals[index];
            var updatedSignal = new PlcSignal 
            { 
                Id = oldSignal.Id, 
                Name = oldSignal.Name, 
                Status = "ERROR", 
                Temperature = oldSignal.Temperature 
            };
            
            _signals[index] = updatedSignal;
        }

        private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            LogListBox.Items.Clear();
            _signals.Clear();
            _signalCounter = 1;
        }
    }
}