# Oscilloscope Network Capture

_ONC_ is a simple oscilloscope network capture application, which can communicate with your oscilloscope over the network (not via USB) and standard SCPI commands. It uses the SCPI socket protocol.

You can easily grab images from the scope, and save them as specifically named files on your local computer.


# First basic usage

- **Configuration**
  - Make sure you can connect to the IP address and port defined. Your port settings could be different, if not using standard port.
  - Run all tests to validate your scope can accept the commands given.
- **Capturing**
  - When you can connect to the scope, you can start capturing.
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
  - InfiniiVision 2000 X-Series
- **Rigol**
  - DS1000Z/MSO1000Z Series
  - DS2000A/MSO2000A Series
- **Rohde & Schwarz**
  - MXO 4 Series
- **Siglent**
  - SDS1000/1000X Series
  - SDS2000/2000X Series
 
If you can help me getting support for other scopes, I would be grateful.
If so, then please follow this checklist:

- Launch application and make sure you can connect to the scope
- Check which are the required SCPI commands to use
- Make sure you can test everything successfully from the _Configuration_ tab
- Goto _Debug_ tab and send the debug info and configuration file to the developer via the button


# Screenshots

<img width="900" height="602" alt="image" src="https://github.com/user-attachments/assets/b32ab37b-8253-4e77-93a9-15df490b8720" />

<img width="900" height="602" alt="image" src="https://github.com/user-attachments/assets/d0775c06-84e9-4aa1-81d7-2aa4721694bd" />

<img width="900" height="602" alt="image" src="https://github.com/user-attachments/assets/f141f912-9bf3-4905-ac5b-b5375aaac5b9" />

<img width="900" height="602" alt="image" src="https://github.com/user-attachments/assets/07146834-9646-426b-94e5-081207c9a42f" />

<img width="900" height="602" alt="image" src="https://github.com/user-attachments/assets/88254776-1931-432a-8fba-f5a656d7eea4" />

<img width="900" height="602" alt="image" src="https://github.com/user-attachments/assets/d436658b-eca2-4c95-911f-8be240dfa705" />
