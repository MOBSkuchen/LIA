# LIA - Language Intermediate Assemblable
LIA is a low level hybrid typed object-oriented programming lanuage which compiles to CIL to run on the .NET framework.
Support for compiling to Java Bytecode is being worked on.
## Why?
I don't know. It's fun I guess.
## What can this Language do, that mine can't?
Nothing. At least in this stage of development, LIA has a lot less functionality than most programming languages.
But the main selling point of this language is that it can compile to CIL and in the future hopefully to Java Bytecode,
which can both be cross compiled into other languages or used as a drag-and-drop replacement.
# Tutorial
## Namespaces, Classes and Functions
### Simplest program
````
#*
Simple Program,
exits with code 0
*#

namespace test

class public Program:
    def public i32 main:        # Main method, entry point 
        return 0                # Exit-code 0
    ;
;
````
Please note that the namespace declaration is not needed.

This is quite similar to C#. One Program can also have multiple **namespaces** and **namespaces** can also have multiple **classes**, which can of course have multiple **functions**.
**functions** and **classes** can be declared as *private* or *public*.