var express = require('express')
  , http = require('http')
  , path = require('path');

var app = express();

app.configure(function(){
  app.set('port', process.env.PORT || 5000);
  app.set('views', __dirname + '/views');
  app.set('view engine', 'ejs');
  app.use(express.favicon());
  app.use(express.logger('dev'));
  app.use(express.bodyParser());
  app.use(express.methodOverride());
  app.use(app.router);
  app.use(express.static(path.join(__dirname, 'public')));
});

app.configure('development', function(){
  app.use(express.errorHandler());
});

var httpServer = http.createServer(app);
httpServer.listen(app.get('port'), function() {
  console.log("Express httpServer listening on port " + app.get('port'));
});

var util = require('util');
var WebSocketServer = require('ws').Server
  , wss = new WebSocketServer({ 'server': httpServer });

wss.broadcast = function(data) {
  for (var i in this.clients) {
    this.clients[i].send(data);
  }
};

var connectionId = 0;
wss.on('connection', function(ws) {
  console.log("connection!");
  connectionId++; // 本当はロックかけないといけない
  ws.send(JSON.stringify({ command: 'connect', connectionId: connectionId }));

  ws.on('message', function(message) {
    console.log('connectionId = %d, message = %s', connectionId, message);
    var obj = JSON.parse(message);
    if (obj.command === 'create') {
      var id = obj.connectionId;
      var type = id % 2 === 0 ? 'ball' : 'cube';
      ws.send(JSON.stringify({ 'command': 'create', 'type': type }));
      var drawData = {};
      for (var i = 1; i <= connectionId; i++) {
        //drawData.push({ connectionId: i, type: i % 2 === 0 ? 'ball' : 'cube'});
        drawData[i] = i % 2 === 0 ? 'ball' : 'cube';
      }
      // 他のプレーヤーのデータ
      wss.broadcast(JSON.stringify({ command: 'draw', 'data': drawData }));
    }
  });
});
