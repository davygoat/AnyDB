/********************************************************************************************************************
 * 
 * Database_DataSet.cs
 * 
 * Methods for returning the result of one or more multiple queries in a DataSet.
 */

using System;
using System.Data;
using System.Data.Common;

namespace AnyDB
{
    public partial class Database
    {
        /// <summary>
        /// Returns the result of one or more SELECT statements in a DataSet. The DataSet.Tables collection is used to 
        /// access the multiple query results.
        /// </summary>
        /// <param name="SelectStatement">
        /// An SQL SELECT statement(s), which can (and should) include parameter markers.
        /// </param>
        /// <param name="QueryParameters">
        /// Parameters to bind to parameter markers in the SELECT statements. You will need as many actual parameters 
        /// as your SQL statements have formal parameter markers, even if your SQL statement uses named markers.
        /// </param>
        /// <returns>
        /// A DataSet. Use the Tables collection within to access the individual DataTable for each query.
        /// </returns>

        public DataSet GetDataSet(string SelectStatement, params object[] QueryParameters)
        {
            string sql = SelectStatement;
            DbConnection connect = null;

            try
            {
                sql = RewriteQuery(SelectStatement);          // text driver throws invalid table here

                using (var command = Driver.CreateCommand())
                {
                    BindParameters(command, ref sql, QueryParameters);

                    connect = CreateOrReuseConnection(ConnectionString);

                    using (var adapt = Driver.CreateDataAdapter())
                    {
                        adapt.SelectCommand = command;
                        adapt.SelectCommand.Connection = connect;
                        adapt.SelectCommand.CommandText = sql;
                        adapt.SelectCommand.Transaction = PossiblyUseTransaction();
                        try
                        {
                            DataSet ds = new DataSet();
                            adapt.Fill(ds);
                            if (Driver.QuirkPaddedStrings)
                                foreach (DataTable dt in ds.Tables) TrimDataTableStrings(dt);
                            return ds;
                        }
                        finally
                        {
                            adapt.SelectCommand = null; // Workaround for SPlite-to-SQLite InvalidCastException
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw PossibleAnyDbException(sql, ex);
            }
            finally
            {
                DisposeTemporaryConnection(connect);
            }
        }
    }
}