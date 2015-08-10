#!/bin/sh

echo '100001' > serial
cp /dev/null certindex.txt
rm certs/*

openssl req -config Hamiller-Tube-CA.conf -new -newkey rsa:4096 -x509 -sha256 -days 3650 -keyout Hamiller-Tube-CA.key -passout pass:monkey -out Hamiller-Tube-CA.pem 

