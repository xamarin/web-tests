#!/bin/sh

openssl req -config server-cert-trusted-im.conf -days 3650 -newkey rsa:4096 -keyout server-cert-trusted-im.key -out server-cert-trusted-im.req
openssl ca -batch -config openssl.conf -cert trusted-im-ca.pem -keyfile trusted-im-ca.key -key monkey -extfile server-cert-trusted-im.conf -extensions server_exts -out server-cert-trusted-im.pem -in server-cert-trusted-im.req

openssl pkcs12 -export -passout pass:monkey -out server-cert-trusted-im-bare.pfx -inkey server-cert-trusted-im.key -in server-cert-trusted-im.pem
openssl pkcs12 -export -passout pass:monkey -out server-cert-trusted-im.pfx -inkey server-cert-trusted-im.key -in server-cert-trusted-im.pem -certfile server-cert-trusted-im.pem

