/********************************************************************************************************************
 * 
 * Database_Prepare.cs
 * 
 * A method to precompile a parameterised SQL query so you can run it lots of times without incurring the overhead of 
 * constant recompiling. A prepared statement also has the advantage of keeping the connection open. You can speed 
 * things up even more if you wrap the whole thing in a transaction.
 */

namespace AnyDB
{
    public partial class Database
    {
        /// <summary>
        /// Prepares a parameterised SQL statement, essentially precompiling it so it can be used repeatedly with less 
        /// overhead. Use this method if you have a complex SQL command that you want to run with a batch of data.
        /// </summary>
        /// <param name="SqlStatement">SQL statement to compile.</param>
        /// <param name="QueryParameters">Parameters to bind to the SQL statement.</param>
        /// <returns></returns>
        public PreparedStatement Prepare(string SqlStatement, params object[] QueryParameters)
        {
            string sql = RewriteQuery(SqlStatement);

            using (var command = Driver.CreateCommand())
            {
                string originalSQL = sql;
                BindParameters(command, ref sql, QueryParameters);
                var connect = CreateOrReuseConnection(ConnectionString);
                command.CommandText = sql;
                command.Connection = connect;
                command.Transaction = PossiblyUseTransaction();
                return new PreparedStatement(this, command, originalSQL);
            }
        }
    }
}