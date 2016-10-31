# RunJ
A handy tool to maximize my efficiency by replacing Win+R


# Usage
After compiling (or simply download it [here](https://github.com/AnoXDD/RunJ/raw/master/RunJ/bin/Release/RunJ.exe)) the .exe file. Make sure you put it in a separate folder as it will generate another config file once you start to set it up.

## First time set up

* Run it and put `$$` and then press `Enter`
* Go to the directory where this program is placed, find the config file generated (called `command.csv`)
* Open it and modify it if necessary by following the manual inside the file

## Modify the config file

Each line of the config file represents a single relationship of Shortcut and Command, in the format of `Shortcut,Command`. For example, in `task,taskmgr`, Shortcut is `task`, and Command is `taskmgr`.

The program will match the content you put in the program with Shortcut, and execute Command if it is a match.

The program will run the first command it matches. If you have multiple lines with the same Shortcut, it will run the Command whose line number is smaller.

### Advanced usage

You will see the advanced manual in the `command.csv` you just generated:

* Use comma to separate a shortcut and the command them, in the format of: `shortcut,command` (which we just mentioned),
  * E.g. If you have `task,taskmgr` in this file, put `task` and enter will open a task manager
* Use "#" to start a new comment line, which the program will ignore the content behind it
* Use "!" to start a Command to pop up a window with content followed
  * Useful if you need to reference something but don't want to memorize it
  * E.g. If you have `hello,!helloworld` in this file, put `hello` will pop up a window says helloworld
  * Use "\n" to start a newline
* Use {0}, {1}, ... , {4} to match the command. 
  * E.g. for the command `c {0} {1},cmd {0} {1}`,
    it will match `c 2 3` and execute `cmd 2 3`
  * NOTE: in `shortcut` {0} must be present before {1}, {2}, ..., and {1} before {2}, ..., etc. 
* For other control over the app, enter `$h` to show the debug window",

The program accepts empty command. You can make use of this feature by binding your favorite command. For example if you use Chrome a lot, you can put `,chrome.exe` in the config file so everytime you wake up the program you can open a new Chrome by simply hitting `Enter`.

## Using this program

You can see the current time and date (and little version number at the right bottom) in the app. The time doesn't update automatically (because it doesn't have too), so to update it you need to hide and show again. 

### Execute the command
This program only has one box for user input so you can't miss it. To execute the command you just entered, simply press `Enter` and the program will hide itself while trying to execute the command you just put. If the program can't find it, it will try to run the system command (same as you run Win+R and put the command in that box). 

If nothing happens, then that probably means there is something wrong in your input and/or your config file. If you believe it is not your fault, please [submit an issue](https://github.com/AnoXDD/RunJ/issues).

### Wake up the program
The program will hide itself after each command (no matter if it's a success). To open the window again press `Alt+Ctrl+Q`.

This shortcut key cannot be changed so far. If you would like to customize it, please [submit an issue](https://github.com/AnoXDD/RunJ/issues) and if enough people are proposing it, I'll implement it. 

### Use Google search
You will see some loaded results under the command you put in the program, and that's powered by Google autocomplete. If you would like to do a Google search on the command you put, press `Ctrl+Enter` to start a Google search.

If you'd like to quick choose the suggested result, use `Tab` to select the first command, or use Arrow Keys to navigate and use `Tab` to choose the suggested result. Once `Tab` is pressed, `Enter` will start a Google search, but only if you don't press any other keys. 

# Off-topic

## Background
I really do not like it when Microsoft takes down Charm, a tiny box that displays current time and date, in Windows 10, which forced me to unhide my taskbar just in order to know the time and date (I hide my taskbar 99% of all the time because I don't really use it).

Then later I read that Chrome is going to take down their App Launcher, another handy app for me launch website or Chrome app quickly from desktop. Now two of my favorites are about to be gone. It's time for me to do something. 

So that was what made me to write this tiny application that combines the functionality of Charm (time and date), Chrome App Launcher and Win+R (Run). To make it more customizable, the user can modify an external configuration file to instruct it what to do given some inputs. 


## Misc. 

In the early phase of this app it was called MyCalculatorv1 simply because I modified the code based on another project
