//#define LOOPIT

using AnyDB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace TestProgram
{
    class Program
    {
        public class AnyDB_Test
        {
            public int ColumnOne { get; set; }
            public string ColumnTwo { get; set; }
            public DateTime ColumnThree { get; set; }
            public double ColumnFour { get; set; }
        }

        class Run
        {
            public Providers Provider;
            public string ConnectString;
            public Run(Providers Provider, string ConnectString)
            {
                this.Provider = Provider;
                this.ConnectString = ConnectString;
            }
        }

        static void Main(string[] args)
        {
            Database.Trace = true;
        
            Console.WindowHeight = 52;// 64;
            Console.BufferHeight = 5000;

            List<Run> list = new List<Run>()
            {
                //// Windows Authentication - inherits my admin rights, defeating access controls
                //new Run(Providers.SQLServer,  @"Data Source=.\SQLEXPRESS; Integrated Security=SSPI; Initial Catalog=AnyDB;"),
                //new Run(Providers.ODBC,       @"Driver=SQL Server Native Client; Server=.\SQLEXPRESS; Database=AnyDB; Trusted_Connection=yes;"), // fuzzy driver name

                //// OK to go
                new Run(Providers.Access,     @"Driver=Microsoft Access Driver; DBQ=..\..\Database Scripts\Access.mdb"),                                 // fuzzy driver name
                //new Run(Providers.CUBRID,     @"server=localhost;database=AnyDB;user=test;password=lemming"),
                //new Run(Providers.DB2,        @"Server=RDBMS:50000; Database=AnyDB; UID=test; PWD=lemming; Current Schema=david"),                   // fuzzy CurrentSchema
                //new Run(Providers.Excel,      @"Driver=Microsoft Excel Driver; DBQ=..\..\Database Scripts\Excel.xls; Readonly=0"),                       // fuzzy driver name
                //new Run(Providers.Firebird,   @"Database=/var/lib/firebird/2.5/data/AnyDB.fdb; Data Source=RDBMS; User ID=test; Password=lemming;"),
                //new Run(Providers.Informix,   @"Server=localhost:9089; Database=AnyDB; UID=test; PWD=lemming;"),
                //new Run(Providers.MonetDB,    @"Driver=MonetDB ODBC Driver; HOST=localhost; PORT=50005; Database=AnyDB; UID=test; PWD=lemming"),
                //new Run(Providers.MySQL,      @"Server=RDBMS; Database=AnyDB; UID=test; PWD=lemming"),
                //new Run(Providers.Oracle,     @"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=xe))); User Id=test; Password=lemming; CurrentSchema=david;"),                                // fake CurrentSchema option
                //new Run(Providers.PostgreSQL, @"Server=RDBMS; Database=anydb; UID=test; PWD=lemming"),
                //new Run(Providers.SQLite,     @"Data Source=..\..\Database Scripts\SQLite.db; Pooling=true; FailIfMissing=true"),
                //new Run(Providers.SQLServer,  @"Server=localhost\SQLEXPRESS; UID=test; Password=lemming; Database=AnyDB;"),
                //new Run(Providers.ODBC,       @"Driver=SQL Server Native Client; Server=.\SQLEXPRESS; Database=AnyDB; UID=test; PWD=lemming;"),         // fuzzy driver name
                //new Run(Providers.OLEDB,      @"Provider=Microsoft.ACE.OLEDB.12.0; Data Source=..\..\Database Scripts\; Extended Properties='Text;HDR=YES;FMT=Delimited'"),
                //new Run(Providers.Text,       @"Driver=Microsoft Text Driver; DBQ=..\..\Database Scripts\"),                                             // fuzzy driver name
            };

#if LOOPIT
            int loopcount = 0;
            again:
#endif

            foreach (var run in list)
            {
                Debug.WriteLineIf(Database.Trace, "\n===\nProvider ================== : " + run.Provider + " " + new string('=', 60 - run.Provider.ToString().Length - 12) + "\n===");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Provider ====================== : {0} {1}", run.Provider, new string('=', 60 - run.Provider.ToString().Length - 15));
                try
                {
                    using (Database db = new Database(run.Provider, run.ConnectString))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine("Invariant                   : {0}", db.ProviderInvariantName);
                        Console.WriteLine("Product name                : {0}", db.ProductName);
                        Console.WriteLine("Product version             : {0}", db.ProductVersion);
                        Console.WriteLine("Driver class                : {0}", db.Driver.Name);
                        Console.WriteLine("Factory class               : {0}", db.Factory.GetType());
                        Console.WriteLine("Readonly                    : {0}", db.Driver.Readonly);
                        Console.WriteLine("Rewrite flags               : {0}", db.RewriteFlags);
                        Console.WriteLine("Allow semicolon             : {0}", db.Driver.AllowSemicolon);
                        Console.WriteLine("Has INSERT                  : {0}", db.Driver.HasInsert);
                        Console.WriteLine("Has UPDATE                  : {0}", db.Driver.HasUpdate);
                        Console.WriteLine("Has DELETE                  : {0}", db.Driver.HasDelete);
                        Console.WriteLine("Has PRIMARY KEY exception   : {0}", db.Driver.HasPrimaryKeyException);
                        Console.WriteLine("Has FOREIGN KEY exception   : {0}", db.Driver.HasForeignKeyException);
                        Console.WriteLine("Has UNIQUE exception        : {0}", db.Driver.HasUniqueException);
                        Console.WriteLine("Has CHECK exception         : {0}", db.Driver.HasCheckException);
                        Console.WriteLine("Has invalid table exception : {0}", db.Driver.HasInvalidTableException);
                        Console.WriteLine("Has permission exception    : {0}", db.Driver.HasPermissionDeniedException);
                        Console.WriteLine("Has access controls         : {0}", db.Driver.HasAccessControl);
                        Console.WriteLine("Has transactions            : {0}", db.Driver.HasTransactions);
                        Console.WriteLine("Has stored procedures       : {0}", db.Driver.HasStoredProcedures);
                        Console.WriteLine("Has multiple cursors        : {0}", db.Driver.HasMultipleCursors);
                        Console.WriteLine("Has output parameters       : {0}", db.Driver.HasOutputParameters);
                        Console.WriteLine("Distinct PRIMARY/UNIQUE     : {0}", db.Driver.HasUniqueException && db.Driver.HasPrimaryKeyException);
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Gray;

                        string sql1 = @"
                                       SELECT *
                                       FROM   AnyDB_test";
                        string sql2 = @"
                                       SELECT column_one
                                       FROM   AnyDB_test
                                       WHERE  column_one = :abc";
                        string sql3 = @"
                                       SELECT column_two
                                       FROM   AnyDB_test
                                       WHERE  column_one = ?";
                        string sql4 = @"
                                       SELECT column_three
                                       FROM   AnyDB_test
                                       WHERE  column_one = {0}";

                        string sql5 = @"
                                       SELECT *
                                       FROM   AnyDB_test
                                       WHERE  column_one <= {0}
                                       AND    column_four <= {0}";

                        string sql6 = @"
                                       SELECT *
                                       FROM   AnyDB_test
                                       WHERE  column_one <= :one
                                       AND    column_four <= :one";

                        string update = "UPDATE AnyDB_test SET column_two = 'OneOneOne' WHERE column_one = 1";

                        Console.WriteLine("ExecuteNonQuery()");
                        Console.WriteLine();
                        if (db.Driver.HasDelete)
                        {
                            Console.WriteLine("DELETE " + db.ExecuteNonQuery("DELETE FROM AnyDB_test WHERE column_one IN (1, 2);") + " rows");
                        }
                        else
                        {
                            Console.WriteLine("DELETE -- not supported");
                        }
                        int n = db.GetScalar<int>("SELECT count(*) FROM AnyDB_test;");
                        if (n < 5)
                        {
                            if (db.Driver.HasInsert)
                            {
                                Console.WriteLine("INSERT " + db.ExecuteNonQuery("INSERT INTO AnyDB_test VALUES (1, 'One', :dt, 1.1)", DateTime.Now) + " row");
                                Console.WriteLine("INSERT " + db.ExecuteNonQuery("INSERT INTO AnyDB_test VALUES (2, 'Two', :dt, 2.2)", DateTime.Now) + " row");
                            }
                            else
                            {
                                Console.WriteLine("INSERT -- not supported");
                            }
                            Console.WriteLine();
                        }
                        else if (!db.Driver.HasDelete)
                        {
                            Console.WriteLine("INSERT -- enough! can't clean up after ourselves.");
                        }

                        Console.WriteLine("GetSingleton()");
                        Console.WriteLine();
                        try
                        {
                            PrintDataRow(db.GetSingleton("SELECT * FROM AnyDB_test WHERE column_one = :one", 1));
                            Console.WriteLine();
                        }
                        catch (AnyDbException ex)
                        {
                            PrintError(ex);
                        }

                        Console.WriteLine("GetDataTable()");
                        Console.WriteLine();
                        PrintDataTable(db.GetDataTable(sql1));
                        Console.WriteLine();

                        Console.WriteLine("Repeatable parameter markers -- .NET style");
                        Console.WriteLine();
                        PrintDataTable(db.GetDataTable(sql5, 1.1));
                        Console.WriteLine();

                        Console.WriteLine("Repeatable parameter markers -- :/@ style");
                        Console.WriteLine();
                        PrintDataTable(db.GetDataTable(sql6, 1.1));
                        Console.WriteLine();

                        if (db.Driver.AllowSemicolon)
                        {
                            Console.WriteLine("GetDataSet() -- using semicolons");
                            Console.WriteLine();
                            PrintDataSet(db.GetDataSet(@" 
                                                          SELECT   *
                                                          FROM     AnyDB_test
                                                          ORDER BY column_one ASC;

                                                          SELECT   *
                                                          FROM     AnyDB_test
                                                          ORDER BY column_one DESC;"));
                            Console.WriteLine();
                        }
                        else
                            Console.WriteLine("GetDataSet() -- semicolons not allowed\n");

                        Console.WriteLine("GetSingleton<T>()");
                        Console.WriteLine();
                        try
                        {
                            PrintObject(db.GetSingleton<AnyDB_Test>("SELECT * FROM AnyDB_test WHERE column_one = :one", 1));
                        }
                        catch(AnyDbException ex)
                        {
                            PrintError(ex);
                        }
                        Console.WriteLine();

                        Console.WriteLine("GetList<T>()");
                        Console.WriteLine();
                        PrintList(db.GetList<AnyDB_Test>(sql1));
                        Console.WriteLine();

                        Console.WriteLine("GetDataReader()");
                        Console.WriteLine();
                        using (var dr = db.GetDataReader(sql1))
                        {
                            PrintDataReader(dr);
                        }
                        Console.WriteLine();

                        Console.WriteLine("GetInteger()  : " + db.GetScalar<int>(sql2, 1));
                        Console.WriteLine("GetString()   : " + db.GetScalar<string>(sql3, 1));
                        Console.WriteLine("GetDateTime() : " + db.GetScalar<DateTime?>(sql4, 1, 2));
                        Console.WriteLine();

                        Console.WriteLine("Query Rewriter");
                        Console.WriteLine();
                        PrintDataTable(db.GetDataTable(@"
                                                         SELECT     *
                                                         FROM       AnyDB_test
                                                         WHERE      column_three >= CURRENT_TIMESTAMP - INTERVAL '1' MINUTE
                                                         ORDER BY   column_one
                                                         LIMIT TO 1 ROW"));
                        PrintDataTable(db.GetDataTable(@"
                                                         SELECT   *
                                                         FROM     AnyDB_test
                                                         WHERE    column_three >= CURRENT_TIMESTAMP - INTERVAL '1 MINUTE'
                                                         ORDER BY column_one
                                                         LIMIT 1"));
                        PrintDataTable(db.GetDataTable(@"
                                                         SELECT        *
                                                         FROM          AnyDB_test
                                                         WHERE         column_three >= CURRENT_TIMESTAMP - 1 MINUTE
                                                         ORDER BY      column_one
                                                         FETCH FIRST 1 ROW ONLY"));
                        PrintDataTable(db.GetDataTable(@"
                                                         SELECT 
                                                         TOP    1 *
                                                         FROM     AnyDB_test
                                                         WHERE    column_three >= DATEADD(MINUTE,-1,CURRENT_TIMESTAMP)
                                                         ORDER BY column_one"));
                        PrintDataTable(db.GetDataTable(@"
                                                         SELECT 
                                                         FIRST  1 *
                                                         FROM     AnyDB_test
                                                         WHERE    column_three >= DATEADD(MINUTE,-1,CURRENT_TIMESTAMP)
                                                         ORDER BY column_one"));
                        Console.WriteLine();

                        Console.WriteLine("JOIN");
                        Console.WriteLine();
                        PrintDataTable(db.GetDataTable(@"
                                                        SELECT   * 
                                                        FROM     AnyDB_test t1
                                                        JOIN     AnyDB_test t2 ON t1.column_one = t2.column_one
                                                        ORDER BY 1, 2, 3"));
                        Console.WriteLine();

                        if (!db.Driver.HasStoredProcedures)
                        {
                            Console.WriteLine("Stored Procedures -- Not supported");
                        }
                        else
                        {
                            Console.WriteLine("ProcedureCall() --- Parameter names provided by .NET");
                            Console.WriteLine();
                            Console.WriteLine("1 column_one + 10.01 = {0} (return value)", db.ProcedureCall("AnyDB_testProcedure1", 1, 10.01));
                            Console.WriteLine("2 column_one + 20.02 = {0} (return value)", db.ProcedureCall("AnyDB_testProcedure1", 2, 20.02));
                            if (db.Driver.HasOutputParameters)
                            {
                                IDbDataParameter outStr = db.Out(DbType.String, 80);
                                IDbDataParameter outFloat = db.Out(DbType.Single);
                                db.ProcedureCall("AnyDB_testProcedure2", 1, outStr, outFloat);
                                Console.WriteLine("1 column_two = \"{0}\" column_four = {1} (output parameters)", outStr.Value, outFloat.Value);
                            }
                            else
                                Console.WriteLine("Output parameters not supported by {0}", run.Provider);
                            Console.WriteLine();
                            using (var tx = db.BeginTransaction())
                            {
                                Console.WriteLine("ProcedureDataTable()");
                                Console.WriteLine();
                                PrintDataTable(db.ProcedureDataTable("AnyDB_testProcedure3"));
                                Console.WriteLine();
                                Console.WriteLine("ProcedureList<T>()");
                                Console.WriteLine();
                                PrintList(tx.Database.ProcedureList<AnyDB_Test>("AnyDB_testProcedure3"));
                                Console.WriteLine();
                            }
                            Console.WriteLine("ProcedureDataSet()" + (!db.Driver.HasMultipleCursors ? " -- only one result set" : ""));
                            Console.WriteLine();
                            PrintDataSet(db.ProcedureDataSet("AnyDB_testProcedure4"));
                            Console.WriteLine();
                            Console.WriteLine("ProcedureDataReader()" + (!db.Driver.HasMultipleCursors ? " -- only one result set" : ""));
                            Console.WriteLine();
                            using (var dr = db.ProcedureDataReader("AnyDB_testProcedure4"))
                            {
                                PrintDataReader(dr);
                            }
                        }

                        Console.WriteLine();

                        if (!db.Driver.HasTransactions)
                        {
                            Console.WriteLine("Transactions -- Not supported");
                            Console.WriteLine();
                        }
                        else
                        {
                            using (var tx = db.BeginTransaction())
                            {
                                Console.Write("UPDATE   => ");
                                db.ExecuteNonQuery(update);
                                Console.WriteLine("{0}", db.GetScalar(sql3, 1));
                                Console.Write("ROLLBACK => ");
                                tx.Rollback();
                            }
                            Console.WriteLine(db.GetScalar<string>(sql3, 1));
                            Console.WriteLine();
                            using (var tx = db.BeginTransaction())
                            {
                                Console.Write("UPDATE   => ");
                                db.ExecuteNonQuery(update);
                                Console.WriteLine("{0}", db.GetScalar<string>(sql3, 1));
                                Console.Write("COMMIT   => ");
                                tx.Commit();
                            }
                            Console.WriteLine(db.GetScalar<string>(sql3, 1));
                            Console.WriteLine();
                        }

                        Console.WriteLine("Errors");
                        Console.WriteLine();
                        CheckErrors(db);

                        if (db.Driver.Readonly)
                        {
                            Console.WriteLine("Database is readonly");
                        }
                        else if (!db.Driver.HasDelete)
                        {
                            Console.WriteLine("Delete -- not supported");
                        }
                        else
                        {
                            Console.WriteLine("DELETE " + db.ExecuteNonQuery("DELETE FROM AnyDB_test WHERE column_one IN (1, 2)") + " rows");
                            PrintDataTable(db.GetDataTable(sql1));
                        }
                        Console.WriteLine();
                    }
#if LOOPIT
                    Console.WriteLine("loop={0} handles={1}", ++loopcount, Process.GetCurrentProcess().HandleCount);
                    Console.Title = string.Format("handles={0}", Process.GetCurrentProcess().HandleCount);
#endif
                }
                catch (Wobbler ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("**** Thrown a wobbler...");
                    Console.WriteLine("**** {0}: {1}", ex.GetType(), ex.Message);
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                }
                catch (ConstructorException ex)
                {
                    PrintError(ex);
                }
                catch (Exception ex)
                {
                    AnyDbException ax = ex as AnyDbException;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("*******************************************************************************");
                    Console.WriteLine("* {0}", run.ConnectString);
                    while (ex != null)
                    {
                        Console.WriteLine("* {0}", ex.GetType());
                        Console.WriteLine("* {0}", ex.Message);
                        ex = ex.InnerException;
                    }
                    if (ax != null && ax.SQL != null)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine("{0}", ax.SQL);
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    Console.WriteLine("*******************************************************************************");
                    Console.WriteLine();
#if LOOPIT
                    Console.WriteLine("loop = {0}", loopcount);
#endif
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                }
            }

#if LOOPIT
            goto again;
#endif
            Console.Write("Press Enter to Exit, or Return to Depart - how oxymoronic: ");
            Console.ReadLine();
        }

        static void WriteWhite(string str, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(str, args);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        static void WriteReverse(string str, params object[] args)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(str, args);
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        static void CheckErrors(Database db)
        {
            /*
             * Check if the database has access controls.
             */

            try
            {
                Console.Write("NOACCESS ");
                if (db.Driver.HasAccessControl)
                    PrintDataTable(db.GetDataTable("SELECT * FROM AnyDB_numbers"));
                throw new NotImplementedException("no access controls, or access not restricted");
            }
            catch (NotImplementedException ex)
            {
                if (db.Driver.HasAccessControl) WriteReverse(" *** {0} *** ", ex.Message);
                else WriteWhite(ex.Message);
                Console.WriteLine();
            }
            catch (PermissionDeniedException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }
            catch (TableNotFoundException ex)
            {
                if (!db.Driver.HasPermissionDeniedException) PrintError(ex, ConsoleColor.White, "(In lieu of PermissionDeniedException.)");
                else throw;
            }

            /*
             * Try and violate the PRIMARY KEY.
             */

            try
            {
                Console.Write("INSERT(PKEY) ");
                if (db.Driver.HasUniqueException)
                {
                    db.ExecuteNonQuery("INSERT INTO AnyDB_test VALUES (1,'One',current_timestamp,1.1);");
                    db.ExecuteNonQuery("INSERT INTO AnyDB_test VALUES (1,'One',current_timestamp,1.1);");
                }
                throw new NotImplementedException("PRIMARY KEY not enforced");
            }
            catch (NotImplementedException ex)
            {
                if (db.Driver.HasUniqueException) WriteReverse(" *** {0} *** ", ex.Message);
                else WriteWhite(ex.Message);
                Console.WriteLine();
            }
            catch (FailedPrimaryKeyException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }
            catch (FailedUniqueConstraintException ex)
            {
                PrintError(ex, ConsoleColor.White, "(Does not distinguish PRIMARY/UNIQUE.)");
            }

            /*
             * Try and violate a FOREIGN KEY constraint.
             */

            try
            {
                Console.Write("UPDATE(FKEY) ");
                if (db.Driver.HasForeignKeyException)
                    db.ExecuteNonQuery("UPDATE AnyDB_test SET column_two = 'oink' WHERE column_one = '1';");
                throw new NotImplementedException("FOREIGN KEY not enforced");
            }
            catch (NotImplementedException ex)
            {
                if (db.Driver.HasForeignKeyException) WriteReverse(" *** {0} *** ", ex.Message);
                else WriteWhite(ex.Message);
                Console.WriteLine();
            }
            catch (FailedForeignKeyException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Access Driver")) WriteWhite("FOREIGN KEY throws a data type mismatch\n");
                else throw;
            }

            /*
             * Try and violate a UNIQUE constraint or index. Microsoft Access
             * and Excel throw a non specific data type mismatch error that cannot
             * reliably be distinguished from, say, trying to insert a string
             * into a numeric field.
             */

            try
            {
                Console.Write("UPDATE(UNIQ) ");
                if (db.Driver.HasUniqueException)
                {
                    db.ExecuteNonQuery("UPDATE AnyDB_test SET column_two = 'Two' WHERE column_one = '1';");
                    PrintDataTable(db.GetDataTable("SELECT * FROM AnyDB_test WHERE column_one IN (1,2)"));
                }
                throw new NotImplementedException("UNIQUE constraint not enforced");
            }
            catch (NotImplementedException ex)
            {
                if (db.Driver.HasUniqueException) WriteReverse(" *** {0} *** ", ex.Message);
                else WriteWhite(ex.Message);
                Console.WriteLine();
            }
            catch (FailedUniqueConstraintException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Access Driver")) WriteWhite("UNIQUE constraint throws a data type mismatch\n");
                else throw;
            }

            /*
             * Try and violate a NOT NULL constraint. Again, Microsoft Access and
             * Excel throw a data type mismatch that cannot easily be attributed
             * to a definite cause.
             */

            try
            {
                Console.Write("UPDATE(NULL) ");
                if (db.Driver.HasNotNullException)
                {
                    db.ExecuteNonQuery("UPDATE AnyDB_test SET column_three = NULL WHERE column_one = '1';");
                    PrintDataTable(db.GetDataTable("SELECT * FROM AnyDB_test WHERE column_one = 1"));
                }
                throw new NotImplementedException("NOT NULL constraint not enforced");
            }
            catch (NotImplementedException ex)
            {
                if (db.Driver.HasNotNullException) WriteReverse(" *** {0} *** ", ex.Message);
                else WriteWhite(ex.Message);
                Console.WriteLine();
            }
            catch (FailedNotNullException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }

            /*
             * Try and violate a CHECK constraint. MonetDB hasn't got them. MySQL
             * lets you create them, but then ignores them anyway.
             */

            try
            {
                Console.Write("UPDATE(CHECK) ");
                if (db.Driver.HasCheckException)
                {
                    db.ExecuteNonQuery("UPDATE AnyDB_test SET column_four = -32768 WHERE column_one = 1;");
                    PrintDataTable(db.GetDataTable("SELECT * FROM AnyDB_test WHERE column_one = 1"));
                }
                throw new NotImplementedException("CHECK constraint not enforced");
            }
            catch (NotImplementedException ex)
            {
                if (db.Driver.HasCheckException) WriteReverse(" *** {0} *** ", ex.Message);
                else WriteWhite(ex.Message);
                Console.WriteLine();
            }
            catch (FailedCheckConstraintException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }

            /*
             * Scalar select, no data.
             */

            try
            {
                Console.Write("SCALAR(NODATA) ");
                db.GetScalar<int>("SELECT column_one FROM AnyDB_test WHERE column_one = :one", -999);
            }
            catch (NoDataException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }

            /*
             * Singleton select, no data.
             */

            try
            {
                Console.Write("SINGLETON(NODATA) ");
                PrintDataRow(db.GetSingleton("SELECT * FROM AnyDB_test WHERE column_one = :one", -999));
            }
            catch (NoDataException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }
            catch (AnyDbException ex)
            {
                PrintError(ex);
            }

            /*
             * Singleton select, too much data.
             */

            try
            {
                Console.Write("SINGLETON(MULTIPLE) ");
                PrintDataRow(db.GetSingleton("SELECT * FROM AnyDB_test WHERE column_one IN (1,2)"));
            }
            catch (MultipleDataException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }
            catch (AnyDbException ex)
            {
                PrintError(ex);
            }

            /*
             * Invalid column, query.
             */

            try
            {
                Console.Write("SELECT(IVCOL) ");
                db.GetScalar("SELECT oink FROM AnyDB_test;");
            }
            catch (ColumnNotFoundException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }

            /*
             * Invalid column, query.
             */

            try
            {
                Console.Write("SELECT(IVCOL) ");
                db.GetScalar("SELECT oink FROM AnyDB_test;");
            }
            catch (ColumnNotFoundException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }

            /*
             * Check for a non-existent table. Some databases just fob you off with
             * an opaque "not allowed".
             */

            try
            {
                Console.Write("SELECT(IVTAB) ");
                db.GetScalar("SELECT * FROM oink;");
            }
            catch (TableNotFoundException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }
            catch (PermissionDeniedException ex)
            {
                if (!db.Driver.HasInvalidTableException)
                {
                    WriteWhite("{0}, {1}", ex.GetType().Name, ex.Message);
                    Console.WriteLine();
                }
                else
                    throw;
            }

            /*
             * Invalid table, INSERT.
             */

            try
            {
                if (db.Driver.HasInsert)
                {
                    Console.Write("INSERT(IVTAB) ");
                    db.ExecuteNonQuery("INSERT INTO oink VALUES (1);");
                }
            }
            catch (TableNotFoundException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }
            catch (PermissionDeniedException ex)
            {
                if (!db.Driver.HasInvalidTableException)
                {
                    WriteWhite("{0}, {1}", ex.GetType().Name, ex.Message);
                    Console.WriteLine();
                }
                else
                    throw;
            }

            /*
             * Invalid table, scalar.
             */

            try
            {
                Console.Write("SCALAR(IVTAB) ");
                db.GetScalar("SELECT * FROM oink");
            }
            catch (TableNotFoundException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }
            catch (PermissionDeniedException ex)
            {
                if (!db.Driver.HasInvalidTableException)
                {
                    WriteWhite("{0}, {1}", ex.GetType().Name, ex.Message);
                    Console.WriteLine();
                }
                else
                    throw;
            }

            /*
             * Invalid table, DataTable.
             */

            try
            {
                Console.Write("DATATABLE(IVTAB) ");
                db.GetDataTable("SELECT * FROM oink");
            }
            catch (TableNotFoundException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }
            catch (PermissionDeniedException ex)
            {
                if (!db.Driver.HasInvalidTableException)
                {
                    WriteWhite("{0}, {1}", ex.GetType().Name, ex.Message);
                    Console.WriteLine();
                }
                else
                    throw;
            }

            /*
             * Invalid table, DataReader.
             */

            try
            {
                Console.Write("READER(IVTAB) ");
                using (var reader=db.GetDataReader("SELECT * FROM oink"))
                {
                }
            }
            catch (TableNotFoundException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }
            catch (PermissionDeniedException ex)
            {
                if (!db.Driver.HasInvalidTableException)
                {
                    WriteWhite("{0}, {1}", ex.GetType().Name, ex.Message);
                    Console.WriteLine();
                }
                else
                    throw;
            }

            /*
             * Invalid procedure. Database that don't tell you about nonexistent
             * tables do, may, tell you about nonexistent procedures. Why the
             * inconsistency? (MySQL Connector/NET used to do this. Now (6.10.6)
             * it seems to be moving towards permission denied (it still says
             * invalid table on the first attempt, but permission denied after
             * that).
             */

            try
            {
                Console.Write("CALL(IVPROC) ");
                db.ProcedureCall("oink");
            }
            catch (ProcedureNotFoundException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }
            catch (PermissionDeniedException ex)
            {
                if (!db.Driver.HasInvalidTableException)
                {
                    WriteWhite("{0}, {1}", ex.GetType().Name, ex.Message);
                    Console.WriteLine();
                }
                else
                    throw;
            }
            catch (NotImplementedException)
            {
                Console.WriteLine("Stored procedures not supported.");
            }

            /*
             * Invalid procedure.
             */

            try
            {
                Console.Write("PROCTABLE(IVPROC) ");
                db.ProcedureDataTable("oink");
            }
            catch (ProcedureNotFoundException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }
            catch (PermissionDeniedException ex)
            {
                if (!db.Driver.HasInvalidTableException)
                {
                    WriteWhite("{0}, {1}", ex.GetType().Name, ex.Message);
                    Console.WriteLine();
                }
                else
                    throw;
            }
            catch (NotImplementedException)
            {
                Console.WriteLine("Stored procedures not supported.");
            }

            /*
             * Invalid procedure.
             */

            try
            {
                Console.Write("PROCSET(IVPROC) ");
                db.ProcedureDataSet("oink");
            }
            catch (ProcedureNotFoundException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }
            catch (PermissionDeniedException ex)
            {
                if (!db.Driver.HasInvalidTableException)
                {
                    WriteWhite("{0}, {1}", ex.GetType().Name, ex.Message);
                    Console.WriteLine();
                }
                else
                    throw;
            }
            catch (NotImplementedException)
            {
                Console.WriteLine("Stored procedures not supported.");
            }

            /*
             * Invalid procedure.
             */

            try
            {
                Console.Write("PROCREADER(IVPROC) ");
                using (var reader=db.ProcedureDataReader("oink"))
                {
                }
            }
            catch (ProcedureNotFoundException ex)
            {
                PrintError(ex, ConsoleColor.White);
            }
            catch (PermissionDeniedException ex)
            {
                if (!db.Driver.HasInvalidTableException)
                {
                    WriteWhite("{0}, {1}", ex.GetType().Name, ex.Message);
                    Console.WriteLine();
                }
                else
                    throw;
            }
            catch (NotImplementedException)
            {
                Console.WriteLine("Stored procedures not supported.");
            }
        }

        static void PrintObject(AnyDB_Test elem)
        {
            Console.WriteLine("{0}|{1}|{2}|{3}", 
                                elem.ColumnOne, 
                                elem.ColumnTwo, 
                                elem.ColumnThree.ToString("dd-MMM-yyyy HH:mm:ss.fffffff"), 
                                elem.ColumnFour);
        }

        static void PrintList(List<AnyDB_Test> list)
        {
            Console.WriteLine("ColumnOne|ColumnTwo|ColumnThree|ColumnFour");
            foreach (var elem in list) PrintObject(elem);
        }

        static void PrintDataRow(DataRow dr)
        {
            for (int i = 0; i < dr.Table.Columns.Count; i++)
            {
                if (i > 0) Console.Write("|");
                if (dr[i] != DBNull.Value && dr[i] is DateTime)
                    Console.Write("{0}", Convert.ToDateTime(dr[i]).ToString("dd-MMM-yyyy HH:mm:ss.fffffff"));
                else
                    Console.Write("{0}", dr[i]);
            }
            Console.WriteLine();
        }
        
        static void PrintDataTable(DataTable dt)
        {
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                if (i > 0) Console.Write("|");
                Console.Write(dt.Columns[i].ColumnName);
            }
            Console.WriteLine();
            foreach (DataRow dr in dt.Rows) PrintDataRow(dr);
        }

        static void PrintDataSet(DataSet ds)
        {
            foreach (DataTable dt in ds.Tables) PrintDataTable(dt);
        }

        static void PrintDataReader(DbDataReader dr)
        {
            do
            {
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    if (i > 0) Console.Write("|");
                    Console.Write("{0}", dr.GetName(i));
                }
                Console.WriteLine();
                while (dr.Read())
                {
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        if (i > 0) Console.Write("|");
                        if (dr[i] != DBNull.Value && dr[i] is DateTime)
                            Console.Write("{0}", Convert.ToDateTime(dr[i]).ToString("dd-MMM-yyyy HH:mm:ss.fffffff"));
                        else
                            Console.Write("{0}", dr[i]);
                    }
                    Console.WriteLine();
                }
            } while (dr.NextResult());
        }

        static void PrintError(AnyDbException exAnyDB, ConsoleColor colour=ConsoleColor.Red, string extra=null)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(exAnyDB.GetType().Name + " :- ");
            Console.ForegroundColor = colour;
            Console.WriteLine(exAnyDB.Message);
            Console.ForegroundColor = ConsoleColor.Gray;
            string sql = exAnyDB.SQL;
            for (Exception ex = exAnyDB.InnerException; ex != null; ex = ex.InnerException)
            {
                Console.WriteLine(ex.Message);
            }
            if (sql != null)
            {
                Console.Write("SQL> ");
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(sql);
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            if (extra != null) Console.WriteLine(extra);
            Console.WriteLine();
        }

        static void CreateAccessProcedures(Database db)
        {
            // requires ExtendedAnsiSQL=1 in the connection string

            db.ExecuteNonQuery(@"CREATE PROCEDURE AnyDB_testProcedure1 (iKey INTEGER,
                                                                        fAdd DOUBLE) AS
                                 SELECT column_four + fAdd
                                 FROM   AnyDB_test
                                 WHERE  column_one = iKey;");

            // no AnyDB_testProcedure2 because no output parameters
            ;

            db.ExecuteNonQuery(@"CREATE PROCEDURE AnyDB_testProcedure3 AS
                                 SELECT *
                                 FROM   AnyDB_test;");
            
            // only one query
            db.ExecuteNonQuery(@"CREATE PROCEDURE AnyDB_testProcedure4 AS
                                 SELECT   *
                                 FROM     AnyDB_test
                                 ORDER BY column_one ASC;");
        }
    }
}