module.exports = {
  apps : [{
    name   : "Arma3WebService",
    script : "Arma3WebService.exe",
    cwd    : "./",
    env: {
      ASPNETCORE_ENVIRONMENT: "Production",
      ASPNETCORE_HTTPS_PORTS: 7172,
      ASPNETCORE_HTTP_PORTS: 5048,
      BotToken: "BOT_TOKEN",
      MonitorChannel: "ChannelID",
      AdminChannel: "ChannelID",
      LoggingChannel: "ChannelID",
      AdminLoggingChannel: "ChannelID", 
      AdminPassword: "in game AdminPassword ", //- (Optional but some remote functions won't be working)
    }
  }]
}
