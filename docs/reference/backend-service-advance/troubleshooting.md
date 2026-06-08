# 🔍 Troubleshooting

#### 1. Connection Refused (Port 5048)

* Ensure your VPS/Cloud provider's hardware firewall allows the TCP port.

#### 2. Bot Not Responding

* Check the console or logs for `Discord.Net` exceptions.
* Ensure the Bot has "Message Content Intent" enabled in the Discord Developer Portal.

#### 3. High Memory Usage

If memory climbs significantly:

* Reduce the frequency of status updates in the Arma 3 CBA settings (default is usually 300s).
* Check the `LOG_LEVEL`; setting it to `Debug` or `Trace` in production can create massive log files and overhead.
