#!/bin/sh

openssl req -config intermediate-ca.conf -new -newkey rsa:4096 -sha512 -days 3650 -keyout intermediate-ca.key -passout pass:monkey -out intermediate-ca.req
openssl ca -batch -config openssl.conf -cert Hamiller-Tube-CA.pem -keyfile Hamiller-Tube-CA.key -key monkey -extfile intermediate-ca.conf -extensions intermediate_ca_exts -out intermediate-ca.pem -in intermediate-ca.req

openssl pkcs12 -export -passin pass:monkey -passout pass:monkey -out intermediate-ca.pfx -inkey intermediate-ca.key -in intermediate-ca.pem

openssl x509 -in intermediate-ca.pem -text > intermediate-ca.cert
openssl rsa -in intermediate-ca.key -passin pass:monkey -text >> intermediate-ca.cert

