# Dark Skin for Unity

[![Build Status](https://travis-ci.com/mukaschultze/unity-dark-skin.svg)](https://travis-ci.com/mukaschultze/unity-dark-skin)
[![Latest release](https://img.shields.io/github/v/release/mukaschultze/unity-dark-skin)](https://github.com/mukaschultze/unity-dark-skin/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/mukaschultze/unity-dark-skin/total)](https://github.com/mukaschultze/unity-dark-skin/releases/latest)

**[Download latest release](https://github.com/mukaschultze/unity-dark-skin/releases/latest)**

## How to use

Just place the executable on the same folder where your unity installations are and run `dark-skin enable` from the command line, the tool will search for executables and patch them. **Close all the editor instances before running this tool.**

The tool will automatically backup and restore the .exe if any errors occur during the patch process. The backuped .exe will be kept at the editor folder if you need to restore it manually later on.

## How to find hex sequence

- `cvdump.exe -headers -p UNITY_FOLDER\Editor\Unity.exe > dump.txt`
- Open `dump.txt` and search for `GetSkinIdx`
- `S_PUB32: [0001:00ABD9C0], Flags: 00000002, ?GetSkinIdx@EditorResources@@QEBAHXZ`
- Function address is virtual addr + section raw pointer
- Ex: 0x00ABD9C0 + 0x400

OR

- `dark-skin findHex UNITY_FOLDER\Editor\Unity.exe`

Then

- https://defuse.ca/online-x86-assembler.htm#disassembly2

### Assembly - Unity 2019.2.0a9

```assembly
b3: 90                      nop
b4: 84 db                   test   bl,bl
b6: 75 04                   jne    0xbc
b8: 33 c0                   xor    eax,eax
ba: eb 02                   jmp    0xbe
bc: 8b 07                   mov    eax,DWORD PTR [rdi]
be: 4c 8d 5c 24 70          lea    r11,[rsp+0x70]
c3: 49 8b 5b 10             mov    rbx,QWORD PTR [r11+0x10]
c7: 49 8b 6b 18             mov    rbp,QWORD PTR [r11+0x18]
cb: 49 8b 73 20             mov    rsi,QWORD PTR [r11+0x20]
cf: 49 8b e3                mov    rsp,r11
d2: 5f                      pop    rdi
d3: c3                      ret
```

### Assembly - Unity 2019.2.0a11

```assembly
42: 80 3d bf d3 8c 06 00    cmp    BYTE PTR [rip+0x68cd3bf],0x0        # 0x68cd408
49: 75 15                   jne    0x60
4b: 33 c0                   xor    eax,eax
4d: eb 13                   jmp    0x62
4f: 90                      nop
50: 49 ff c0                inc    r8
53: 42 80 3c 03 00          cmp    BYTE PTR [rbx+r8*1],0x0
58: 0f 84 84 00 00 00       je     0xe2
5e: eb f0                   jmp    0x50
60: 8b 07                   mov    eax,DWORD PTR [rdi]
62: 4c 8d 5c 24 70          lea    r11,[rsp+0x70]
67: 49 8b 5b 10             mov    rbx,QWORD PTR [r11+0x10]
6b: 49 8b 6b 18             mov    rbp,QWORD PTR [r11+0x18]
6f: 49 8b 73 20             mov    rsi,QWORD PTR [r11+0x20]
73: 49 8b e3                mov    rsp,r11
76: 5f                      pop    rdi
77: c3                      ret
```
