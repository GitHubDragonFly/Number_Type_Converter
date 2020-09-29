# NumberConversion - Windows app to convert from and to different number systems.

Best-effort attempt at number conversion up to 128 bits.

Signed & Unsigned Integers, Single, Double, Binary, Octal, Hex values.

All the displayed values revolve around the binary representation and/or integer promotion, with the following logic being used:

- 128-bit = 2 x 64-bit = 4 x 32-bit = 8 x 16-bit = 16 x 8-bit ... 64-bit = 2 x 32-bit = 4 x 16-bit = 8 x 8-bit ... etc
- Any number will be looked at with the required number of bits for the data type of the display
- All the data types that can represent the value will be displayed directly, otherwise multiple values will be displayed
- The required number of bits will be achieved by either splitting the original bits into groups or extending it while observing the sign
- The weight of all the displayed values is either of: (Hi <--- Lo) or (Hi ----> Lo)
- Floating-point binary format is: Sign bit - Exponent bits - Fraction bits (32-bit = 1-8-23 ; 64-bit = 1-11-52)
- Floating-point equivalent integer representation is packed into the BigInteger number
- Floating-point equivalent integer representation observes only the Integral part and disregards the Fractional part of the number