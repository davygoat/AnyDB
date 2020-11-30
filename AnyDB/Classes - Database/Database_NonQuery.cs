/********************************************************************************************************************
 * 
 * Database_NonQuery.cs
 * 
 * Methods for running non queries, which are SQL statements such as INSERT, UPDATE and "DROP TABLE students" if your
 * name is Robert.
 */

using System;

namespace AnyDB
{
    public partial class Database
    {
        /// <summary>
        /// Executes an SQL statement, typically an INSERT, UPDATE or DELETE.
        /// </summary>
        /// <param name="SqlStatement">The SQL statement to execute, which can use parameter markers.</param>
        /// <param name="QueryParameters">Parameters to bind to the SQL statement.</param>
        /// <returns>Typically the number of rows affected by the UPDATE statement.</returns>

        public int ExecuteNonQuery(string SqlStatement, params object[] QueryParameters)
        {
            string sql = SqlStatement;
            try
            {
                sql = RewriteQuery(sql);

                using (var command = Driver.CreateCommand())
                {
                    BindParameters(command, ref sql, QueryParameters);

                    var connect = CreateOrReuseConnection(ConnectionString);
                    try
                    {
                        command.CommandText = sql;
                        command.Connection = connect;
                        command.Transaction = PossiblyUseTransaction();
                        return command.ExecuteNonQuery();
                    }
                    finally
                    {
                        DisposeTemporaryConnection(connect);
                    }
                }
            }
            catch (Exception ex)
            {
                throw PossibleAnyDbException(sql, ex);
            }
        }
    }
}