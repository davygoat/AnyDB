#!/bin/sh

# vim:set ts=4:

# DB2 #################################################################################
#
# Neither yourself nor root have permission to CREATE DATABASE.
# Create the database as user db2inst1.
# Now you have a database and CREATE TABLE privilege by default.
#
# Run this script under the test account.
#

if [ ! -d /opt/ibm/db2 ]; then
	echo
	echo "First install DB2:"
	echo
	echo "dpkg -i db2exc_i386.deb    # old V9.7 because I have a 32 bit VirtualBox"
	echo "apt-get install -f db2exc  # complains about unmet dependencies, no problem"
	echo "apt-get -f install         # installs missing packages, inc. db2exc."
	echo
	exit
fi

echo
echo "Creating database AnyDB as user db2inst1. This'll take a while..."
echo

sudo su db2inst1 <<-EOD
	. /home/db2inst1/sqllib/db2profile
	db2 create database AnyDB
EOD

db2 -td@ <<-EOD

	CONNECT TO AnyDB@

	CREATE TABLE AnyDB_numbers
	(
	   number_text VARCHAR(80) NOT NULL PRIMARY KEY
	)@

	INSERT INTO AnyDB_numbers VALUES ('One')@
	INSERT INTO AnyDB_numbers VALUES ('Two')@
	INSERT INTO AnyDB_numbers VALUES ('Three')@
	INSERT INTO AnyDB_numbers VALUES ('Four')@
	INSERT INTO AnyDB_numbers VALUES ('Five')@
	INSERT INTO AnyDB_numbers VALUES ('OneOneOne')@

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
	)@

	CREATE PROCEDURE AnyDB_testProcedure1 (IN iKey INTEGER, 
										   IN fAdd DOUBLE PRECISION)
	BEGIN

	   DECLARE cur CURSOR WITH RETURN TO CLIENT FOR
	   SELECT  column_four + fAdd
	   FROM    AnyDB_test
	   WHERE   column_one = iKey;

	   OPEN cur;

	END
	@

	CREATE PROCEDURE AnyDB_testProcedure2 (IN  iInput INT,
										   OUT sOutputStr VARCHAR(80),
										   OUT sOutputFloat REAL)
	BEGIN

	   SELECT column_two,
			  column_four
	   INTO   sOutputStr,
			  sOutputFloat
	   FROM   AnyDB_test
	   WHERE  column_one = iInput;

	END
	@

	CREATE PROCEDURE AnyDB_testProcedure3()
	BEGIN

	   DECLARE cur CURSOR WITH RETURN TO CLIENT FOR
	   SELECT  *
	   FROM    AnyDB_test;

	   OPEN cur;

	END
	@

	CREATE PROCEDURE AnyDB_testProcedure4 ()
	BEGIN

	   DECLARE  cur1 CURSOR WITH RETURN TO CLIENT FOR
	   SELECT   *
	   FROM     AnyDB_test
	   ORDER BY column_one ASC;

	   DECLARE  cur2 CURSOR WITH RETURN TO CLIENT FOR
	   SELECT   *
	   FROM     AnyDB_test
	   ORDER BY column_one DESC;

	   OPEN cur1;
	   OPEN cur2;

	END
	@

	GRANT SELECT,INSERT,UPDATE,DELETE ON TABLE AnyDB_test TO test@
	GRANT EXECUTE ON PROCEDURE AnyDB_testProcedure1 TO test@
	GRANT EXECUTE ON PROCEDURE AnyDB_testProcedure2 TO test@
	GRANT EXECUTE ON PROCEDURE AnyDB_testProcedure3 TO test@
	GRANT EXECUTE ON PROCEDURE AnyDB_testProcedure4 TO test@

EOD
