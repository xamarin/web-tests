#!/bin/sh

openssl req -x509 -config penguin.conf -nodes -days 3650 -newkey rsa:4096 -out penguin.pem -keyout penguin.key -extensions client_exts
openssl pkcs12 -export -passout pass:penguin -out penguin.pfx -inkey penguin.key -in penguin.pem
openssl x509 -in penguin.pem > penguin.cert
openssl rsa -in penguin.key >> penguin.cert

