using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PontoDeEncontro.Models;
using PontoDeEncontro.Services;

namespace PontoDeEncontro
{
    public partial class PontoDeEncontroWindow : Window
    {
        private readonly DatabaseService _db;
        private readonly DispatcherTimer _timer;
        private List<Pessoa> _pessoasAreaInterna = new();
        private List<Pessoa> _pessoasPontoEncontro = new();

        public PontoDeEncontroWindow(DatabaseService db)
        {
            InitializeComponent();
            _db = db;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(8)
            };
            _timer.Tick += Timer_Tick;

            Loaded += PontoDeEncontroWindow_Loaded;
        }

        private void PontoDeEncontroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarEmpresas();

            if (chkAutoAtualizar.IsChecked == true)
                _timer.Start();
        }

        // ===================== CARREGAMENTO DE DADOS =====================

        private void CarregarEmpresas()
        {
            try
            {
                var empresas = _db.GetEmpresasVinculadasPontoDeEncontro();
                cmbEmpresa.ItemsSource = empresas;
                cmbEmpresa.DisplayMemberPath = "EmpDescricao";

                if (empresas.Count > 0)
                    cmbEmpresa.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                LogService.LogError("CarregarEmpresas", ex);
                txtStatus.Text = $"Erro ao carregar empresas: {ex.Message}";
            }
        }

        private void CarregarPontosDeEncontro(int empNumero)
        {
            try
            {
            var pontos = _db.GetPontosDeEncontroPorEmpresa(empNumero);
                cmbPontoEncontro.ItemsSource = pontos;
                cmbPontoEncontro.DisplayMemberPath = "AmbDescricao";

                lstPontosAtivos.ItemsSource = pontos.Select(p => p.AmbDescricao).ToList();

                if (pontos.Count > 0)
                    cmbPontoEncontro.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                LogService.LogError("CarregarPontosDeEncontro", ex);
                txtStatus.Text = $"Erro ao carregar pontos de encontro: {ex.Message}";
            }
        }

        private void CarregarListas()
        {
            if (cmbEmpresa.SelectedItem is not Empresa empresa)
                return;

            if (cmbPontoEncontro.SelectedItem is not Ambiente ambPonto)
                return;

            try
            {
                // Pessoas no Ponto de Encontro selecionado
                _pessoasPontoEncontro = _db.GetPessoasNoPontoDeEncontro(empresa.EmpNumero, ambPonto.AmbNumero);
                dgPontoEncontro.ItemsSource = _pessoasPontoEncontro;
                lblTotalPontoEncontro.Text = $"Pessoas no Ponto de Encontro: {_pessoasPontoEncontro.Count}";

                // Pessoas na Área Interna
                _pessoasAreaInterna = _db.GetPessoasAreaInterna(empresa.EmpNumero);
                dgAreaInterna.ItemsSource = _pessoasAreaInterna;
                lblTotalAreaInterna.Text = $"Total de Pessoas: {_pessoasAreaInterna.Count}";

                txtStatus.Text = $"Atualizado em {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                LogService.LogError("CarregarListas", ex);
                txtStatus.Text = $"Erro ao carregar listas: {ex.Message}";
            }
        }

        // ===================== EVENTOS DE COMBOS =====================

        private void CmbEmpresa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbEmpresa.SelectedItem is Empresa empresa)
            {
                CarregarPontosDeEncontro(empresa.EmpNumero);
            }
        }

        private void CmbPontoEncontro_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CarregarListas();
        }

        // ===================== SELEÇÃO NO GRID =====================

        private void DgAreaInterna_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgAreaInterna.SelectedItem is Pessoa pessoa)
                ExibirDetalhePessoaEsquerda(pessoa);
        }

        private void DgPontoEncontro_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPontoEncontro.SelectedItem is Pessoa pessoa)
                ExibirDetalhePessoaDireita(pessoa);
        }

        private void ExibirDetalhePessoaEsquerda(Pessoa pessoa)
        {
            txtDetPinEsq.Text = pessoa.Pin;
            txtDetNomeEsq.Text = pessoa.PesNome;
            txtDetRamalEsq.Text = pessoa.PesRamalCom;
            txtDetCelularEsq.Text = pessoa.PesCelular;

            try
            {
                imgPessoaEsq.Source = _db.GetFotoPessoa(pessoa.Pin);
            }
            catch
            {
                imgPessoaEsq.Source = null;
            }
        }

        private void ExibirDetalhePessoaDireita(Pessoa pessoa)
        {
            txtDetPinDir.Text = pessoa.Pin;
            txtDetNomeDir.Text = pessoa.PesNome;
            txtDetRamalDir.Text = pessoa.PesRamalCom;
            txtDetCelularDir.Text = pessoa.PesCelular;

            try
            {
                imgPessoaDir.Source = _db.GetFotoPessoa(pessoa.Pin);
            }
            catch
            {
                imgPessoaDir.Source = null;
            }
        }

        // ===================== BUSCA POR PIN / NOME =====================

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            var pinFiltro = txtPin.Text.Trim();
            var nomeFiltro = txtNome.Text.Trim();

            if (string.IsNullOrEmpty(pinFiltro) && string.IsNullOrEmpty(nomeFiltro))
                return;

            var filtrada = _pessoasAreaInterna.Where(p =>
            {
                bool match = true;
                if (!string.IsNullOrEmpty(pinFiltro))
                    match = p.Pin.Contains(pinFiltro, StringComparison.OrdinalIgnoreCase);
                if (match && !string.IsNullOrEmpty(nomeFiltro))
                    match = p.PesNome.Contains(nomeFiltro, StringComparison.OrdinalIgnoreCase);
                return match;
            }).ToList();

            dgAreaInterna.ItemsSource = filtrada;
            lblTotalAreaInterna.Text = $"Total de Pessoas: {filtrada.Count}";
        }

        // ===================== BOTÕES =====================

        private void BtnLimpar_Click(object sender, RoutedEventArgs e)
        {
            txtPin.Text = string.Empty;
            txtNome.Text = string.Empty;
            LimparDetalheEsquerda();
            LimparDetalheDireita();

            // Restaura lista completa da área interna
            dgAreaInterna.ItemsSource = _pessoasAreaInterna;
            lblTotalAreaInterna.Text = $"Total de Pessoas: {_pessoasAreaInterna.Count}";
        }

        private void BtnAtualizar_Click(object sender, RoutedEventArgs e)
        {
            CarregarListas();
        }

        private void BtnSair_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ===================== TIMER / AUTO ATUALIZAÇÃO =====================

        private void ChkAutoAtualizar_Changed(object sender, RoutedEventArgs e)
        {
            if (_timer == null) return;

            if (chkAutoAtualizar.IsChecked == true)
                _timer.Start();
            else
                _timer.Stop();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            CarregarListas();
        }

        // ===================== HELPERS =====================

        private void LimparDetalheEsquerda()
        {
            txtDetPinEsq.Text = string.Empty;
            txtDetNomeEsq.Text = string.Empty;
            txtDetRamalEsq.Text = string.Empty;
            txtDetCelularEsq.Text = string.Empty;
            imgPessoaEsq.Source = null;
        }

        private void LimparDetalheDireita()
        {
            txtDetPinDir.Text = string.Empty;
            txtDetNomeDir.Text = string.Empty;
            txtDetRamalDir.Text = string.Empty;
            txtDetCelularDir.Text = string.Empty;
            imgPessoaDir.Source = null;
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }
    }
}
