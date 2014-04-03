var http = require('http');
var crypto = require('crypto')

http.createServer(function (req, res) {
  console.log("port: " + req.socket.remotePort);
  var d = "" + req.socket.remotePort;
  res.writeHead(200, {'Content-Type': 'application/json', 'Content-Length':"" + d.length});
  res.end(d);
}).listen(9615);