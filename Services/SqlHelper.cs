using Microsoft.Data.Sql;
using Microsoft.Data.SqlClient;
using SQLUserForge.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.ServiceProcess;

namespace SQLUserForge.Services
{
    public static class SqlHelper
    {
        // Énumération des instances via SQL Browser — peut échouer si le service Browser est désactivé.
        // On propose toujours la saisie manuelle si nécessaire.
        public static IEnumerable<string> EnumerateLocalInstancesReal()
        {
            var set = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                DataTable dt = SqlDataSourceEnumerator.Instance.GetDataSources();
                foreach (DataRow row in dt.Rows)
                {
                    string server = row["ServerName"]?.ToString() ?? "";
                    string? inst = row["InstanceName"] as string;

                    if (string.IsNullOrWhiteSpace(server)) continue;

                    // default instance -> juste le nom machine
                    string full = string.IsNullOrWhiteSpace(inst) ? server : $"{server}\\{inst}";
                    if (server.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                        set.Add(full);
                }
            }
            catch
            {
                // silencieux : on combinera avec le fallback services
            }

            // Fallback B : services (ci-dessous) fusionné
            foreach (var s in EnumerateInstancesFromServices())
                set.Add(s);

            // Option: si rien trouvé, garde au moins la saisie manuelle
            return set;
        }

        private static IEnumerable<string> EnumerateInstancesFromServices()
        {
            var machine = Environment.MachineName;
            foreach (var svc in ServiceController.GetServices())
            {
                // MSSQLSERVER = default instance ⇒ se connecte via "MACHINE"
                if (svc.ServiceName.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase))
                {
                    yield return machine;
                }
                // MSSQL$NAME = instance nommée ⇒ "MACHINE\NAME"
                else if (svc.ServiceName.StartsWith("MSSQL$", StringComparison.OrdinalIgnoreCase))
                {
                    var inst = svc.ServiceName.Substring("MSSQL$".Length);
                    if (!string.IsNullOrWhiteSpace(inst))
                        yield return $"{machine}\\{inst}";
                }
            }
        }

        public static IEnumerable<string> EnumerateLocalInstances()
        {
            return EnumerateLocalInstancesReal();
        }

        public static SqlConnection MakeAdminConnection(UserRequest req, string database = "master")
        {
            var cb = new SqlConnectionStringBuilder
            {
                DataSource = req.ServerInstance,
                InitialCatalog = database,
                TrustServerCertificate = true,
                ConnectTimeout = 15
            };

            if (req.UseIntegratedSecurity)
            {
                cb.IntegratedSecurity = true;
            }
            else
            {
                cb.UserID = req.AdminLogin;
                cb.Password = req.AdminPassword;
                cb.IntegratedSecurity = false;
            }

            return new SqlConnection(cb.ConnectionString);
        }

        public static List<string> GetDatabases(UserRequest req)
        {
            var list = new List<string>();
            using var cnn = MakeAdminConnection(req, "master");
            cnn.Open();
            using var cmd = new SqlCommand(
                "SELECT name FROM sys.databases WHERE state = 0 ORDER BY name;", cnn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(rd.GetString(0));
            }
            return list;
        }

        public static bool LoginExists(UserRequest req, string loginName)
        {
            using var cnn = MakeAdminConnection(req, "master");
            cnn.Open();
            using var cmd = new SqlCommand(
                "SELECT 1 FROM sys.server_principals WHERE name = @n;", cnn);
            cmd.Parameters.AddWithValue("@n", loginName);
            var o = cmd.ExecuteScalar();
            return o != null;
        }

        public static bool UserExistsInDb(UserRequest req, string database, string userName)
        {
            using var cnn = MakeAdminConnection(req, database);
            cnn.Open();
            using var cmd = new SqlCommand(
                "SELECT 1 FROM sys.database_principals WHERE name = @n;", cnn);
            cmd.Parameters.AddWithValue("@n", userName);
            var o = cmd.ExecuteScalar();
            return o != null;
        }

        public static void CreateLogin(UserRequest req)
        {
            using var cnn = MakeAdminConnection(req, "master");
            cnn.Open();

            // Si le login existe déjà, on ne recrée pas (idempotent)
            if (!LoginExists(req, req.NewLoginName))
            {
                using var create = new SqlCommand(@"
DECLARE @login sysname       = @pLogin;
DECLARE @pwd   nvarchar(128) = @pPassword;

DECLARE @sql nvarchar(max) =
    N'CREATE LOGIN ' + QUOTENAME(@login) +
    N' WITH PASSWORD = N''' +
    REPLACE(@pwd, N'''', N'''''') +
    N''', CHECK_POLICY = OFF, CHECK_EXPIRATION = OFF;';

EXEC (@sql);", cnn);

                create.Parameters.AddWithValue("@pLogin", req.NewLoginName);
                create.Parameters.Add("@pPassword", System.Data.SqlDbType.NVarChar, 128).Value = req.NewLoginPassword;
                create.ExecuteNonQuery();
            }

            // Base par défaut
            using (var alter = new SqlCommand(
                $"ALTER LOGIN [{req.NewLoginName}] WITH DEFAULT_DATABASE = [{req.TargetDatabase}];", cnn))
            {
                alter.ExecuteNonQuery();
            }

            // Rôles serveur
            foreach (var role in req.SelectedServerRoles)
            {
                using var roleCmd = new SqlCommand(
                    $"ALTER SERVER ROLE [{role}] ADD MEMBER [{req.NewLoginName}];", cnn);
                roleCmd.ExecuteNonQuery();
            }
        }

        public static void CreateDbUserAndRoles(UserRequest req)
        {
            using var cnn = MakeAdminConnection(req, req.TargetDatabase);
            cnn.Open();

            // Crée le user DB si besoin
            if (!UserExistsInDb(req, req.TargetDatabase, req.NewLoginName))
            {
                using var createUser = new SqlCommand(
                    $"CREATE USER [{req.NewLoginName}] FOR LOGIN [{req.NewLoginName}] WITH DEFAULT_SCHEMA = [dbo];",
                    cnn);
                createUser.ExecuteNonQuery();
            }

            // Assigner les rôles base
            foreach (var role in req.SelectedDbRoles)
            {
                using var roleCmd = new SqlCommand(
                    $"ALTER ROLE [{role}] ADD MEMBER [{req.NewLoginName}];",
                    cnn);
                roleCmd.ExecuteNonQuery();
            }
        }
    }
}
