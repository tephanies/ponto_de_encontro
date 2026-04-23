

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

        public bool ExistePessoaNoPontoDeEncontro()
        {
            const string sql = @"
                    SELECT TOP 1 1
                    FROM Pessoas p
                    INNER JOIN Ambientes a ON p.ambNumero = a.ambNumero
                    WHERE a.ambPontoEncontro = 1 AND ISNULL(p.pesHabilitado,0) = 1";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            var result = cmd.ExecuteScalar();
            return result != null;
        }

        public List<Empresa> GetEmpresasVinculadasPontoDeEncontro()
        {
            var lista = new List<Empresa>();
            const string sql = @"
                    SELECT DISTINCT e.empNumero, e.empDescricao
                    FROM Empresa e
                    INNER JOIN LinkEmpresaAmbientes lea ON e.empNumero = lea.empNumero
                    INNER JOIN Ambientes a ON lea.ambNumero = a.ambNumero
                    WHERE ISNULL(a.ambPontoEncontro,0) = 1
                    ORDER BY e.empDescricao";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Empresa
                {
                    EmpNumero = Convert.ToInt32(reader.GetValue(0)),
                    EmpDescricao = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
                });
            }

            return lista;
        }

        public List<Ambiente> GetPontosDeEncontroPorEmpresa(int empNumero)
        {
            var lista = new List<Ambiente>();
            const string sql = @"
                    SELECT a.ambNumero, a.ambPontoEncontro, a.ambExterno, 
                    ISNULL(a.ambDescricao,'') 
                    FROM Ambientes a
                    INNER JOIN LinkEmpresaAmbientes lea ON 
                    a.ambNumero = lea.ambNumero
                    WHERE lea.empNumero = @empNumero 
                    AND ISNULL(a.ambPontoEncontro,0) = 1
                    ORDER BY a.ambNumero";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@empNumero", empNumero);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Ambiente
                {
                    AmbNumero = Convert.ToInt32(reader.GetValue(0)),
                    AmbPontoEncontro = Convert.ToInt32(reader.GetValue(1)) == 1,
                    AmbExterno = Convert.ToInt32(reader.GetValue(2)) == 1,
                    AmbDescricao = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
                });
            }

            return lista;
        }

        public List<Pessoa> GetPessoasNoPontoDeEncontro(int empNumero, int ambPontoEncontroNumero)
        {
            const string sql = @"
                    SELECT p.Pin, p.pesNome, ISNULL(p.pesRamalCom,''), ISNULL(p.pesCelular,''), p.empNumero, p.ambNumero
                    FROM Pessoas p
                    INNER JOIN LinkEmpresaAmbientes lea ON lea.empNumero = @empNumero AND lea.ambNumero = p.ambNumero
                    WHERE ISNULL(p.pesHabilitado,0) = 1
                      AND p.ambNumero = @ambPontoEncontro";

            return ExecutarConsultaPessoas(sql, new[]
            {
                    new SqlParameter("@empNumero", empNumero),
                    new SqlParameter("@ambPontoEncontro", ambPontoEncontroNumero)
                });
        }

        public List<Pessoa> GetPessoasAreaInterna(int empNumero)
        {
            const string sql = @"
                    SELECT p.Pin, p.pesNome, ISNULL(p.pesRamalCom,''), ISNULL(p.pesCelular,''), p.empNumero, p.ambNumero
                    FROM Pessoas p
                    INNER JOIN LinkEmpresaAmbientes lea ON lea.empNumero = @empNumero AND lea.ambNumero = p.ambNumero
                    INNER JOIN Ambientes a ON p.ambNumero = a.ambNumero
                    WHERE ISNULL(p.pesHabilitado,0) = 1
                      AND ISNULL(a.ambExterno,0) = 0
                      AND ISNULL(a.ambPontoEncontro,0) = 0";

            return ExecutarConsultaPessoas(sql, new[]
            {
                    new SqlParameter("@empNumero", empNumero)
                });
        }

        public System.Windows.Media.Imaging.BitmapImage? GetFotoPessoa(string pin)
        {
            const string sql = "SELECT fotFoto FROM Fotos WHERE PIN = @pin";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pin", pin);
            var result = cmd.ExecuteScalar();

            if (result == null || result == DBNull.Value) return null;
            var bytes = (byte[])result;
            return BytesToBitmapImage(bytes);
        }

        // Event methods
        public List<PontoEncontroEvent> GetPendingPontoEvents()
        {
            var lista = new List<PontoEncontroEvent>();
            const string sql = @"
                    SELECT Id, PIN, AmbNumero, CreatedAt, Processed, ProcessedAt
                    FROM dbo.PontoEncontroEvents
                    WHERE Processed = 0
                    ORDER BY CreatedAt ASC";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new PontoEncontroEvent
                {
                    Id = reader.GetInt32(0),
                    Pin = reader.IsDBNull(1) ? null : reader.GetString(1),
                    AmbNumero = Convert.ToInt32(reader.GetValue(2)),
                    CreatedAt = reader.GetDateTime(3),
                    Processed = reader.GetBoolean(4),
                    ProcessedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5)
                });
            }

            return lista;
        }

        public void MarkEventsProcessed(IEnumerable<int> ids)
        {
            var idArray = ids?.ToArray() ?? Array.Empty<int>();
            if (idArray.Length == 0) return;

            var parameters = new List<SqlParameter>();
            var inClause = new System.Text.StringBuilder();
            for (int i = 0; i < idArray.Length; i++)
            {
                var name = "@id" + i;
                if (i > 0) inClause.Append(", ");
                inClause.Append(name);
                parameters.Add(new SqlParameter(name, idArray[i]));
            }

            var sql = $"UPDATE dbo.PontoEncontroEvents SET Processed = 1, ProcessedAt = SYSUTCDATETIME() WHERE Id IN ({inClause})";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());
            cmd.ExecuteNonQuery();
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
                    Pin = reader.GetValue(0)?.ToString() ?? string.Empty,
                    PesNome = reader.GetValue(1)?.ToString() ?? string.Empty,
                    PesRamalCom = reader.GetValue(2)?.ToString() ?? string.Empty,
                    PesCelular = reader.GetValue(3)?.ToString() ?? string.Empty,
                    EmpNumero = Convert.ToInt32(reader.GetValue(4)),
                    AmbNumero = Convert.ToInt32(reader.GetValue(5))
                });
            }

            return lista;
        }

        private static BitmapImage? BytesToBitmapImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
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
