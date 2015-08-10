#!/bin/sh

openssl req -config monkey.conf -nodes -days 3650 -newkey rsa:4096 -keyout monkey.key -out monkey.req -sha256
openssl ca -batch -config openssl.conf -extfile monkey.conf -extensions client_exts -cert Hamiller-Tube-CA.pem -keyfile Hamiller-Tube-CA.key -key monkey -out monkey.pem -days 3650 -in monkey.req
openssl pkcs12 -export -passout pass:monkey -out monkey.pfx -inkey monkey.key -in monkey.pem
openssl x509 -in monkey.pem -text > monkey.cert
openssl rsa -in monkey.key -text >> monkey.cert
