#!/bin/sh

openssl req -config server-cert.conf -days 3650 -newkey rsa:4096 -keyout server-cert.key -out server-cert.req

openssl ca -batch -config openssl.conf -cert Hamiller-Tube-CA.pem -keyfile Hamiller-Tube-CA.key -key monkey -extfile server-cert.conf -extensions server_exts -out server-cert.pem -in server-cert.req 

openssl pkcs12 -export -passout pass:monkey -out server-cert.pfx -inkey server-cert.key -in server-cert.pem

openssl x509 -in server-cert.pem -text > server-cert.cert
openssl rsa -in server-cert.key -text >> server-cert.cert 
