<p align="left" dir="auto">
  <a href="https://opensource.ganweicloud.com" rel="nofollow">
    <img style="width:130px;height:130px;" src="https://github.com/ganweisoft/Devices/blob/main/src/src/logo.jpg">
  </a>
</p>

[![GitHub license](https://camo.githubusercontent.com/5eaf3ed8a7e8ccb15c21d967b8635ac79e8b1865da3a5ccf78d2572a3e10738a/68747470733a2f2f696d672e736869656c64732e696f2f6769746875622f6c6963656e73652f646f746e65742f6173706e6574636f72653f636f6c6f723d253233306230267374796c653d666c61742d737175617265)](https://github.com/ganweisoft/Devices/blob/main/LICENSE) ![AppVeyor](https://ci.appveyor.com/api/projects/status/v8gfh6pe2u2laqoa?svg=true) ![](https://img.shields.io/badge/join-discord-infomational)

English | [简体中文](README-CN.md)

Devices natively support Modbus and OPC UA (Open Platform Communications Unified Architecture), two of the most widely used communication protocols in the field of industrial automation, providing efficient and reliable data acquisition and device interaction capabilities.

# Source Code Structure
```bash
|-- GWModbusStandard.STD          # Modbus Protocol Standard Implementation Module
|   |-- Core                      # Modbus Core Protocol Implementation
|   |-- Helper                    # Modbus Protocol Auxiliary Utilities
|   |-- Model                     # Modbus Data Model Definitions
|   |-- Service                   # Modbus Communication Service Implementation
|-- GWOpcUAStandard.STD           # OPC UA Protocol Standard Implementation Module
    |-- Helper                    # OPC UA Protocol Auxiliary Utilities
    |-- Model                     # OPC UA Information Model Definitions
    |-- Service                   # OPC UA Server/Client Implementation
    |-- lib                       # OPC UA Protocol Stack Dependencies
```

### License  
Devices is licensed under the very permissive MIT License. For details, see [License](https://github.com/ganweisoft/Devices/blob/main/LICENSE).

### How to Contribute  
We warmly welcome contributions from developers. If you find a bug or have any ideas you'd like to share, feel free to submit an [issue](https://github.com/ganweisoft/Devices/blob/main/CONTRIBUTING.md).
