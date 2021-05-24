# Open Network-MS

## What's this?

This project's goal is to build an Arduino-based open-source hardware replacement for Eaton / MGE UPS network cards, primarily the Network MiniSlot (MS) card, also referred to internally as NMC1.

## Why?

There are a few reasons why an OSHW replacement for these cards makes sense:

- An open source project offers home network hobbyists an opportunity to contribute features that are useful to them.
- Eaton's current generation [Network-M2](https://www.eaton.com/us/en-us/catalog/backup-power-ups-surge-it-power-distribution/eaton-gigabit-network-card---na.html) card is very expensive (200 GBP or more)
- Eaton's discontinued [Network-MS](https://www.eaton.com/us/en-us/catalog/backup-power-ups-surge-it-power-distribution/eaton-network-card-ms.html) card is still quite expensive (100 GBP or more)
- Second-hand stock availability will continue to worsen, leading to even higher pricing in future.
- MGE 66102 cards (the older predecessor to Network-MS) have very limited compatibility.
- Official support for legacy products is minimal, particularly for products made by MGE before the acquisition.
- USB interfaces on the UPSes can be unreliable and extremely frustrating to debug.
- Firmware updates for Network-MS and MGE 66102 cards are restricted to a maximum version depending on the hardware revision ("technical level") of the card, and most of the advertised "compatible" UPS products are not supported on the earlier firmware versions. If you get a card with an older revision, it cannot be upgraded to newer firmware, and the card may not work with your UPS.
- Sellers usually do not specify a particular hardware revision as part of listings. There is no reliable public information on how to identify a card's hardware revision visually. Generally you have to plug the card into a compatible UPS and go into the web panel or console to check, which is often a catch-22 situation.
- The environmental monitoring probe (temperature and humidity sensor) is not included as standard and is *horrifically* overpriced.
- Older firmware versions contain security vulnerabilities. Fixes for these issues have not been back-ported for use with earlier hardware revisions, and it is very unlikely that they ever will be.
- The latest firmware revision for the Network-MS only supports SSLv3 and TLSv1.0, with a self-signed 1024-bit RSA certificate, static RSA key exchange, 128-bit RC4 (not 512-bit as they rather ludicrously suggest in the documentation), and MD5 digest. This is woefully outdated - both SSLv3 and TLSv1.0 are deprecated.

All of this aside, it seems unfair to buy a pricey rackmount UPS and find that connecting it to your network for monitoring is an expensive and potentially-frustrating process.

## Goals

The goals and guiding principles of the project are as follows:

- **Cheap** - The card should cost less than a second hand Network-MS card.
- **Easy** - It should be a drop-in replacement for the old cards, with no complicated setup.
- **Available** - Parts used in the hardware should be commonly available across the world, and easily swapped if a specific part is discontinued or out of stock.
- **Flexible** - The card design should try to accommodate as many unknowns as possible, to maximise the likelihood that it will work in as-yet-untested UPSes. The card should contain functionality that aids remote reverse engineering and debugging.
- **Clear** - Documentation is as important as functionality. Missing or unclear documentation is a bug.

## Status

As of the 21st of May 2021, this project is in the very early stages of reverse engineering the existing systems.

The first piece of hardware, codename Spork, has gone through a draft design and will be going for production within the next week.

No other hardware or software has been designed yet.

## Hardware Roadmap

There are four primary phases to the project:

1. Codename ***Spork*** - A simple interposer board that enables initial reverse engineering work without disassembly of the UPS.
2. Codename ***Chopstick*** - The first prototype, utilising an external ESP32 development board.
3. Codename ***Sushi*** - A refined version of Chopstick, with wired Ethernet and an SD card.
4. Codename ***Peppercorn*** - A fully integrated card with the ESP32 brought onboard, no longer relying upon a plug-in dev board.

### Spork

