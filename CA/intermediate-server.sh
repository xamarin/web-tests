#!/bin/sh

openssl req -config intermediate-server.conf -days 3650 -newkey rsa:4096 -keyout intermediate-server.key -out intermediate-server.req

openssl ca -batch -config openssl.conf -cert intermediate-ca.pem -keyfile intermediate-ca.key -key monkey -extfile intermediate-server.conf -extensions intermediate_server_exts -out intermediate-server.pem -in intermediate-server.req

openssl pkcs12 -export -passin pass:monkey -passout pass:monkey -out intermediate-server.pfx -inkey intermediate-server.key -in intermediate-server.pem

openssl x509 -in intermediate-server.pem -text > intermediate-server.cert
openssl rsa -in intermediate-server.key -passin pass:monkey -text >> intermediate-server.cert 
