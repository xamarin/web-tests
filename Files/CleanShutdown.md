Clean Shutdown in TLS 1.2
=========================

According to the [TLS 1.2 Spec](https://tools.ietf.org/html/rfc4346#section-7.2.1),
a close_notify alert should be send prior to closing the connection.

Originally added as a way to prevent truncation attacks, it is no longer necessary
when the application level protocol (such as HTTP 1.0+) has a way of tracking
connection closure.

The reality is that a large amount of HTTP browsers and servers either don't send
the close_notify alert or don't send a close_notify reply upon receiving it.

This [article](https://security.stackexchange.com/questions/82028/ssl-tls-is-a-server-always-required-to-respond-to-a-close-notify)
has some interesting details.

For HTTP, we don't need it because the protocol has a reliable way of detecting the
end of a connection.  The only situation where it could be useful is when you're trying
to mix TLS and unencrypted traffic on the same connection.

When using AppleTls (which is based on Apple's SecureTransport), there is a problem
with the way they implement SSLClose():

Calling SSLClose() sends the close_notify alert, then sets and internal flag which marks
the connection as closed for both reading and writing.  The next time you call SSLRead(),
it'll immediately return telling you the connection has been closed gracefully.

This means - should the remote reply by sending a close_notify back - there's no way
of reading it, thus leaving you with some blob of encrypted data.

Therefor, we do not send any close_notify alerts anymore by default, but there is an
internal API to explicitly enable them when using BTLS.

Implementation in Mono
======================

The code has now landed in mono/master ([PR #5465](https://github.com/mono/mono/pull/5465)).

There is a new internal property `MonoTlsSettings.SendCloseNotify`, which can be used to
send the close_notify when using BTLS.

I also submitted a bug report to Apple:
[rdar://34167402](https://openradar.appspot.com/34167402)

