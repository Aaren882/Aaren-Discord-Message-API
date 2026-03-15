module.exports = {
  apps : [{
    name   : "Arma3WebService",
    script : "Arma3WebService.exe",
    cwd    : "./",
    env: {
      ASPNETCORE_ENVIRONMENT: "Production",
      ASPNETCORE_HTTPS_PORTS: 7172,
      ASPNETCORE_HTTP_PORTS: 5048,
    }
  }]
}
