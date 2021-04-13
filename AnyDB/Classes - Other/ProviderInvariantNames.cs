/********************************************************************************************************************
 * 
 * ProviderInvariantNames.cs
 * 
 * This would have been an enum, but you cannot have string enums in C#. So it's a static class instead, with readonly 
 * properties. It also acts as an internal lookup table to match the Providers enum against the actual provider string.
 */

using System.Collections.Generic;

namespace AnyDB
{
    /// <summary>
    /// .NET providers' assembly names.
    /// </summary>
    public static class ProviderInvariantNames
    {
        internal static Dictionary<Providers, string> Invariants = new Dictionary<Providers, string>()
        {
            { Providers.Access,          "System.Data.Odbc"                },
            { Providers.CUBRID,          "CUBRID.Data.CUBRIDClient"        },
            { Providers.DB2,             "IBM.Data.DB2"                    },
            { Providers.Excel,           "System.Data.Odbc"                },
            { Providers.Firebird,        "FirebirdSql.Data.FirebirdClient" },
            { Providers.Informix,        "IBM.Data.Informix"               },
            { Providers.MonetDB,         "System.Data.Odbc"                },
            { Providers.MySQL,           "MySql.Data.MySqlClient"          },
            { Providers.ODBC,            "System.Data.Odbc"                },
            { Providers.OLEDB,           "System.Data.OleDb"               },
            { Providers.Oracle,          "Oracle.ManagedDataAccess.Client" },
            { Providers.OracleUnmanaged, "Oracle.DataAccess.Client"        },
            { Providers.PostgreSQL,      "Npgsql"                          },
            { Providers.Rdb,             "System.Data.Odbc"                },
            { Providers.SQLite,          "System.Data.SQLite"              },
            { Providers.SQLServer,       "System.Data.SqlClient"           },
            { Providers.Text,            "System.Data.Odbc"                },
        };

        /// <summary>
        /// Microsoft Access -- "System.Data.Odbc"
        /// </summary>
        public static string Access
        {
            get
            {
                return Invariants[Providers.DB2];
            }
        }

        /// <summary>
        /// CUBRID -- "CUBRID.Data"
        /// </summary>
        public static string CUBRID
        {
            get
            {
                return Invariants[Providers.CUBRID];
            }
        }

        /// <summary>
        /// IBM DB2 -- "IBM.Data.DB2"
        /// </summary>
        public static string DB2
        {
            get
            {
                return Invariants[Providers.DB2];
            }
        }

        /// <summary>
        /// Firebird -- "FirebirdSql.Data.FirebirdClient"
        /// </summary>
        public static string Firebird
        {
            get
            {
                return Invariants[Providers.Firebird];
            }
        }

        /// <summary>
        /// IBM Informix -- "IBM.Data.Informix"
        /// </summary>
        public static string Informix
        {
            get
            {
                return Invariants[Providers.Informix];
            }
        }

        /// <summary>
        /// MonetDB -- "System.Data.Odbc".
        /// </summary>
        public static string MonetDB
        {
            get
            {
                return Invariants[Providers.MonetDB];
            }
        }

        /// <summary>
        /// MySQL -- "MySql.Data.MySqlClient"
        /// </summary>
        public static string MySQL
        {
            get
            {
                return Invariants[Providers.MySQL];
            }
        }

        /// <summary>
        /// ODBC -- "System.Data.Odbc"
        /// </summary>
        public static string ODBC
        {
            get
            {
                return Invariants[Providers.ODBC];
            }
        }

        /// <summary>
        /// OLEDB -- "System.Data.OleDb"
        /// </summary>
        public static string OLEDB
        {
            get
            {
                return Invariants[Providers.OLEDB];
            }
        }

        /// <summary>
        /// Oracle -- "Oracle.ManagedDataAccess.Client"
        /// </summary>
        public static string Oracle
        {
            get
            {
                return Invariants[Providers.Oracle];
            }
        }

        /// <summary>
        /// Oracle -- "Oracle.DataAccess.Client"
        /// </summary>
        public static string OracleUnmanaged
        {
            get
            {
                return Invariants[Providers.OracleUnmanaged];
            }
        }

        /// <summary>
        /// PostgreSQL -- "Npgsql"
        /// </summary>
        public static string PostgreSQL
        {
            get
            {
                return Invariants[Providers.PostgreSQL];
            }
        }

        /// <summary>
        /// DEC Rdb, now called Oracle Rdb -- "System.Data.Odbc"
        /// </summary>
        public static string Rdb
        {
            get
            {
                return Invariants[Providers.Rdb];
            }
        }

        /// <summary>
        /// SQLite -- "System.Data.SQLite"
        /// </summary>
        public static string SQLite
        {
            get
            {
                return Invariants[Providers.SQLite];
            }
        }

        /// <summary>
        /// SQLServer -- "System.Data.SqlClient"
        /// </summary>
        public static string SQLServer
        {
            get
            {
                return Invariants[Providers.SQLServer];
            }
        }

        /// <summary>
        /// ASCII text (comma delimited) -- "System.Data.OleDb".
        /// </summary>
        public static string Text
        {
            get
            {
                return Invariants[Providers.Text];
            }
        }
    }
}