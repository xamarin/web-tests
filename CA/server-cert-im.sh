#!/bin/sh

openssl req -config server-cert-im.conf -days 3650 -newkey rsa:4096 -keyout server-cert-im.key -out server-cert-im.req
openssl ca -batch -config openssl.conf -cert Hamiller-Tube-IM.pem -keyfile Hamiller-Tube-IM.key -key monkey -extfile server-cert-im.conf -extensions server_exts -out server-cert-im.pem -in server-cert-im.req

openssl pkcs12 -export -passout pass:monkey -out server-cert-im-bare.pfx -inkey server-cert-im.key -in server-cert-im.pem
openssl pkcs12 -export -passout pass:monkey -out server-cert-im.pfx -inkey server-cert-im.key -in server-cert-im.pem -certfile Hamiller-Tube-IM.pem
openssl pkcs12 -export -passout pass:monkey -out server-cert-im-full.pfx -inkey server-cert-im.key -in server-cert-im.pem -certfile Hamiller-Tube-IM-and-CA.pem

