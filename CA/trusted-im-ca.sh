#!/bin/sh

openssl req -config trusted-im-ca.conf -new -newkey rsa:4096 -sha512 -days 3650 -keyout trusted-im-ca.key -passout pass:monkey -out trusted-im-ca.req
openssl ca -batch -config openssl.conf -cert Hamiller-Tube-CA.pem -keyfile Hamiller-Tube-CA.key -key monkey -extfile trusted-im-ca.conf -extensions trusted_im_ca_exts -out trusted-im-ca.pem -in trusted-im-ca.req

openssl pkcs12 -export -passin pass:monkey -passout pass:monkey -out trusted-im-ca.pfx -inkey trusted-im-ca.key -in trusted-im-ca.pem

openssl x509 -in trusted-im-ca.pem -text > trusted-im-ca.cert
openssl rsa -in trusted-im-ca.key -passin pass:monkey -text >> trusted-im-ca.cert

