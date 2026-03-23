using System;
using System.Windows;
using System.Windows.Threading;
using PontoDeEncontro.Services;
            
namespace PontoDeEncontro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _db;
        private readonly DispatcherTimer _timerPolling;
        private PontoDeEncontroWindow? _pontoWindow;

        public MainWindow(DatabaseService databaseService)
        {
            InitializeComponent();
            _db = databaseService;

            _timerPolling = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(8)
            };
            _timerPolling.Tick += TimerPolling_Tick;
            _timerPolling.Start();
        }

        private void TimerPolling_Tick(object? sender, EventArgs e)
        {
            try
            {
                bool existe = _db.ExistePessoaNoPontoDeEncontro();

                if (existe)
                {
                    txtMonitorStatus.Text = $"Pessoa detectada! ({DateTime.Now:HH:mm:ss})";
                    AbrirTelaPontoDeEncontro();
                }
                else
                {
                    txtMonitorStatus.Text = $"Nenhuma pessoa no ponto de encontro. ({DateTime.Now:HH:mm:ss})";
                }
            }
            catch (Exception ex)
            {
                txtMonitorStatus.Text = $"Erro: {ex.Message}";
            }
        }

        private void AbrirTelaPontoDeEncontro()
        {
            // Se a janela já estiver aberta, apenas traz para frente
            if (_pontoWindow != null && _pontoWindow.IsLoaded)
            {
                _pontoWindow.Activate();
                return;
            }

            _pontoWindow = new PontoDeEncontroWindow(_db);
            _pontoWindow.Closed += (s, args) => _pontoWindow = null;
            _pontoWindow.Show();
        }

        private void BtnAbrirManual_Click(object sender, RoutedEventArgs e)
        {
            AbrirTelaPontoDeEncontro();
        }

        protected override void OnClosed(EventArgs e)
        {
            _timerPolling.Stop();
            _pontoWindow?.Close();
            base.OnClosed(e);
        }
    }
}