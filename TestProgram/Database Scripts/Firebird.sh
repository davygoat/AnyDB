#!/bin/sh

# vim:set ts=4:

grep RemoteBindAddress /etc/firebird/2.5/firebird.conf

# Firebird ###############################################################
#
# apt-get install firebird2.5-superclassic
# dpkg-reconfigure firebird2.5-superclassic          # yes to start server
#

cat <<EOD














EOD

isql-fb -e <<EOD
	CONNECT localhost:/var/lib/firebird/2.5/data/AnyDB.fdb USER SYSDBA PASSWORD database;
	DROP USER $USER;
	DROP USER test;
	DROP DATABASE;
	COMMIT;
EOD

isql-fb -e <<EOD
	CREATE DATABASE "localhost:/var/lib/firebird/2.5/data/AnyDB.fdb" USER 'SYSDBA' PASSWORD 'database';
	CREATE USER $USER PASSWORD 'fred';
	CREATE USER test PASSWORD 'lemming';
EOD

isql-fb -e <<EOD

	CONNECT localhost:/var/lib/firebird/2.5/data/AnyDB.fdb USER $USER PASSWORD fred;

	CREATE TABLE AnyDB_numbers
	(
	   number_text VARCHAR(80) NOT NULL PRIMARY KEY
	);

	INSERT INTO AnyDB_numbers VALUES ('One');
	INSERT INTO AnyDB_numbers VALUES ('Two');
	INSERT INTO AnyDB_numbers VALUES ('Three');
	INSERT INTO AnyDB_numbers VALUES ('Four');
	INSERT INTO AnyDB_numbers VALUES ('Five');
	INSERT INTO AnyDB_numbers VALUES ('OneOneOne');

	COMMIT;
	
	CONNECT localhost:/var/lib/firebird/2.5/data/AnyDB.fdb USER $USER PASSWORD fred;

	CREATE TABLE AnyDB_test
	(
	   column_one   INTEGER          NOT NULL,
	   column_two   VARCHAR(80)      NOT NULL,
	   column_three TIMESTAMP        NOT NULL,
	   column_four  DOUBLE PRECISION NOT NULL,
	   PRIMARY KEY (column_one),
	   FOREIGN KEY (column_two) REFERENCES AnyDB_numbers,
	   UNIQUE      (column_two),
	   CHECK       (column_four > 0)
	);
	
	SET TERM ##;

	CREATE PROCEDURE AnyDB_testProcedure1 (iKey INTEGER,
	                                       fAdd REAL)
	RETURNS (retVal REAL) AS
	BEGIN
	
	   SELECT column_four + :fAdd
	   FROM   AnyDB_test
	   WHERE  column_one = :iKey
	   INTO   :retVal;
	
	END
	##
	
	CREATE PROCEDURE AnyDB_testProcedure2 (iInput INTEGER)
	RETURNS (sOutputStr VARCHAR(80),
	         fOutputFloat FLOAT) AS
	BEGIN
	
	   SELECT column_two,
	          column_four
	   FROM   AnyDB_test
	   WHERE  column_one = :iInput
	   INTO   :sOutputStr,
	          :fOutputFloat;
	
	END
	##
	
	CREATE PROCEDURE AnyDB_testProcedure3
	RETURNS (column_one   INTEGER,
	         column_two   VARCHAR(80),
	         column_three TIMESTAMP,
	         column_four  FLOAT) AS
	BEGIN
	   FOR SELECT column_one,
	              column_two,
	              column_three,
	              column_four
	       FROM   AnyDB_test
	       INTO   :column_one,
	              :column_two,
	              :column_three,
	              :column_four DO
	   BEGIN
	      SUSPEND;
	   END
	END
	##
	
	CREATE PROCEDURE AnyDB_testProcedure4
	RETURNS (column_one   INTEGER,
	         column_two   VARCHAR(80),
	         column_three TIMESTAMP,
	         column_four  FLOAT) AS
	BEGIN
	   FOR SELECT   column_one,
	                column_two,
	                column_three,
	                column_four
	       FROM     AnyDB_test
	       ORDER BY column_one DESC
	       INTO     :column_one,
	                :column_two,
	                :column_three,
	                :column_four DO
	   BEGIN
	      SUSPEND;
	   END
	END
	##
	
	SET TERM ;##

	GRANT SELECT,INSERT,UPDATE,DELETE ON AnyDB_test TO test;
	GRANT EXECUTE ON PROCEDURE AnyDB_testProcedure1 TO test;
	GRANT EXECUTE ON PROCEDURE AnyDB_testProcedure2 TO test;
	GRANT EXECUTE ON PROCEDURE AnyDB_testProcedure3 TO test;
	GRANT EXECUTE ON PROCEDURE AnyDB_testProcedure4 TO test;

	COMMIT;

EOD
