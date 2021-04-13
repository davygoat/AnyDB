using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace AnyDB
{
    public partial class Database
    {
        Exception PossibleAnyDbException(string sql, Exception ex)
        {
            Match m;
            string msg = ex.Message;
            bool PneU = Driver.HasUniqueException && Driver.HasPrimaryKeyException;
            if (PneU)
            {
                if ((m=Driver.rePrimaryKeyException.Match(msg)).Success)
                {
                    if (m.Groups.Count >= 2) return new FailedPrimaryKeyException(sql, ex, m.Groups["NAME"].Value);
                    else return new FailedPrimaryKeyException(sql, ex);
                }
            }
            if ((m=Driver.reNotUniqueException.Match(msg)).Success)
            {
                if (m.Groups.Count >= 2) return new FailedUniqueConstraintException(sql, ex, m.Groups["NAME"].Value, PneU);
                else return new FailedUniqueConstraintException(sql, ex, null, PneU);
            }
            if ((m=Driver.reInvalidColumnException.Match(msg)).Success)
            {
                if (m.Groups.Count >= 2) return new ColumnNotFoundException(sql, ex, m.Groups["NAME"].Value);
                else return new ColumnNotFoundException(sql, ex);
            }
            if ((m=Driver.reInvalidTableException.Match(msg)).Success)
            {
                if (m.Groups.Count >= 2) return new TableNotFoundException(sql, ex, m.Groups["NAME"].Value);
                else return new TableNotFoundException(sql, ex);
            }
            if ((m=Driver.reInvalidProcedureException.Match(msg)).Success)
            {
                if (m.Groups.Count >= 2) return new ProcedureNotFoundException(sql, ex, m.Groups["NAME"].Value);
                else return new ProcedureNotFoundException(sql, ex);
            }
            if ((m=Driver.reForeignKeyException.Match(msg)).Success)
            {
                if (m.Groups.Count >= 2) return new FailedForeignKeyException(sql, ex, m.Groups["NAME"].Value);
                else return new FailedForeignKeyException(sql, ex);
            }
            if ((m=Driver.reCheckConstraintException.Match(msg)).Success)
            {
                if (m.Groups.Count >= 2) return new FailedCheckConstraintException(sql, ex, m.Groups["NAME"].Value);
                else return new FailedCheckConstraintException(sql, ex);
            }
            if ((m=Driver.reNotNullException.Match(msg)).Success)
            {
                if (m.Groups.Count >= 2) return new FailedNotNullException(sql, ex, m.Groups["NAME"].Value);
                else return new FailedNotNullException(sql, ex);
            }
            if ((m=Driver.rePermissionDeniedException.Match(msg)).Success)
            {
                if (m.Groups.Count >= 2) return new PermissionDeniedException(sql, ex, m.Groups["NAME"].Value);
                else return new PermissionDeniedException(sql, ex);
            }
            return new AnyDbException(sql, ex.Message, ex);
        }
    }

    /// <summary>
    /// A generic AnyDB exception. All AnyDB exceptions, including PrimaryKeyException, TableNotFoundException, etc.
    /// derive from this Exception type. Use this exception if you want to catch *everything* thrown or rethrown by 
    /// AnyDB.
    /// </summary>
    public class AnyDbException : DbException
    {
        /// <summary>
        /// The SQL statement that led to the error. Useful to include in your error logs.
        /// </summary>
        public string SQL;

        internal AnyDbException(string sql, string msg, Exception inner)
            : base(msg, inner)
        {
            this.SQL = sql;
        }

        internal AnyDbException(string msg)
            : base(msg)
        {
        }
    }

    /// <summary>
    /// This exception is the base class for constructor related errors. It is thrown by the Database constructor
    /// if it cannot make a connection to the database.
    /// </summary>
    public class ConstructorException : AnyDbException
    {
        internal ConstructorException(string constr, string provider, string message, Exception ex)
            : base(constr, message, ex)
        {
            this.SQL = provider + " / " + constr;
        }
    }

    /// <summary>
    /// This exception is thrown if we cannot connect to the database because the required .NET Data Provider is not
    /// installed on the system.
    /// </summary>
    public class ProviderNotFoundException : ConstructorException
    {
        internal ProviderNotFoundException(string constr, string provider, Exception ex)
            : base(constr, provider, "'" + provider + "' data provider not found.", ex)
        {
        }
    }

    /// <summary>
    /// This exception is thrown if we cannot connect to the database because the required ODBC driver is not installed.
    /// </summary>
    public class DriverNotFoundException : ConstructorException
    {
        internal DriverNotFoundException(string constr, string driver, Exception ex)
            : base(constr, driver, "'" + driver + "' ODBC driver not found.", ex)
        {
        }
    }

    /// <summary>
    /// This exception is thrown if we cannot connect to the database, but we don't exactly know why.
    /// </summary>
    public class ConnectException : ConstructorException
    {
        internal ConnectException(string constr, string provider, Exception ex)
            : base(constr, provider, "Cannot connect to database.", ex)
        {
        }
    }

    /// <summary>
    /// This exception is the base class for singleton select exceptions. A singleton select must return exactly one
    /// row, not zero, not more.
    /// </summary>
    public class SingletonException : AnyDbException
    {
        internal SingletonException(string sql, string msg, Exception ex)
            : base(sql, msg, ex)
        {
        }
    }

    /// <summary>
    /// This exception is thrown if a singleton select returned zero records.
    /// </summary>
    public class NoDataException : SingletonException
    {
        internal NoDataException(string sql)
            : base(sql, "Singleton select returned NO DATA.", new KeyNotFoundException())
        {
        }

        internal NoDataException(string sql, Exception ex)
            : base(sql, "Singleton select returned NO DATA.", ex)
        {
        }
    }

    /// <summary>
    /// This exception is thrown if a singleton select returned multiple records.
    /// </summary>
    public class MultipleDataException : SingletonException
    {
        internal MultipleDataException(string sql)
            : base(sql, "Singleton select returned MULTIPLE records.", new IndexOutOfRangeException())
        {
        }
    }

    /// <summary>
    /// You are not allowed to use this table, column, stored procedure, etc. Check your permissions.
    /// </summary>
    public class PermissionDeniedException : AnyDbException
    {
        /// <summary>
        /// What object this except refers to, e.g. most commonly the table name.
        /// </summary>
        public string Reference;

        internal PermissionDeniedException(string sql, Exception ex, string what=null, string orig=null)
            : base(sql,
                   what  != null ? "Permission denied for '" + what + "'." 
                 :                 "Permission denied.", 
                   ex)
        {
            this.Reference = what;
        }
    }

    /// <summary>
    /// This exception will be thrown if the database returns an invalid column error. That usually means you have
    /// an error in your SQL query.
    /// </summary>
    public class ColumnNotFoundException : AnyDbException
    {
        /// <summary>
        /// Which column you were trying to access, so you check for typos.
        /// </summary>
        public string ColumnName;

        internal ColumnNotFoundException(string sql, Exception ex, string col=null, string orig=null)
            : base(sql,
                   orig != null ? orig + " (That probably means \"column not found\".)" 
                 : col  != null ? "Column '" + col + "' not found." 
                 :                "Column not found.", 
                   ex)
        {
            this.ColumnName = col;
        }
    }

    /// <summary>
    /// This exception is thrown if you try to query a table that does not exist. That usually means you have an error 
    /// in your query. Some database also throw this exception if you lack the necessary <i>permission</i> to use the
    /// the table in question, in which case you should check your permissions. If you are using the Text driver, then
    /// this exception can also be thrown if you are trying to query a text file that does not exist in the "database" 
    /// directory, or has the wrong extension.
    /// </summary>
    public class TableNotFoundException : AnyDbException
    {
        /// <summary>
        /// Which nonexistent table you were trying use. Check for typos.
        /// </summary>
        public string TableName;

        internal TableNotFoundException(string sql, Exception ex, string tbl=null, string msg=null)
            : base(sql,
                   msg != null ? msg
                 : tbl != null ? "Table '" + tbl + "' not found."
                 :               "Table not found.", 
                   ex)
        {
            this.TableName = tbl;
        }
    }

    /// <summary>
    /// This exception is thrown if you try to call a stored procedure that does not exist (typo), or in some cases if 
    /// you lack the necessary permission to execute the procedure (grant).
    /// </summary>
    public class ProcedureNotFoundException : AnyDbException
    {
        /// <summary>
        /// Which procedure you tried to execute. Check for typos.
        /// </summary>
        public string ProcedureName;

        internal ProcedureNotFoundException(string sql, Exception ex, string prc=null)
            : base(sql,
                   prc != null ? "Procedure '" + prc + "' not found." 
                 :               "Procedure not found.", 
                   ex)
        {
            this.ProcedureName = prc;
        }
    }

    /// <summary>
    /// An INSERT or UPDATE statement failed because it would have resulted in a column not being unique. Some
    /// databases also throw this exception on a primary key violation.
    /// </summary>
    public class FailedUniqueConstraintException : FailedGenericConstraintException
    {
        internal FailedUniqueConstraintException(string sql, Exception ex, string what, bool PneU, string msg=null)
            : base(sql,
                   ex, 
                   what, 
                   msg  != null ? msg
                 : what != null ? "Failed UNIQUE" + (!PneU ? " (or PRIMARY KEY)" : "") + " check on '" + what + "'" 
                 :                "Failed UNIQUE" + (!PneU ? " (or PRIMARY KEY)" : "") + " check.")
        {
        }
    }

    /// <summary>
    /// An INSERT or UPDATE statement failed because it conflicted with the table's primary key. The primary key must
    /// be unique by definition. Some database also throw UNIQUE constraint violations as a primary key exception,
    /// others will throw a FailedUniqueConstraintException on a primary key violation.
    /// </summary>
    public class FailedPrimaryKeyException : FailedUniqueConstraintException
    {
        internal FailedPrimaryKeyException(string sql, Exception ex, string what=null)
            : base(sql,
                   ex,
                   what,
                   false,
                   what != null ? "Failed PRIMARY KEY check on '" + what + "'." 
                 :                "Failed PRIMARY KEY check.")
        {
        }
    }

    /// <summary>
    /// An INSERT or UPDATE failed because you tried to give a column a value that conflicted with its FOREIGN KEY
    /// or REFERENCES clause (domain integrity). This error also occurs if you attempt to delete a record from a table 
    /// while that record is still referred to in other tables (referential integrity).
    /// </summary>
    public class FailedForeignKeyException : FailedGenericConstraintException
    {
        internal FailedForeignKeyException(string sql, Exception ex, string what=null)
            : base(sql,
                   ex,
                   what,
                   what != null ? "Failed FOREIGN KEY check on '" + what + "'." 
                 :                "Failed FOREIGN KEY check.")
        {
        }
    }

    /// <summary>
    /// An INSERT or UPDATE failed because you tried to set a field to NULL when it it is constrained to be NOT NULL.
    /// </summary>
    public class FailedNotNullException : FailedGenericConstraintException
    {
        internal FailedNotNullException(string sql, Exception ex, string what=null)
            : base(sql,
                   ex,
                   what,
                   what != null ? "Failed NOT NULL check on '" + what + "'." 
                 :                "Failed NOT NULL check.")
        {
        }
    }

    /// <summary>
    /// An INSERT or UPDATE failed because you tried to set a field to some value it is not allowed to have, e.g. if
    /// the field has to be between a given minimum and maximum value.
    /// </summary>
    public class FailedCheckConstraintException : FailedGenericConstraintException
    {
        internal FailedCheckConstraintException(string sql, Exception ex, string what=null)
            : base(sql,
                   ex,
                   what,
                   what != null ? "Failed CHECK constraint on '" + what + "'." 
                 :                "Failed CHECK constraint.")
        {
        }
    }

    /// <summary>
    /// Base class for PRIMARY KEY, FOREIGN KEY, UNIQUE, NOT NULL and CHECK constraint exceptions. You can use this
    /// as a catch-all for all these constraint related exceptions.
    /// </summary>
    public class FailedGenericConstraintException : AnyDbException
    {
        /// <summary>
        /// What this constraint refers to, e.g. table name, column name, constraint id, depending on the database
        /// product. Any useful 'handle' I can give you as context.
        /// </summary>
        public string Reference;

        internal FailedGenericConstraintException(string sql, Exception ex, string what=null, string msg=null)
            : base(sql,
                   msg  != null ? msg
                 : what != null ? "Failed unspecified constraint on '" + what + "'." 
                 :                "Failed unspecified constraint.", 
                   ex)
        {
            this.Reference = what;
        }
    }
}