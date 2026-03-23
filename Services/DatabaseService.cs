using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Media.Imaging;
using PontoDeEncontro.Models;

namespace PontoDeEncontro.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Retorna todos os ambientes marcados como Ponto de Encontro.
        /// </summary>
        public List<Ambiente> GetAmbientesPontoDeEncontro()
        {
            var lista = new List<Ambiente>();
            const string sql = "SELECT ambNumero, ambPontoEncontro, ambExterno FROM Ambiente WHERE ambPontoEncontro = 1";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Ambiente
                {
                    AmbNumero = reader.GetInt32(0),
                    AmbPontoEncontro = reader.GetInt32(1) == 1,
                    AmbExterno = reader.GetInt32(2) == 1
                });
            }

            return lista;
        }

        /// <summary>
        /// Retorna todos os ambientes internos (não externo e não ponto de encontro).
        /// </summary>
        public List<int> GetAmbientesInternos()
        {
            var lista = new List<int>();
            const string sql = "SELECT ambNumero FROM Ambiente WHERE ambExterno = 0 AND ambPontoEncontro = 0";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
                lista.Add(reader.GetInt32(0));

            return lista;
        }

        /// <summary>
        /// Verifica se existe alguma pessoa habilitada em algum ambiente de ponto de encontro.
        /// </summary>
        public bool ExistePessoaNoPontoDeEncontro()
        {
            const string sql = @"
                SELECT TOP 1 1 
                FROM Pessoas p
                INNER JOIN Ambiente a ON p.ambNumero = a.ambNumero
                WHERE a.ambPontoEncontro = 1 AND p.pesHabilitado = 1";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            var result = cmd.ExecuteScalar();
            return result != null;
        }

        /// <summary>
        /// Retorna as empresas vinculadas a ambientes de ponto de encontro.
        /// </summary>
        public List<Empresa> GetEmpresasVinculadasPontoDeEncontro()
        {
            var lista = new List<Empresa>();
            const string sql = @"
                SELECT DISTINCT e.empNumero, e.empDescricao
                FROM Empresa e
                INNER JOIN LinkEmpresaAmbientes lea ON e.empNumero = lea.empNumero
                INNER JOIN Ambiente a ON lea.ambNumero = a.ambNumero
                WHERE a.ambPontoEncontro = 1
                ORDER BY e.empDescricao";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Empresa
                {
                    EmpNumero = reader.GetInt32(0),
                    EmpDescricao = reader.GetString(1)
                });
            }

            return lista;
        }

        /// <summary>
        /// Retorna pessoas habilitadas em ambientes vinculados à empresa selecionada,
        /// que estejam em ambientes de Ponto de Encontro.
        /// </summary>
        public List<Pessoa> GetPessoasNoPontoDeEncontro(int empNumero, int ambPontoEncontroNumero)
        {
            const string sql = @"
                SELECT p.Pin, p.pesNome, ISNULL(p.pesRamalCom,''), ISNULL(p.pesCelular,''), p.empNumero, p.ambNumero
                FROM Pessoas p
                INNER JOIN LinkEmpresaAmbientes lea ON lea.empNumero = @empNumero AND lea.ambNumero = p.ambNumero
                WHERE p.pesHabilitado = 1
                  AND p.ambNumero = @ambPontoEncontro";

            return ExecutarConsultaPessoas(sql, new[]
            {
                new SqlParameter("@empNumero", empNumero),
                new SqlParameter("@ambPontoEncontro", ambPontoEncontroNumero)
            });
        }

        /// <summary>
        /// Retorna pessoas habilitadas em ambientes internos vinculados à empresa selecionada.
        /// (não externo e não ponto de encontro)
        /// </summary>
        public List<Pessoa> GetPessoasAreaInterna(int empNumero)
        {
            const string sql = @"
                SELECT p.Pin, p.pesNome, ISNULL(p.pesRamalCom,''), ISNULL(p.pesCelular,''), p.empNumero, p.ambNumero
                FROM Pessoas p
                INNER JOIN LinkEmpresaAmbientes lea ON lea.empNumero = @empNumero AND lea.ambNumero = p.ambNumero
                INNER JOIN Ambiente a ON p.ambNumero = a.ambNumero
                WHERE p.pesHabilitado = 1
                  AND a.ambExterno = 0
                  AND a.ambPontoEncontro = 0";

            return ExecutarConsultaPessoas(sql, new[]
            {
                new SqlParameter("@empNumero", empNumero)
            });
        }

        /// <summary>
        /// Retorna a foto da pessoa pelo PIN.
        /// </summary>
        public BitmapImage? GetFotoPessoa(string pin)
        {
            const string sql = "SELECT fotFoto FROM Fotos WHERE Pin = @pin";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pin", pin);
            var result = cmd.ExecuteScalar();

            if (result == null || result == DBNull.Value)
                return null;

            var bytes = (byte[])result;
            return BytesToBitmapImage(bytes);
        }

        /// <summary>
        /// Retorna os ambientes de ponto de encontro vinculados a uma empresa.
        /// </summary>
        public List<Ambiente> GetPontosDeEncontroPorEmpresa(int empNumero)
        {
            var lista = new List<Ambiente>();
            const string sql = @"
                SELECT a.ambNumero, a.ambPontoEncontro, a.ambExterno
                FROM Ambiente a
                INNER JOIN LinkEmpresaAmbientes lea ON a.ambNumero = lea.ambNumero
                WHERE lea.empNumero = @empNumero AND a.ambPontoEncontro = 1";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@empNumero", empNumero);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Ambiente
                {
                    AmbNumero = reader.GetInt32(0),
                    AmbPontoEncontro = reader.GetInt32(1) == 1,
                    AmbExterno = reader.GetInt32(2) == 1
                });
            }

            return lista;
        }

        private List<Pessoa> ExecutarConsultaPessoas(string sql, SqlParameter[] parameters)
        {
            var lista = new List<Pessoa>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Pessoa
                {
                    Pin = reader.GetValue(0).ToString() ?? string.Empty,
                    PesNome = reader.GetValue(1).ToString() ?? string.Empty,
                    PesRamalCom = reader.GetValue(2).ToString() ?? string.Empty,
                    PesCelular = reader.GetValue(3).ToString() ?? string.Empty,
                    EmpNumero = Convert.ToInt32(reader.GetValue(4)),
                    AmbNumero = Convert.ToInt32(reader.GetValue(5))
                });
            }

            return lista;
        }

        private static BitmapImage? BytesToBitmapImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            try
            {
                var image = new BitmapImage();
                using var ms = new MemoryStream(bytes);
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }
    }
}
