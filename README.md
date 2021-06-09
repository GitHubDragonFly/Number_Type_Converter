# Number Conversion

Windows app to convert from and to different number systems.

This is the best-effort attempt at number conversion up to 128 bits.

Signed & Unsigned Integers, Single, Double, Binary, Octal, Hex values.

An open mind and some knowledge of number systems, hopefully binary, is required in order to understand the displayed values.

# Functionality
All the displayed values revolve around the binary representation and/or integer promotion/demotion, with the following logic being used:

- 128-bit = 2 x 64-bit = 4 x 32-bit = 8 x 16-bit = 16 x 8-bit ... 64-bit = 2 x 32-bit = 4 x 16-bit = 8 x 8-bit ... etc
- Any number will be looked at with the required number of bits for the data type of the display:
 - For example, if you select Int64 then the binary will show 64 bits regardless of what valid number from the 64-bit range you enter
 - All other data types that have that same number within their own range will then display it as such
- All the data types that can represent the value will have it be displayed directly, otherwise multiple values of that data type will be displayed
- The required number of bits will be achieved by either splitting the original bits into groups or extending it while observing the sign
- The weight of all the displayed values is: Hi <----> Lo.
- Floating-point binary format is: Sign bit - Exponent bits - Fraction bits (32-bit = 1-8-23 ; 64-bit = 1-11-52)
- Floating-point equivalent integer representation is packed into the BigInteger number
- Floating-point equivalent integer representation observes only the Integral part and disregards the Fractional part of the number

# Build
All it takes is to:

- Download and install Visual Studio community edition (ideally 2019).
- Download and extract the zip file of this project.
- Open this as an existing project in Visual Studio and, on the menu, do:
  - Build/Build Solution (or press Ctrl-Shift-B).
  - Debug/Start Debugging (or press F5) to run the app.
- Locate created EXE file in the /bin/Debug folder and copy it over to your preferred folder or Desktop.

# Licensing
This is licensed under MIT License.

# Trademarks
Any and all trademarks, either directly or indirectly mentioned in this project, belong to their respective owners.
