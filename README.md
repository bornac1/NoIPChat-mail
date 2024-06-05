# NoIPChat mail

NoIPChat mail plugin enables usage of regular email clients via SMTP and POP3 in NoIPChat network.

**Installation**

To install using default configuration, just place NoIPChat mail.nip into the Plugins folder of the Server.

Once plugin is unpacked, close the Server if any changes are needed in configuration.

**Configuration**

Plugin is configured through Config.php

Inside both SMTP and POP3 tags there should be at least one Interface.

InterfaceIP is IP address assigned to network interface (in case of NAT, this is private IP)

Port is a port number (port should be opend in firewall)