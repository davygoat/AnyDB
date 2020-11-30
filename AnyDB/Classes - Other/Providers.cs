/********************************************************************************************************************
 * 
 * Providers.cs
 * 
 * An enum of all the supported database providers.
 */

namespace AnyDB
{
    /// <summary>
    /// 
    /// </summary>
    
    public enum Providers
    {
        /// <summary>
        /// Microsoft Access, using ODBC.
        /// </summary>
        Access,

        /// <summary>
        /// CUBRID.
        /// </summary>
        CUBRID,

        /// <summary>
        /// IBM DB2 data provider.
        /// </summary>
        DB2,

        /// <summary>
        /// Microsoft Excel, using ODBC.
        /// </summary>
        Excel,

        /// <summary>
        /// Firebird ADO.NET Data Provider (www.firebirdsql.org).
        /// </summary>
        Firebird,

        /// <summary>
        /// IBM Informix data provider. (Providers.DB2 also works.)
        /// </summary>
        Informix,

        /// <summary>
        /// MonetDB, a column-oriented database.
        /// </summary>
        MonetDB,

        /// <summary>
        /// Connector/Net, a fully managed ADO.NET driver for for MySQL (www.mysql.com), which also works for MariaDB 
        /// (www.mariadb.org).
        /// </summary>
        MySQL,

        /// <summary>
        /// ODBC, which is like common denominator that will enable you to communicate with just about any database, 
        /// and even a few non databases.
        /// </summary>        
        ODBC,

        /// <summary>
        /// OLEDB.
        /// </summary>
        OLEDB,

        /// <summary>
        /// Oracle.DataAccess.Client, aka ODP.NET, aka ODAC.
        /// </summary>
        OracleUnmanaged,

        /// <summary>
        /// Oracle.ManagedDataAccess.Client, aka ODP.NET, aka ODAC.
        /// </summary>
        Oracle,

        /// <summary>
        /// Npgsql, .NET Data Provider (npgsql.projects.pgfoundry.org) 
        /// for PostgreSQL (www.postgresql.org).
        /// </summary>
        PostgreSQL,

        /// <summary>
        /// DEC Rdb, now called Oracle Rdb. The ORDP provider still has bugs, so we'll use the ODBC driver instead. 
        /// Just make sure to supply an ODBC connection string.
        /// </summary>
        Rdb,
        
        /// <summary>
        /// System.Data.SQLite, an ADO.NET provider for SQLite (www.sqlite.org).
        /// </summary>
        SQLite,

        /// <summary>
        /// System.Data.SqlClient, the .NET framework's own provider for Microsoft SQL Server.
        /// </summary>
        SQLServer,

        /// <summary>
        /// ASCII text, usually comma delimited.
        /// </summary>
        Text,
    }
}