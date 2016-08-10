#!/bin/sh

openssl req -config Hamiller-Tube-IM.conf -new -newkey rsa:4096 -sha256 -days 3650 -keyout Hamiller-Tube-IM.key -passout pass:monkey -out Hamiller-Tube-IM.req
openssl ca -batch -config openssl.conf -cert Hamiller-Tube-CA.pem -keyfile Hamiller-Tube-CA.key -key monkey -extfile Hamiller-Tube-IM.conf -extensions ca_exts -out Hamiller-Tube-IM.pem -in Hamiller-Tube-IM.req
cat Hamiller-Tube-IM.pem Hamiller-Tube-CA.pem > Hamiller-Tube-IM-and-CA.pem
