/********************************************************************************************************************
 * 
 * Database_Reader.cs
 * 
 * Methods for fetching rows of a SELECT statement in a through a DataReader(). Although a DataTable is generally more 
 * reliable, sometimes you just need to read rows and write them out one at a time. Another reason for using a DataReader 
 * is that you don't have to wait for all the data to be read into a collection before doing something with the first 
 * couple of rows.
 */

using System;
using System.Data;
using System.Data.Common;

namespace AnyDB
{
    public partial class Database
    {
        /// <summary>
        /// Returns a DbDataReader for your SQL query. It also accepts a variable number of parameters, which will be 
        /// bound to any parameter markers in your SQL statement. This method should be called within a using block to 
        /// ensure that the DbDataReader is properly disposed after use. Improper use of data readers of results in 
        /// orphaned connections and handle leaks. If you just want to get some data out of a database, a DataTable is 
        /// generally much more reliable.
        /// </summary>
        /// <param name="SelectStatement">
        /// An SQL SELECT statement, which can (and should) include parameter markers.
        /// </param>
        /// <param name="QueryParameters">
        /// Parameters to bind to parameter markers in the SELECT statement.
        /// </param>
        /// <returns>A DataReader.</returns>

        public DbDataReader GetDataReader(string SelectStatement, params object[] QueryParameters)
        {
            DbConnection connect = null;
            string sql = SelectStatement;

            try
            {
                sql = RewriteQuery(SelectStatement);    // text driver throws invalid table here

                /*
                 * Firebird and Postgres don't close connections when a DbDataReader is disposed. So we'll have to 
                 * bypass connection pooling to ensure we don't run out of handles. But you don't want to do this if 
                 * the connection is used by a Transaction.
                 */

                CommandBehavior cb = CommandBehavior.Default;

                if (Driver.QuirkDataReaderCloseConnection)
                {
                    if (Transaction == null) cb = CommandBehavior.CloseConnection;
                }

                /*
                 * Now let's get on with the job. I would put the DbCommand and DbConnection into using blocks, but some 
                 * databases won't let you use the DbDataReader after the Connection is disposed by the using block 
                 * (which, I suppose, is the correct behaviour). We'll just have to let the local variables go out of 
                 * scope by themselves, and be collected once the DataReader releases the last reference.
                 * 
                 * I wish it were possible for certain methods to insist that they be called in a using block, just like 
                 * you can define base classes that cannot be instantiated but only inherited. It's frightning when 
                 * people stop thinking about allocation and freeing of resources.
                 */

                var command = Driver.CreateCommand();
                BindParameters(command, ref sql, QueryParameters);

                connect = CreateOrReuseConnection(ConnectionString);

                command.CommandText = sql;
                command.Connection = connect;
                command.Transaction = PossiblyUseTransaction();
                return command.ExecuteReader(cb);              // other drivers throw here
            }
            catch (Exception ex)
            {
                // Do Dispose() connection, there's NO DATA.
                DisposeTemporaryConnection(connect);
                throw PossibleAnyDbException(sql, ex);
            }
            finally
            {
                // Don't Dispose() connection, there IS data.
            }
        }
    }
}