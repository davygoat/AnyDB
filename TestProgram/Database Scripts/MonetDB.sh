#!/bin/sh

# vim:set ts=4:

set -e

if [ -z "`which mclient`" ]; then
	echo
	echo "MonetDB is not installed. Run the following commands as root."
	echo
	echo -----------------------------------------------------------------------------------------------
	cat <<-EOD

		# create monetdb.list
		echo -n >/etc/apt/sources.list.d/monetdb.list
		echo "deb http://dev.monetdb.org/downloads/deb/ jessie monetdb" >>/etc/apt/sources.list.d/monetdb.list 
		echo "deb-src http://dev.monetdb.org/downloads/deb/ jessie monetdb" >>/etc/apt/sources.list.d/monetdb.list 

		# install MonetDB public key
		wget --output-document=- http://monetdb.org/downloads/MonetDB-GPG-KEY | apt-key add -

		# update apt-get
		apt-get update

		# install MonetDB
		apt-get install -y monetdb5-sql monetdb-client

		# add $USER to monetdb group
		usermod -a -G monetdb $USER

		# start monetdb
		ex /etc/default/monetdb5-sql -c '0,$s/^STARTUP="no"/STARTUP="yes"' -c wq
		echo 'port=50005' >>/var/lib/monetdb/.merovingian_properties
		echo 'listenaddr=0.0.0.0' >>/var/lib/monetdb/.merovingian_properties
		/etc/init.d/monetdb5-sql start

		# then log out and log back in to pick up your 'monet' group membership

	EOD
	echo -----------------------------------------------------------------------------------------------
	echo
	exit
fi

# MonetDB #################################################################

if [ ! -f ~/.monetdb ]; then
	echo "creating ~/.monetdb"
	echo user=monetdb >>~/.monetdb
	echo password=monetdb >>~/.monetdb
	chmod 600 ~/.monetdb
fi

monetdb create AnyDB;
monetdb release AnyDB;

mclient -d AnyDB -e <<EOD

	CREATE USER test WITH PASSWORD 'lemming' NAME 'AnyDB Test User' SCHEMA sys;

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

	CREATE TABLE AnyDB_test
	(
	   column_one   INTEGER          NOT NULL,
	   column_two   VARCHAR(80)      NOT NULL,
	   column_three TIMESTAMP        NOT NULL,
	   column_four  DOUBLE PRECISION NOT NULL,
	   PRIMARY KEY (column_one),
	   FOREIGN KEY (column_two) REFERENCES AnyDB_numbers
	-- UNIQUE      (column_two),      -- has bugs
	-- CHECK       (column_four > 0)  -- not recognised
	);

	CREATE FUNCTION AnyDB_testProcedure1(iKey INTEGER,
	                                     fAdd DOUBLE PRECISION)
	RETURNS DOUBLE PRECISION 
	BEGIN

	   RETURN SELECT column_four + fAdd
	          FROM   AnyDB_test
	          WHERE  column_one = iKey;

	END;

	CREATE FUNCTION AnyDB_testProcedure3()
	RETURNS TABLE (column_one INTEGER,
	               column_two VARCHAR(80),
	               column_three TIMESTAMP,
	               column_four  DOUBLE PRECISION)
	BEGIN
		RETURN SELECT *
	           FROM   AnyDB_test;
	END;

	CREATE FUNCTION AnyDB_testProcedure4()
	RETURNS TABLE (column_one INTEGER,
	               column_two VARCHAR(80),
	               column_three TIMESTAMP,
	               column_four  DOUBLE PRECISION)
	BEGIN
		RETURN SELECT   *
	           FROM     AnyDB_test
	           ORDER BY column_one DESC;
	END;

	GRANT SELECT,INSERT,UPDATE,DELETE ON AnyDB_test TO test;
	GRANT EXECUTE ON FUNCTION AnyDB_testProcedure1 TO test;
	GRANT EXECUTE ON FUNCTION AnyDB_testProcedure3 TO test;
	GRANT EXECUTE ON FUNCTION AnyDB_testProcedure4 TO test;

EOD
