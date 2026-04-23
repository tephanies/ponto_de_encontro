using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System
    .Windows;
using System.Windows.Threading;
using PontoDeEncontro.Services;
            


namespace PontoDeEncontro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ConnectionString = "Server=LACERDA;Database=WSP1;User Id=sa;Password=sa123";
        private readonly DatabaseService _db;
        private readonly DispatcherTimer _timerPolling;
        private PontoDeEncontroWindow? _pontoWindow;

        public MainWindow() : this(new DatabaseService(ConnectionString))
        {
        }

        public MainWindow(DatabaseService db)
        {
            InitializeComponent();

            _db = db;

            _timerPolling = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _timerPolling.Tick += TimerPolling_Tick;
            _timerPolling.Start();
        }

        private void TimerPolling_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (_pontoWindow != null && _pontoWindow.IsLoaded) return;

                var events = _db.GetPendingPontoEvents();
                if (events.Count > 0)
                {
                    var ids = events.Select(ev => ev.Id).ToArray();
                    _db.MarkEventsProcessed(ids);

                    OpenPontoWindow();
                    txtMonitorStatus.Text = $"Evento detectado ({events.Count}) - janela aberta.";
                }
                else
                {
                    txtMonitorStatus.Text = $"Aguardando... ({DateTime.Now:HH:mm:ss})";
                }
            }
            catch (Exception ex)
            {
                txtMonitorStatus.Text = $"Erro: {ex.Message}";
            }
        }

        private void OpenPontoWindow()
        {
            if (_pontoWindow != null && _pontoWindow.IsLoaded)
            {
                _pontoWindow.Activate();
                return;
            }

            _pontoWindow = new PontoDeEncontroWindow(_db);
            _pontoWindow.Closed += (_, _) => _pontoWindow = null;
            _pontoWindow.Show();
        }

        private void BtnAbrirManual_Click(object sender, RoutedEventArgs e) => OpenPontoWindow();

        protected override void OnClosed(EventArgs e)
        {
            _timerPolling.Stop();
            _pontoWindow?.Close();
            base.OnClosed(e);
        }
    }
}