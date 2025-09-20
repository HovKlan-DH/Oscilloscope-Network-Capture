# Oscilloscope Network Capture

"_ONC_" is a simple oscilloscope network capture application, which can communicate with your oscilloscope over the network (not via USB). The tool has been developed for own usage, but as it uses a standard SCPI socket protocol, then it it _should_ work on other oscilloscopes also, but I would be happy to know if people with either confirm "_this is working on my scope model XYZ_" or ideally help me reporting this as an error to me.

My use-case is that I need to do quite a few oscilloscope baseline measurements for my other project, [Commodore Repair Toolbox](https://github.com/HovKlan-DH/Commodore-Retro-Toolbox), which is a tool used for troubleshooting retro/vintage Commodore 64 and 128 etc. Then I will capture one IC at a time, and using _ONC_ is considerable faster than capturing this to a USB-pen, transfer it to local PC and rename files (have tried that - no fun in that).

# First basic usage

- Check that it can detect your oscilloscope - it will do that at application launch, but if it fails, then make sure the IP address is correct of your scope and do "Check oscilloscope connectivity" to validate it works.
- Click the button "Capture continuesly" which will go in "capture mode".
- Watch your scope and try and use the keyboard commands:
  - `+` to decrease timespan (zoom-in)
  - `-` to increase timespan (zoom-out)
  - `*` to STOP acquisition on scope
  - `*` again to take a new snapshot on scope
  - `/` to RESUME acquisition on scope
  - `ARROW UP` to raise trigger level 0.25V
  - `ARROW DOWN` to lower trigger level 0.25V
  - `ENTER` to save image from scope to a file named from the filename format
  - `ESCAPE`to exit capture mode
- When in capture mode, then try and change some of the capturing variables - e.g. change number from `1` to `5`, and see now that the saved file will use this number for the next file you save

# Typical ports used (I assume, but not fully sure)

* Rigol
  - Typical port is `5555` 
* Siglent
  - Typical port is `5025` 

# Requirements

* Windows 10 or newer (64-bit)
* .NET Framework 4.8.1

# Confirmed working on following oscilloscopes

* Rigol DS2202A
* Siglent SDS 1104X - E
* Siglent SDS 1204X - E

# Screenshots

<img width="900" height="723" alt="image" src="https://github.com/user-attachments/assets/20df2f78-f5d8-4817-b9be-51d31f5e420f" />

<img width="900" height="723" alt="image" src="https://github.com/user-attachments/assets/3ad578ca-19a1-4b49-a7cf-61d9fa85cd46" />

<img width="900" height="723" alt="image" src="https://github.com/user-attachments/assets/91a7afa2-9403-45b2-bb6e-b8fe07c6a376" />
