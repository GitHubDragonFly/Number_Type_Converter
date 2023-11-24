# Number Conversion

This repository has Windows based app that can be used to convert from and to different number systems or data types.

Online version is available as well:

|    Online Version    | [Webpage](https://githubdragonfly.github.io/viewers/templates/Number%20Conversion.html) |
|         :--          |                                       :--                                               |
| Online Version Code  | [Repository](https://github.com/GitHubDragonFly/GitHubDragonFly.github.io)              |

This is the best-effort attempt at number conversion up to 128 bits.

Supported number or data types are: signed & unsigned integer, floating point, binary, octal and hex numbers with some possible terminology related to these:
- `Int8` `UInt8` `Int16` `UInt16` `Int32` `UInt32` `Int64` `UInt64` `Int128` `UInt128` 
- `SBYTE` `BYTE` `SHORT` `USHORT` `INT` `UINT` `DINT` `UDINT` `LONG` `ULONG`
- `Float32` `Float64` `REAL` `FLOAT` `SINGLE` `DOUBLE` `LREAL`

An open mind and some knowledge of number systems, hopefully binary, will help understand the displayed values.

Possibly of good use to those who are dealing with Programmable Logic Controllers (PLC) - some Modbus logic was used here. All is good as an educational resource.

# Number Conversion App

![Number Conversion App](screenshot/Number%20Conversion.png?raw=true)

# Functionality
All the displayed values revolve around the binary representation and/or integer promotion/demotion, with the following logic being used:

- `128-bit = 2 x 64-bit = 4 x 32-bit = 8 x 16-bit = 16 x 8-bit` ... `64-bit = 2 x 32-bit = 4 x 16-bit = 8 x 8-bit` ... etc
- Signed integers have equivalency for as long as the displayed number exists within their own data type
- Unsigned integers have equivalency only if they represent the same signed integer
- Any number will be looked at with the required number of bits for the selected data type, for example:
  - If you select Int64 then the binary will show 64 bits regardless of what valid number from the 64-bit range you enter
  - All other data types that have that same integer number within their own range will then display it as such
  - All other data types that don't have that same integer number within their own range will then display multiple values of their own data type:
    - each displayed value is representing the number of bytes required for their own data type
    - bytes are extracted from the binary representation of the original number
- The required number of bits will be achieved by either splitting the original bits into groups or extending it while observing the sign
- The weight of all the displayed values is `Hi <----> Lo` and with the following picture helping understand it:

![Number Systems](screenshot/Number%20Systems.png?raw=true)

Floating-point numbers are somewhat special and that's why they are in their own section
- Floating-point binary format is: Sign bit - Exponent bits - Fraction bits (ex.: 32-bit = 1-8-23 bits ; 64-bit = 1-11-52 bits)
- Floating-point equivalent integer representation is packed into the BigInteger number
- Floating-point equivalent integer representation observes only the Integral part and disregards the Fractional part of the number
- If you want to use the `BYTE` logic for floating-point numbers then you will have to perform 2 conversions, here is an example:
  - Select `Float32` data type, enter number "1.2241" and then check its representation under `BinaryF32`, which should show the following:
     - `0 01111111 00111001010111101001111` which is Sign bit - Exponent bits - Fraction bits representation
  - This is 32 bits so it can be looked at as 4 bytes (4 groups of 8 bits)
  - Right click and copy this binary number
  - Switch to `Binary` data type, right click and paste the number, hit `Convert` button or just press Enter
  - Now you can see how those 4 bytes are interpreted by integer data types, ex. `Int8` will show "63, -100, -81, 79" while `Int32` will show "1067233103"
  - Also, the floating-point numbers section now shows a representation of "1067233103" and not the number "1.2241" we started with
 
# Build
All it takes is to:

- Either use the Windows executable file from the `exe` folder or follow the instructions below to build it yourself.
- Download and install Visual Studio community edition (2022 was used for last updates to this project).
- Download and extract the zip file of this project.
- Open this as an existing project in Visual Studio and, on the menu, do:
  - Build/Build Solution (or press Ctrl-Shift-B).
  - Debug/Start Debugging (or press F5) to run the app.
- Locate created EXE file in the /bin/Debug folder and copy it over to your preferred folder or Desktop.

# Licensing
This is licensed under MIT License.

# Trademarks
Any and all trademarks, either directly or indirectly mentioned in this project, belong to their respective owners.