Spork is an interposer that sits between a real network card and the UPS, making it easier to hook into the electrical signals with an oscilloscope or other test equipment. It has no functionality of its own - it is simply an electrical pass-through with test points.

See the [Spork documentation](spork.md) for more information.

#### Status

As of 2021-05-21, a draft version of the Spork PCB has been designed, and it is expected to be sent for manufacturing within the next week.

Some initial investigation into the Network-MS card has been done.

### Chopstick

Chopstick will be the first working prototype card. It will be based on an ESP32 MCU, which provides WiFi and Bluetooth connectivity.

To simplify the design, the card will most likely have a slot for an DO1T (aka DOIT) ESP32 DevKit V1 dev board. These are really cheap and it saves a bunch of unnecessary design work around the MCU and USB side of things, for the prototyping phase.

Likely hardware features include:

- Onboard 3.3V and 5.0V regulation from the UPS power rail (possibly [TPS6300x](https://www.ti.com/product/TPS63000) buck-boost).
- 5V tolerant I/O with the UPS.
- Full TVS diode coverage of all I/O connectors.
- External temperature and humidity sensing (DHT sensor)
- Internal temperature and humidity sensing (Sensirion SHT sensor)
- Locator and status LEDs. Possibly something RGB for multi-function use.
- External alarm signal (likely hi-Z, pulled to 0V when alarm asserted)

Possible hardware features include:

- Optional internal smoke sensor (MQ-2)
- External GPIO, SPI, and/or I2C.
- NC/NO alarm signals.

This version will be a fairly rough initial design, with less attention paid to part placement.

#### Design decisions

- [ ] What switching regs should be used?
- [ ] Can we bypass the onboard linear reg on the ESP32 dev board and drive 3V3 directly?
- [ ] Which IO buffer should be used on the UPS side? Consider propagation delay if IO rate is high.
- [ ] Which IO buffer should be used on the 

### Sushi

Sushi is the second prototype iteration. It will add wired Ethernet support and an SD card. Sushi will continue to use an ESP32 dev board.

The most likely candidate PHY is the WIZnet W5500, since there are existing libraries that should just work out of the box. The main attraction is that the W5xxx series chips have excellent library support and are interfaced via SPI, meaning that only one dedicated GPIO is consumed by it.

The ESP32 has an inbuilt 10/100 Ethernet MAC interface that can talk to a PHY over an RMII interface. This has some benefits: the RMII interface uses DMA, so it's much faster and uses less CPU time for transfers, and the MAC supports VLAN tagging features, which the W5500 doesn't. The major downside is that RMII requires a dedicated 9 pin interface, which eats up half of the GPIOs on even the bigger ESP32 modules.

Unless someone can come up with an incredibly compelling reason to use RMII, the plan is a W5500 or similar SPI-based chip.

There's no point talking about gigabit, since the ESP32 MAC doesn't support it even with a full 17-pin MII interface.

An SD card slot will likely be added, for storing configuration data and logs.

### Peppercorn

Peppercorn will be the first fully integrated design. The MCU will be bought on board, so there is no longer any dependence on the external dev board. This will be the first device that's more of an actual tool than a development platform.

Depending on how many issues there are with WiFi and BT reception on modules that use an integrated antenna, the regular ESP32 module may be replaced with one of the IPEX options, to allow for an external antenna.

The ESP32-IROVER-IE module seems like a good choice. Dual core, 80-240MHz, 20x GPIO, 4-16MB flash, 8MB PSRAM, IPEX antenna port.

The USB interface will be handled by one of the cheap CP120x chips. Works fine for all of the dev boards.

A face plate for the board will be cut from plastic using a laser cutter.

### Beyond

Some other ideas that are worth investigating in the far future:

- Additional battery backup for the network card, to keep it up and running far after the UPS battery is exhausted.
- 3G/4G/5G network connection, to ensure that alerts reach someone even if the network is completely down.

## Software

The firmware for the cards will be Arduino-based.

### Features

Planned features include:

- UPS features
  - Read input voltage, frequency
  - Read output voltage, frequency, power, load
  - Integration of kWh measurements
  - Read battery capacity
  - Read battery remaining time
  - Manual on/off
  - Scheduled power on/off
  - Load balancing support (without requiring Intelligent Power Protector)
  - Load shedding support via controlled outlets
- Alerts and logging
  - Local SD card logging
  - Prometheus endpoint
  - InfluxDB support
  - MQTT support
  - SMTP email alert support
  - Syslog support
  - SNMP (exact features and versions supported depends on available SNMP libraries)
  - SNMP traps
- Security
  - Password authentication on the web panel (likely BLAKE2 hashing)
  - TLS 1.2 support at minimum, possible TLS 1.3 support
  - Custom certificate support
  - Custom CA support (for handling internal CAs)
  - Audit events for security events and config changes
- Web panel
  - Status page
  - WiFi configuration
  - IPv4 / DNS configuration
  - Authentication and security
  - Graphing
  - NTP configuration
  - Alerting configuration (Prometheus, InfluxDB, MQTT, SMTP, Syslog, SNMP, etc.)
  - Environmental sensor configuration
  - Load balancing configuration
  - UPS shutdown / load shedding configuration
  - Config backup and restore
- Misc
  - NTP time synchronisation

### Roadmap

The version roadmap is as follows:

- **0.0.x** - Initial interface work, getting the code to talk to the UPS and get the right information out of it.
- **0.1.x** - Minimum viable product. Basic web interface, UPS state reported, Prometheus endpoint works.
- **0.2.x** - Initial security feature set added (TLS, authentication, etc.)
- **0.3.x** to **0.9.x** - Feature support expansion.
- **1.0.x** and beyond - Complete base feature set implemented.

## Documentation

- [Reverse engineering](docs/reverse-engineering/reversing.md)
- [Spork](docs/spork/spork.md) (interposer board)

## Test Hardware

The following hardware is being used for development.

### UPS products

- MGE Pulsar Evolution 3000 UPS

### Network cards

- Eaton Network-MS Card
  - Thought to be technical level 06 or 07.
  - Thought to be card revision FA.
  - Part number: `710-00255-06P`
  - Front silkscreen: `CARTE NMC1 34003816XD_1FA`
  - Rear silkscreen: `CARTE NMC1 34003816XD_6FA`
- MGE 66102 Card
  - Too early a hardware revision for the Pulsar Evolution 3000, so mostly being used as a hardware reference.
  - Technical level 03.
  - Card revision CA.
  - P/N ???? (need to dig the card out to check)
  - Front silkscreen `CARTE NMC1 34003816XD_1CA`
  - Rear silkscreen `CARTE NMC1 34003816XD_6CA`

### Expanding coverage

If you have an Eaton UPS and a Network-MS card, and would like to help out, please get in touch. Dumping the card IO while browsing through the web panel can provide a ton of insight into how various features work.

If you wish to donate a Network-MS or Network-M2 card of any technical level, please let me know. Higher technical levels are preferred; a revision JA (technical level 17) Network-MS card would help a lot. I'd also be interested if you want to sell a card, as long as it's a significantly later technical level than I already have.

If you happen to have an older UPS that's supported by a technical level 06 card (check section 1.2.1 of the [Network-MS user guide](docs/eaton-network-card-ms-user-guide-manual.pdf) for a full list), but are using a more modern Network-MS card - i.e. technical level 08 or higher - please consider swapping your card with mine to help out this project. I can offer some compensation if needed.

## License

The hardware design files and software files are released under MIT license.

Firmware files, documents, and other materials from MGE and Eaton are provided here for convenient reference only, and are not released under any license. If you are an employee or representative of Eaton and you have questions or concerns about the content distributed here, please open up a GitHub issue, or privately contact me at *{my github username at gmail*}.

Datasheets and other documentation from third parties are provided here for convenient reference only, and are not released under any license.