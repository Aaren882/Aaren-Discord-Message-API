module.exports = {
  apps : [{
    name   : "Arma3WebService",
    script : "Arma3WebService.exe",
    cwd    : "./",
    env: {
      ASPNETCORE_ENVIRONMENT: "Production",
      // ASPNETCORE_HTTPS_PORTS: 7172,
      ASPNETCORE_HTTP_PORTS: 5048,
      
      DB_PROVIDER: "SQLite", //- "SQLite", "MySQL", "NpgSQL"
      //- SQLite
      DB_CONNECTION_STRING: "Data Source=data.db",
      //- MySQL (MySQL, mariaDB)
      // DB_CONNECTION_STRING: "Server=localhost;Port=3306;Database=test;User=root;Password=example;",
      //- PostgreSQL (NpgSQL)
      // "DB_CONNECTION_STRING": "Host=localhost;Port=5432;Database=test;Username=postgres;Password=example",
      
      BotToken: "BOT_TOKEN",
      MonitorChannel: "ChannelID",
      AdminChannel: "ChannelID",
      LoggingChannel: "ChannelID",
      AdminLoggingChannel: "ChannelID", 
      AdminPassword: "in game AdminPassword ", //- (Optional but some remote functions won't be working)
    }
  }]
}
