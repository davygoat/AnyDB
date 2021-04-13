using AnyDB;
using System.Data;
using System.Data.Common;

namespace SPlite
{
    internal static class SPliteProcs
    {
        internal static void DoTransaction(string fnam, TransactDelegate doThis)
        {
            var cs = $"Data Source={fnam}; FailIfMissing=true;";
            Database.BeginTransaction(Providers.SQLite, cs, doThis);
        }

        internal static void CheckSyntax(Transaction tx, string name, string sql)
        {
            var test = new AnyDB.Drivers.SQLite.SPliteCommand.TrialRun(name, sql);
            using (DbCommand command = tx.Database.Driver.CreateCommand())
            {
                command.Connection = tx.TheConnection;
                AnyDB.Drivers.SQLite.SPliteCommand.CheckSyntax(test, command);
            }
        }

        private static void TryExecute(Transaction tx, string sql, string ignore)
        {
            try
            {
                tx.Database.ExecuteNonQuery(sql);
            }
            catch(AnyDbException ex)
            {
                if (!ex.Message.Contains(ignore)) throw;
            }
        }

        #region Messy code (outdented to improve SQLite .schema command readability)

        internal static string DummyCreateProcedure()
        {
            return @"
CREATE PROCEDURE name (:one INTEGER,
                       :two VARCHAR(80)) AS
BEGIN
   SELECT ;
END";
        }

        internal static void PossiblyCreateTable(Transaction tx)
        {
            // CREATE TABLE
            TryExecute(tx,
                       @"
CREATE TABLE splite_procs
(
   name TEXT NOT NULL PRIMARY KEY,
   sql  TEXT NOT NULL
)",
                       "table splite_procs already exists");
        }

        internal static void PossiblyCreateTriggers(Transaction tx)
        {
            // Prevent INSERT
            TryExecute(tx,
                       @"
CREATE TRIGGER splite_procs_noinsert
BEFORE INSERT ON splite_procs
BEGIN
   SELECT RAISE (FAIL, 'table splite_procs may not be modified');
END",
                       "trigger splite_procs_noinsert already exists");

            // Prevent UPDATE
            TryExecute(tx,
                       @"
CREATE TRIGGER splite_procs_noupdate
BEFORE UPDATE ON splite_procs
BEGIN
   SELECT RAISE (FAIL, 'table splite_procs may not be modified');
END",
                       "trigger splite_procs_noupdate already exists");

            // Prevent DELETE
            TryExecute(tx,
                       @"
CREATE TRIGGER splite_procs_nodelete
BEFORE DELETE ON splite_procs
BEGIN
   SELECT RAISE (FAIL, 'table splite_procs may not be modified');
END",
                       "trigger splite_procs_nodelete already exists");
        }

        #endregion

        internal static void PossiblyDropTriggers(Transaction tx)
        {
            TryExecute(tx, "DROP TRIGGER splite_procs_noinsert", "no such trigger");
            TryExecute(tx, "DROP TRIGGER splite_procs_noupdate", "no such trigger");
            TryExecute(tx, "DROP TRIGGER splite_procs_nodelete", "no such trigger");
        }

        internal static DataTable ReadAllProcedures(Transaction tx)
        {
            DataTable dt = tx.Database.GetDataTable(@"SELECT   name
                                                      FROM     splite_procs
                                                      ORDER BY name");
            return dt;
        }

        internal static string GetProcedureText(Transaction tx, string name)
        {
            return tx.Database.GetScalar<string>(@"SELECT sql
                                                   FROM   splite_procs
                                                   WHERE  name = :prcnam",
                                                   name);
        }

        internal static void CreateOrReplaceProcedure(Transaction tx, string name, string sql, string oldname)
        {
            sql = "\r\n" + sql.Trim();
            if (oldname != null && ReplaceProcedure(tx, name, sql, oldname) == 1) return;
            else CreateProcedure(tx, name, sql);
        }

        private static void CreateProcedure(Transaction tx, string name, string sql)
        {
            tx.Database.ExecuteNonQuery(@"INSERT INTO splite_procs
                                          VALUES (:name, :sql)",
                                          name,
                                          sql);
        }

        private static int ReplaceProcedure(Transaction tx, string name, string sql, string oldname)
        {
            return tx.Database.ExecuteNonQuery(@"UPDATE splite_procs
                                                 SET    name = :newnam,
                                                        sql  = :sql
                                                 WHERE  name = :oldnam",
                                                 name,
                                                 sql,
                                                 oldname);
        }

        internal static void DropProcedure(Transaction tx, string name)
        {
            tx.Database.ExecuteNonQuery(@"DELETE
                                          FROM   splite_procs
                                          WHERE  name = :name",
                                          name);
        }
    }
}