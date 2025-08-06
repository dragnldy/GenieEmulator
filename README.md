Dev in progress.  Need to add tool to capture the raw data from Genie etc.  
  Server emulator that will feed captured raw data logs back to a Genie or Genie type client.  
Mostly used for testing and develoment but also handy for replaying favorite sessions, sharing fights with others, etc.  
The associated client emulator can be used to test the server emulator.  
Optional command line arguments can be used to set host, port and file to feed to the client  
host=127.0.0.1  
port=8888  
source=testbrief.txt  
client=local  
  
127.0.0.1 is the local loopback address and is the default  
8888 is the default listening port  
source and client only used for internal test procedures- otherwise don't include   
Should be compatible with Windows, Linux, and MacOS but only tested in Windows.
