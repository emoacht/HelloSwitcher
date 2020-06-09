# Hello Switcher

Hello Switcher is a Windows desktop tool to help switching Windows Hello cameras.

If your PC has a built-in Windows Hello camera like a Surface series and you wish to add a USB web camera which has Windows Hello capability* for using Windows Hello authentication even when the built-in camera cannot be used, you will not be able to switch the two cameras as you might expect. It is because Microsoft has not added the functionality to manage multiple Windows Hello cameras to Windows 10 yet.

This tool helps switching between a built-in camera and a USB camera. It works as follows:

- If a specifiled USB camera is attached to your PC, this tool will __disable__ a specified built-in camera so that the USB camera is used for Windows Hello authentication.
- If the USB camera is dettached, this tool will __enable__ the built-in camera so that it will be used for the authentication again.

This tool internally calls [DevCon](https://docs.microsoft.com/en-us/windows-hardware/drivers/devtest/devcon), a command-line tool for managing devices, with administrator privilege to enable/disable a built-in camera.

(* USB cameras with Windows Hello capability are limited in the market: Mouse Computer CM02; Logitech Brio Webcam; and Lenovo 500 FHD Webcam.)

## Requirements

 * Windows 10
 * .NET Framework 4.8
 * Devcon.exe

## Download

:floppy_disk: [Executable](https://github.com/emoacht/HelloSwitcher/releases/download/1.0.0/HelloSwitcher100.zip)

## Getting started

1. Get __Devcon.exe__. It is included in Windows Driver Kit (WDK). To download it only, see [this thread](https://superuser.com/questions/1002950/quick-method-to-install-devcon-exe).

2. Place __Devcon.exe__ in the folder where this tool's executable exists.

3. Get device IDs of Windows Hello cameras by either of the following:

- Open Device Manager -> in `Cameras` node, find Windows Hello cameras -> open properties -> select `Details` -> in `Property` dropdown list, select `Hardware ID` -> copy the values.
- Execute __Devcon.exe__ with `hwids =camera` arguments.

4. Open camera.txt included in this tool's package and replace the following values:

- `BuiltinCameraId` value -> built-in camera's device ID
- `UsbCameraId` value -> USB camera's device ID

5. If you wish to run this tool from startup, register it in Task Scheduler.

- In `Gereral`, check `Run with highest privileges`.
- In `Conditions`, uncheck __both__ `Start the task only if the computer is on AC power` and `Stop if the computer switches to battery power`.
- In `Settings`, uncheck `Stop the task if it runs longer than:`.

6. Once started, this tool shows its icon in notification area indicating the existence of the specified USB camera and offers some optional commands in right-click menu.

## Remarks

Since this tool runs after OS's sign-in, it cannot change at sign-in, enabled/disabled state of the specified built-in camera at last sign-out.

## History

Ver 1.0.0 2020-6-9

 - Initial release

## License

 - MIT License

## Developer

 - emoacht (emotom[atmark]pobox.com)
