# Reverse engineering

## UPS interface

Questions to answer:

- [ ] What's the pinout of the card edge connector?
- [ ] What voltage(s) are provided? 5V expected since LT1507 buck regulator is present.
- [ ] What's the physical layer for communications? 3.3V UART seems likely.
- [ ] What's the transport layer? Modbus Serial RTU seems likely since the card was made by Schneider Electric. JBUS is also a possibility since it is mentioned in docs.

## Network-MS Hardware

Network-MS (MiniSlot) cards have two version numbers: Technical Level and Card Revision. The card revision is a unique identifier for each distinct revision of the PCB. The specifics of the technical level are less clear, but it appears to be mostly tied to changes in UPS compatibility. The technical level is also referred to as "NT" (probably due to the French translation of technical level, i.e. "niveau technique") in the documentation.

### Technical Levels

| Level | Associated Card Revisions | Last Compatible Firmware Release | Notes                                                        |
| ----- | ------------------------- | -------------------------------- | ------------------------------------------------------------ |
| 01    |                           | EE                               |                                                              |
| 02    |                           | EE                               |                                                              |
| 03    |                           | EE                               |                                                              |
| 04    |                           | GE                               |                                                              |
| 05    |                           | GE                               |                                                              |
| 06    |                           | GE                               |                                                              |
| 07    |                           | GE                               |                                                              |
| 08    |                           | GE                               |                                                              |
| 09    | FB and GA                 | GE / HF                          | TL09 cards have varying firmware support depending on the card revision. FB cards support up to GE, whereas GA cards support up to HF. |
| 13    | HA                        | HF                               |                                                              |
| 17    | JA                        | JK                               |                                                              |
| 23    | JL                        | ??                               | This info comes [from reddit](https://www.reddit.com/r/homelabsales/comments/nf29su/wuk_eaton_ups_networkms_card_with_specific/gyl3v2z/). Unknown latest release - probably supports all the way up to LE. |

## Network-MS Firmware

The firmware appears to be based on Digi NET+OS. A [firmware header parsing script](../../tools/netos_firmware_header_parser.linq) has been created to extract some information about the firmware update files. The data in the files appears to be compressed or otherwise encoded, though, so I haven't managed to reverse engineer any of the ARM7 stuff out of them yet. The documentation mentions LZSS, LZSS2, and LZ77, but the flags in the header _seem_ to indicate that the data isn't actually compressed. However, I'm making that assumption based on the idea that the `BL_WRITE_TO_FLASH`, `BL_LZSS_COMPRESSED`, `BL_LZSS2_COMPRESSED`, etc. flags are sequential, which probably isn't correct - sadly I can't get hold of the NET+OS source or BSPs to confirm this. Efforts to decompress the payloads has yet to yield any results.

### Firmware files

The release dates here come from release notes. Unfortunately most of these can't be downloaded from anywhere any more.

| Version ID                                                   | Release date             | SHA256 hash                                                  |
| ------------------------------------------------------------ | ------------------------ | ------------------------------------------------------------ |
| AA - [possible file](../3rd_party/network-ms-firmware/nmc_uupload.bin) | Possibly 04/2008         | (?) e8dc697746996be31c95e38a4a15831b95528d46f043f9be9228c8c416033732 |
| BA                                                           |                          |                                                              |
| CA                                                           |                          |                                                              |
| CB                                                           |                          |                                                              |
| DA                                                           |                          |                                                              |
| EA                                                           |                          |                                                              |
| [EB](../3rd_party/network-ms-firmware/nmc_eb.bin)                    |                          | 6ec121d2324e5a1de184b1b1b84ef7a94f7f2c8862331e2eaab10d172192936e |
| [EC](../3rd_party/network-ms-firmware/nmc_ec.bin)                    |                          | 1f4b26d8ea5e192d2a074c3453fe2baee96ec13c8435e63cae7710f9a0faf054 |
| [EE](../3rd_party/network-ms-firmware/nmc_ee.bin)                    |                          | d4257c566128d0a5b7185885927c729d85c30143ca0f0be9a5d7e586bed9b6bc |
| [FA](../3rd_party/network-ms-firmware/nmc_fa.bin)                    |                          | 4e81e177d09eeb9bd0943685983cadef9274869eedc72023cb9d927092fd2b23 |
| [GA](../3rd_party/network-ms-firmware/nmc_ga.bin)                    |                          | 977c548d5071bd62a1789d90c17e167068a1a17e1fd3043aee6b77f97a05ec40 |
| [GB](../3rd_party/network-ms-firmware/nmc_gb.bin)                    |                          | 29e7c9f8d0bd15eb01c178224f1e899f971bcc6241df2305994603f4d357db4b |
| [GC](../3rd_party/network-ms-firmware/nmc_gc.bin)                    |                          | dac400161591559b7fb08aa25e88ce1e80ee94f7bedbda5fe79fd8d243836b73 |
| [GD](../3rd_party/network-ms-firmware/nmc_gd.bin)                    |                          | 97f52c20d080921f03fe2f8e12fbacbb3c278fe9de82bc150e6d11b2f26e1954 |
| [GE](../3rd_party/network-ms-firmware/nmc_ge.bin)                    |                          | d9870ae78f210f30426bca7cb56f9f26c2a97cb2a6b5422e4a516d61d06fdbbe |
| [HA](../3rd_party/network-ms-firmware/nmc_ha.bin)                    |                          | 5afefc88863cf115a286a0eeb5621510eb2dfcc92aca7ff43d404c20cd2e8e03 |
| [HB](../3rd_party/network-ms-firmware/nmc_hb.bin)                    |                          | 9b82b7168d367d72047c3da483e4d93f87e27ac6373c08e72cdeee21e498c8d1 |
| [HC](../3rd_party/network-ms-firmware/nmc_hc.bin)                    |                          | 7440565e38a6c8f8e2782fe1bc9b438d2d9df59049c9a0fe8b4a79f97febb78b |
| [HD](../3rd_party/network-ms-firmware/nmc_hd.bin)                    | 2011-08-31               | 394f2438380f2beec2a48a3716423d90053c82421ac2971c61eb1f2f88d38167 |
| [HE](../3rd_party/network-ms-firmware/nmc_he.bin)                    | 2011-10-06 or 2011-06-10 | 9687d19cfe7bdb84cf74edaa7dc4bd1de2466c65d58ed386ed076b7b77856e0a |
| [HF](../3rd_party/network-ms-firmware/nmc_hf.bin)                    |                          | 131d14c196b14055f2ed0eccbb49bbd692ba968da6909601e0e1caa3153864a6 |
| [JA](../3rd_party/network-ms-firmware/nmc_ja.bin)                    | 2013-02-19               | 61d39a3333d995ffaa102c8662e7529f52fffa55ba6cda3310535136c43c74f9 |
| [JB](../3rd_party/network-ms-firmware/nmc_jb.bin)                    | 2014-06-02 or 2014-02-06 | 6df20f2585b586665ca8a3490a9aa4529ba36594fae6d698640c080591b0c092 |
| [JC](../3rd_party/network-ms-firmware/NMC_EATON_JC.bin)              | 2014-09-02 or 2014-02-09 | fadd9692a57a3a2b5945e7071bd46a6fbb62f7f015b65131ed5881dabe0e3f5c |
| JD                                                           |                          |                                                              |
| JF                                                           |                          |                                                              |
| JH                                                           |                          |                                                              |
| JI                                                           |                          |                                                              |
| JJ                                                           |                          |                                                              |
| JK                                                           |                          |                                                              |
| JL                                                           | 2017-02-23               |                                                              |
| [KB](../3rd_party/network-ms-firmware/NetworkMS_KB.bin)              | 2017-12-19               | a7eac29f0f8c8c3a0bb2a166be5a817210572af58c404ecce80bf07291f8b631 |
| LA                                                           |                          |                                                              |
| LB                                                           |                          |                                                              |
| LC                                                           | 2018-10-18               |                                                              |
| [LD](../3rd_party/network-ms-firmware/NetworkMS_LD.bin)              | 2018-12-12               | 99e8827cb9ac171cd14775e29ba8cfb4481f2ba92d3122858045764db7f60c29 |
| [LE](../3rd_party/network-ms-firmware/NMC_EATON_LE.bin)              | 2020-11-02               | 4cb34195fbc8313801a412a94669185711903e0cc38275edf86580e7f32847c6 |

Some of the dates in the release notes are ambiguous, so both forms are given. A particularly silly example is HE, which says "10/06/2011 (english format)", but the HD firmware has a release date of 2011-08-31, which would mean HD came out two months *after* HE. This is obviously not the case; they just messed up the date.

There's a [release note](../3rd_party/network-ms-firmware/release_note_nmct.txt) with no exact version ID, just stating "Version 1.50" and a notice about "performance/reliability issues with NMCs using SSL", which is thought to be associated with version AA. The note about SSL issues aligns with what later release notes say about version AA. There's also a firmware file just called "nmc_upload.bin", which is the smallest seen so far (~700KB) - this may well be the AA release.

### Firmware header data

| **Version ID**                                        | File Size | Header Size<sup>†</sup> | Signature | Version | Flags<sup>‡</sup> | Flash Address | RAM Address | Image Size |
| ----------------------------------------------------- | --------- | ----------------------- | --------- | ------- | ----------------- | ------------- | ----------- | ---------- |
| [AA?](../3rd_party/network-ms-firmware/nmc_uupload.bin)       | 708098    | 36/36                   | bootHdr   | 0x0000  | 0x00000009        | 0x00020000    | 0x08004000  | 0x000ACDDA |
| [Unknown (216)](../3rd_party/network-ms-firmware/nmc_216.bin) | 1045161   | 36/36                   | bootHdr   | 0x0000  | 0x00000009        | 0x00020000    | 0x08004000  | 0x000FF281 |
| [EB](../3rd_party/network-ms-firmware/nmc_eb.bin)             | 1096751   | 36/36                   | bootHdr   | 0x0001  | 0x00000009        | 0x00020000    | 0x08004000  | 0x0010BC07 |
| [EC](../3rd_party/network-ms-firmware/nmc_ec.bin)             | 1096483   | 36/36                   | bootHdr   | 0x0001  | 0x00000009        | 0x00020000    | 0x08004000  | 0x0010BAFB |
| [EE](../3rd_party/network-ms-firmware/nmc_ee.bin)             | 1104562   | 36/36                   | bootHdr   | 0x0001  | 0x00000009        | 0x00020000    | 0x08004000  | 0x0010DA8A |
| [FA](../3rd_party/network-ms-firmware/nmc_fa.bin)             | 1259383   | 36/36                   | bootHdr   | 0x0002  | 0x00000009        | 0x00010000    | 0x08004000  | 0x0013374F |
| [GA](../3rd_party/network-ms-firmware/nmc_ga.bin)             | 1349584   | 36/36                   | bootHdr   | 0x0002  | 0x00000009        | 0x00010000    | 0x08004000  | 0x001497A8 |
| [GB](../3rd_party/network-ms-firmware/nmc_gb.bin)             | 1349057   | 36/36                   | bootHdr   | 0x0002  | 0x00000009        | 0x00010000    | 0x08004000  | 0x00149599 |
| [GC](../3rd_party/network-ms-firmware/nmc_gc.bin)             | 1399686   | 36/36                   | bootHdr   | 0x0002  | 0x00000009        | 0x00010000    | 0x08004000  | 0x00155B5E |
| [GD](../3rd_party/network-ms-firmware/nmc_gd.bin)             | 1399337   | 36/36                   | bootHdr   | 0x0002  | 0x00000009        | 0x00010000    | 0x08004000  | 0x00155A01 |
| [GE](../3rd_party/network-ms-firmware/nmc_ge.bin)             | 1522690   | 36/36                   | bootHdr   | 0x0002  | 0x00000009        | 0x00010000    | 0x08004000  | 0x00173BDA |
| [HA](../3rd_party/network-ms-firmware/nmc_ha.bin)             | 1567064   | 36/36                   | bootHdr   | 0x0003  | 0x00000009        | 0x00010000    | 0x08004000  | 0x0017E930 |
| [HB](../3rd_party/network-ms-firmware/nmc_hb.bin)             | 1571023   | 36/36                   | bootHdr   | 0x0003  | 0x00000009        | 0x00010000    | 0x08004000  | 0x0017F8A7 |
| [HC](../3rd_party/network-ms-firmware/nmc_hc.bin)             | 1645046   | 36/36                   | Nmc_hdr   | 0x0003  | 0x00000009        | 0x00010000    | 0x08004000  | 0x001919CE |
| [HD](../3rd_party/network-ms-firmware/nmc_hd.bin)             | 1645543   | 36/36                   | Nmc_hdr   | 0x0003  | 0x00000009        | 0x00010000    | 0x08004000  | 0x00191BBF |
| [HE](../3rd_party/network-ms-firmware/nmc_he.bin)             | 1645540   | 36/36                   | Nmc_hdr   | 0x0003  | 0x00000009        | 0x00010000    | 0x08004000  | 0x00191BBC |
| [HF](../3rd_party/network-ms-firmware/nmc_hf.bin)             | 1677408   | 36/36                   | Nmc_hdr   | 0x0003  | 0x00000009        | 0x00010000    | 0x08004000  | 0x00199838 |
| [JA](../3rd_party/network-ms-firmware/nmc_ja.bin)             | 1893248   | 36/36                   | Nmc55Hd   | 0x0003  | 0x00000009        | 0x00010000    | 0x08004000  | 0x001CE358 |
| [JB](../3rd_party/network-ms-firmware/nmc_jb.bin)             | 1901076   | 36/36                   | Nmc55Hd   | 0x0003  | 0x00000009        | 0x00010000    | 0x08004000  | 0x001D01EC |
| [JC](../3rd_party/network-ms-firmware/NMC_EATON_JC.bin)       | 1964508   | 36/36                   | Nmc55Hd   | 0x0003  | 0x00000009        | 0x00010000    | 0x08004000  | 0x001DF9B4 |
| [KB](../3rd_party/network-ms-firmware/NetworkMS_KB.bin)       | 2370377   | 92/92                   | NmcKA     | 0x0705  | 0x00000009        | 0x00020000    | 0x00004000  | 0x00242AE9 |
| [LD](../3rd_party/network-ms-firmware/NetworkMS_LD.bin)       | 2250526   | 92/92                   | NmcKA     | 0x0705  | 0x00000009        | 0x00020000    | 0x00004000  | 0x002256BE |
| [LE](../3rd_party/network-ms-firmware/NMC_EATON_LE.bin)       | 2248825   | 92/92                   | NmcKA     | 0x0705  | 0x00000009        | 0x00020000    | 0x00004000  | 0x00225019 |

<sup>†</sup> The header size is a split field, where the first number is the size of the complete header, and the second number is the size of the NET+OS header. The first number can be larger if custom header information is included. So far all firmware images have the same size for both fields, with a larger header introduced in a later firmware version (possibly NET+OS 7.5)

<sup>‡</sup> The flags field is a bitfield with a number of defined constants, but unfortunately the exact values of those constants haven't been published online as they're part of the NET+OS BSP source. The ones we know exist are `BL_WRITE_TO_FLASH`, `BL_EXECUTE_FROM_ROM`, `BL_LZSS_COMPRESSED`, `BL_LZSS2_COMPRESSED`, and `BL_BYPASS_CRC_CHECK`.

No reverse engineering has been done on this card. It's a low priority since it's so old.

