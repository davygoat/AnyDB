#!/bin/sh

#
# I'm keeping this file just in case, but I'm not planning on revisiting MaxDB any
# time soon. It's just too much hassle.
#

# vim:set ts=4:

echo
echo "  ==========================================================================="
echo "  =                                                                         ="
echo "  =  Expect to see lots of 'prompts'.   This takes a while to run, so just  ="
echo "  =  WAIT for the  'Installation Successfully Finished'  whoosh,  and then  ="
echo "  =  for the DDL statements to finish off.                                  ="
echo "  =                                                                         ="
echo "  ==========================================================================="
echo

#
# Stop 'n' drop to start from scratch.
#

dbmcli -d AnyDB -u DBM,DBM <<-EOD
	db_stop
	db_drop
EOD

#
# Create the database.
#

dbmcli db_create AnyDB DBM,DBM

#
# Now we need a whole load of 'over my head' configuration.
#

dbmcli -d AnyDB -u DBM,DBM <<-EOD
	param_startsession
	param_init
	param_put CacheMemorySize 1000
	param_put MaxDataVolumes 64
	param_put MaxUserTasks 10
	param_checkall
	param_commitsession
	param_addvolume 1 DATA DISKD0001 F 32768
	param_addvolume 1 LOG  DISKL0001 F  6400
	db_admin
	db_activate DBADMIN,database
	load_systab
EOD

echo
echo "Wait... There's more..."
echo

dbmcli -d AnyDB -u DBM,DBM <<-EOD
	auto_update_statistics ON
	db_execute SET LOG AUTO OVERWRITE ON
EOD

#
# Create a user with DBA rights. It seems you can't create a user
# without a password.
#

sqlcli -u DBADMIN,database -d AnyDB CREATE USER $USER PASSWORD fred DBA NOT EXCLUSIVE

#
# The cursor MUST be called $CURSOR. The dollar sign has to be escaped to
# avoid shell expansion. That's just an unfortunate consequence of my 
# preference for a "here document". Also note that table names within a 
# procedure MUST be qualified. The default schema happens to be the same
# as the definer's database id, similar to Oracle. 
#

(
cat <<-EOD

	CREATE TABLE AnyDB_test
	(
	   column_one   INTEGER NOT NULL,
	   column_two   VARCHAR(80),
	   column_three TIMESTAMP,
	   column_four  DOUBLE PRECISION,
	   PRIMARY KEY (column_one)
	)
	//

	CREATE DBPROC AnyDB_testProcedure1 (IN iKey INTEGER, 
	                                    IN fAdd FLOAT) 
	RETURNS CURSOR AS
	   \$CURSOR = 'THECURSOR';
	BEGIN
	   DECLARE :\$CURSOR CURSOR FOR
	   SELECT  column_four + :fAdd 
	   FROM    ${USER}.AnyDB_test 
	   WHERE   column_one = :iKey;
	END;
	//

	CREATE DBPROC AnyDB_testProcedure2 (IN  iInput INTEGER,
	                                    OUT sOutputStr VARCHAR(80),
	                                    OUT sOutputFloat REAL) AS
	BEGIN
	   SELECT column_two,
	          column_four
	   INTO   :sOutputStr,
	          :sOutputFloat
	   FROM   ${USER}.AnyDB_test
	   WHERE  column_one = :iInput;
	END;
	//

	CREATE DBPROC AnyDB_testProcedure3
	RETURNS CURSOR AS
	   \$CURSOR = 'THECURSOR';
	BEGIN
	   DECLARE :\$CURSOR CURSOR FOR
	   SELECT  *
	   FROM    ${USER}.AnyDB_test;
	END;
	//

	CREATE DBPROC AnyDB_testProcedure4
	RETURNS CURSOR AS
	   \$CURSOR = 'THECURSOR';
	BEGIN
	   DECLARE  :\$CURSOR CURSOR FOR
	   SELECT   *
	   FROM     ${USER}.AnyDB_test
	   ORDER BY column_one DESC;
	END;
	//

	COMMIT
	//
EOD
) | sed 's/^	//g;' >.anydb.sdb.sql

#
# You can't do this with a "here document". Hence the temporary
# file.
#

sqlcli -u ${USER},fred -d AnyDB -z -f -i .anydb.sdb.sql
rm .anydb.sdb.sql

echo
echo "  ==========================================================================="
echo "  =                                                                         ="
echo "  =                       Phew. That took a bit of doing!                   ="
echo "  =                                                                         ="
echo "  ==========================================================================="
echo
exit

-- MaxDB -----------------------------------------------------------------

// 
// Admin user (by convention): DBADMIN
// 
// \h             HELP
// \dt [PATTERN]  SHOW TABLES     -- note: * doesn't work, use % instead
// \dp [PATTERN]  SHOW PROCEDURES -- note: * doesn't work, use % instead
// \mu ON         multiline mode (or use -m command line option)
// 
// ODBC username,password must be uppercase
// 
//
// Run scripts the following command:
//
// $ sqlcli -d DATABASE -u USER,PASSWORD -i FILENAME -f
//
// -d    database name
// -u    username-comma-password
// -i    input filename
// -f    print SQL commands
// -z    disable auto-commit
// -cSEP separator, default is //.
//
// To change the default separator, use -c option, e.g. -cGO for Microsoft style.
//
// WARNING: Looks like Informix and SAP do not coexist. SAP (the ERP) is no 
//          longer supported on Informix. You're not even allowed to use
//          MaxDB for SAP the ERP. Says something, I think. ;-)
//
// Registry cleaning: M790823, MaxDB, "SAP ", \sdb\, (e.g.) SAP1 (db name).
//
