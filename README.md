# Oscilloscope Network Capture

_ONC_ is a simple oscilloscope network capture application, which can communicate with your oscilloscope over the network (not via USB) and standard SCPI commands. It uses the SCPI socket protocol.

You can easily grab images from the scope, and save them as specifically named files on your local computer.


# First basic usage

- **Configuration**
  - Make sure you can connect to the IP address and port defined. Your port settings could be different, if not using standard port.
  - Run all tests to validate your scope can accept the commands given.
- **Capturing**
  - When you can connect to the scope, you can start capturing with `ENTER`.
    - Capturing will only work in the two tabs _Capturing_ and _Debug_.
- **Debug**
  - Check the _Debug_ tab for exact SCPI commands sent to the scope or errors.

View _Help_ tab for more help.


# Variables
_Variables_ are something that can be used for the filename. E.g. the `{NUMBER}` is a sequential number for every capture you do, or it could be the `{DATE}` and `{TIME}`.
You can also create custom variables. E.g. if you are capturing something specifically for the NTSC region (I am from the world of Commodore 64, where this is a thing), then you can create a variable named `{REGION}`, and then have its value set to `NTSC`. This this variable can be used within the filename.


# Keyboard actions
The important thing when measuring on an oscilloscope is to focus on getting the right signal, and then it can be annoying or hard to fiddle with the various knobs or dials on the scope in the same time, so to make it a little easier (for some things), then there are some keyboard actions that can be done:

- `ENTER` to save image from scope to a file, and named from the filename format
- `+` to decrease timespan (zoom-in)
- `-` to increase timespan (zoom-out)
- `*` to put scope in SINGLE mode (basically to catch a new image)
- `/` to put scope in RUN mode (resume acquisition)
- `ARROW UP` to raise trigger level
- `ARROW DOWN` to lower trigger level

The keyboard actions were implemented to support using an external numpad (or keyboard) placed near your measurement bench - maybe with something like this, which I find works like a dream for me:

<img width="502" height="425" alt="image" src="https://github.com/user-attachments/assets/03fbb77f-1e3b-404e-914b-6f39d02e0f03" />


# Requirements

* Windows 7 or newer (both 32/64-bit)
* .NET Framework 4.8


# Supported scopes

- **Keysight/Agilent**
  - InfiniiVision 2000 X 
  - InfiniiVision 6000 X
- **Rigol**
  - DHO1000
  - DHO4000
  - DS1000Z
  - DS2000A
  - MSO1000Z
  - MSO2000A
- **Rohde & Schwarz**
  - MXO 4
  - RTA4000
- **Siglent**
  - SDS800X HD
  - SDS1000DL+
  - SDS1000CML+
  - SDS1000CNL+
  - SDS1000X
  - SDS1000X+
  - SDS1000X-E
  - SDS1000X HD
  - SDS2000X
  - SDS2000X HD
  - SDS2000X Plus
  - SDS3000X HD
  - SDS5000X
  - SDS6000 Pro
  - SDS6000A
  - SDS6000L
  - SDS7000A
  - SHS800X
  - SHS1000X
 
If you can help me getting support for other scopes, I would be grateful.
If so, then please follow this checklist:

- Launch application and make sure you can connect to the scope
- Check which are the required SCPI commands to use
- Make sure you can test everything successfully from the _Configuration_ tab
- Goto _Debug_ tab and send the debug info and configuration file to the developer via the button


# Screenshots


<img width="900" height="602" alt="image" src="https://github.com/user-attachments/assets/6955586a-8e9a-4291-bddd-4a380eacddd9" />


<img width="900" height="602" alt="image" src="https://github.com/user-attachments/assets/ff684323-9427-4fde-92c7-81b92098757b" />


<img width="900" height="602" alt="image" src="https://github.com/user-attachments/assets/2c062af0-63f9-4b6b-86e7-1434132c9711" />


<img width="900" height="602" alt="image" src="https://github.com/user-attachments/assets/d18d213e-e5bd-4ff7-b68e-b2d251349aeb" />


<img width="900" height="602" alt="image" src="https://github.com/user-attachments/assets/1c4dd614-2260-4c15-9033-697567122789" />


<img width="900" height="602" alt="image" src="https://github.com/user-attachments/assets/f68bb338-1567-4e71-9448-9d7f612e6fb0" />


<img width="900" height="602" alt="image" src="https://github.com/user-attachments/assets/57710b2c-e685-43b4-96b0-a13b1ea6aa01" />
